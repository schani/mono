#include <assert.h>
#include <pthread.h>
#include <signal.h>

#include <config.h>
#include <mono/metadata/metadata.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/utils/mono-linked-list-set.h>
#include <mono/utils/atomic.h>

static MonoLinkedListSet lls;

enum {
	STATE_OUT,
	STATE_BUSY,
	STATE_IN
};

#define N 23
#define NUM_ITERS 10000000
#define NUM_THREADS 8
#define LOCAL_NODES_PER_THREAD 0

typedef struct {
	MonoLinkedListSetNode node;
	int tid;		/* owner thread, or -1 if it's global */
	int state;
} node_t;

typedef struct {
	int tid;
	int skip;
	int num_adds;
	int num_removes;
	pthread_t thread;
} thread_data_t;

static node_t nodes [N];

static inline void
mono_hazard_pointer_clear_all (MonoThreadHazardPointers *hp, int retain)
{
	if (retain != 0)
		mono_hazard_pointer_clear (hp, 0);
	if (retain != 1)
		mono_hazard_pointer_clear (hp, 1);
	if (retain != 2)
		mono_hazard_pointer_clear (hp, 2);
}

static void
free_node (void *n)
{
	node_t *node = n;
	assert (node->state == STATE_BUSY);
	node->state = STATE_OUT;
}

static gboolean
change_state (int *state, gboolean local, int new, int old)
{
	if (local) {
		assert (*state == old);
		*state = new;
		return TRUE;
	}
	return InterlockedCompareExchange (state, new, old) == old;
}

static void
signal_handler (int signum)
{
	//g_print ("signal!\n");
	g_usleep (75000);
}

static void
set_signal_handler (void)
{
	int result;
	/*
	struct sigaction sa;

	sa.sa_handler = signal_handler;
	sa.sa_sigaction = NULL;
	sigfillset (&sa.sa_mask);
	sa.sa_flags = SA_RESTART;
	result = sigaction (SIGXCPU, &sa, NULL);
	*/
	result = signal (SIGXCPU, signal_handler) == SIG_ERR;
	assert (!result);
}

static void*
worker (void *arg)
{
	thread_data_t *thread_data = arg;
	MonoThreadHazardPointers *hp;
	int skip = thread_data->skip;
	int i, j;
	gboolean result;

	//set_signal_handler ();

	mono_thread_info_register_small_id ();

	hp = mono_hazard_pointer_get ();

	i = 0;
	for (j = 0; j < NUM_ITERS; ++j) {
		gboolean local = nodes [i].tid == thread_data->tid;
		if (!local && nodes [i].tid >= 0)
			goto skip_over;

		switch (nodes [i].state) {
		case STATE_BUSY:
			mono_thread_hazardous_try_free_some ();
			break;
		case STATE_OUT:
			if (change_state (&nodes [i].state, local, STATE_BUSY, STATE_OUT)) {
				result = mono_lls_find (&lls, hp, i);
				assert (!result);
				mono_hazard_pointer_clear_all (hp, -1);

				result = mono_lls_insert (&lls, hp, &nodes [i].node);
				mono_hazard_pointer_clear_all (hp, -1);

				assert (nodes [i].state == STATE_BUSY);
				nodes [i].state = STATE_IN;

				++thread_data->num_adds;
			}
			break;
		case STATE_IN:
			if (change_state (&nodes [i].state, local, STATE_BUSY, STATE_IN)) {
				result = mono_lls_find (&lls, hp, i);
				assert (result);
				assert (mono_hazard_pointer_get_val (hp, 1) == &nodes [i].node);
				mono_hazard_pointer_clear_all (hp, -1);

				result = mono_lls_remove (&lls, hp, &nodes [i].node);
				mono_hazard_pointer_clear_all (hp, -1);

				++thread_data->num_removes;
			}
			break;
		default:
			assert (FALSE);
		}

		skip_over:
		i += skip;
		if (i >= N) {
			node_t *node;

			i -= N;

			MONO_LLS_FOREACH_SAFE (&lls, node, node_t*) {
				assert (node->state != STATE_OUT);
			} MONO_LLS_END_FOREACH_SAFE;
		}
	}

	return NULL;
}

int
main (int argc, char *argv [])
{
	int primes [] = { 1, 2, 3, 5, 7, 11, 13, 17 };
	MonoThreadInfoCallbacks thread_callbacks;
	thread_data_t thread_data [NUM_THREADS];
	int i, result, stop_count;

	memset (&thread_callbacks, 0, sizeof (thread_callbacks));

	mono_metadata_init ();

	mono_threads_init (&thread_callbacks, 0);

	mono_thread_smr_init ();
	mono_lls_init (&lls, free_node);

	set_signal_handler ();

	for (i = 0; i < N; ++i) {
		nodes [i].node.key = i;
		nodes [i].state = STATE_OUT;
		nodes [i].tid = -1;
	}

	for (i = 0; i < NUM_THREADS; ++i) {
		int j;

		for (j = 0; j < LOCAL_NODES_PER_THREAD; ++j)
			nodes [i * LOCAL_NODES_PER_THREAD + j].tid = i;
	}

	for (i = 0; i < NUM_THREADS; ++i) {
		thread_data [i].tid = i;
		thread_data [i].num_adds = thread_data [i].num_removes = 0;
		thread_data [i].skip = primes [i] % N;
		result = pthread_create (&thread_data [i].thread, NULL, worker, &thread_data [i]);
		assert (!result);
	}

	i = 0;
	stop_count = 0;
	while (pthread_tryjoin_np (thread_data [0].thread, NULL)) {
		g_usleep (100000);
		pthread_kill (thread_data [i].thread, SIGXCPU);
		i = (i + 1) % NUM_THREADS;
		++stop_count;
	}

	for (i = 1; i < NUM_THREADS; ++i) {
		result = pthread_join (thread_data [i].thread, NULL);
		assert (!result);
	}

	for (i = 0; i < NUM_THREADS; ++i)
		printf ("thread %d  adds %d  removes %d\n", i, thread_data [i].num_adds, thread_data [i].num_removes);
	printf ("stopped %d times\n", stop_count);

	return 0;
}
