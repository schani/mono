#ifndef __MONO_STATIC_ANALYZER_SUPPORT_H__
#define __MONO_STATIC_ANALYZER_SUPPORT_H__

#ifdef __CHECKER__
#include <glib.h>

#define PERMISSION_WORKER_THREAD	__attribute__((permission(worker_thread)))
#define PERMISSION_LOCKING		__attribute__((permission(locking))) PERMISSION_WORKER_THREAD
#define PERMISSION_LOCK_FREE		__attribute__((permission(lock_free))) PERMISSION_LOCKING

extern __SIZE_TYPE__ __builtin_object_size(void *, int) PERMISSION_LOCK_FREE;

extern void * __builtin___memcpy_chk(void *, const void *, __SIZE_TYPE__, __SIZE_TYPE__) PERMISSION_LOCK_FREE;
extern void * __builtin___memset_chk(void *, int, __SIZE_TYPE__, __SIZE_TYPE__) PERMISSION_LOCK_FREE;

extern void __sync_synchronize(void) PERMISSION_LOCK_FREE;

int getpagesize(void) PERMISSION_LOCK_FREE;
void *mmap(void *addr, size_t len, int prot, int flags, int fd, off_t offset) PERMISSION_LOCK_FREE;
int munmap(void *addr, size_t len) PERMISSION_LOCK_FREE;
int mprotect(void *addr, size_t len, int prot) PERMISSION_LOCK_FREE;

pthread_t pthread_self(void) PERMISSION_LOCK_FREE;

/* FIXME: These are not lock-free! */
void            g_log                   (const gchar    *log_domain,
                                         GLogLevelFlags  log_level,
                                         const gchar    *format,
                                         ...) PERMISSION_LOCK_FREE;

void    g_assertion_message_expr        (const char     *domain,
                                         const char     *file,
                                         int             line,
                                         const char     *func,
                                         const char     *expr) PERMISSION_LOCK_FREE;

void g_return_if_fail_warning (const char *log_domain,
			       const char *pretty_function,
			       const char *expression) PERMISSION_LOCK_FREE;

gpointer g_malloc0        (gsize	 n_bytes) PERMISSION_LOCKING;
void	 g_free	          (gpointer	 mem) PERMISSION_LOCKING;

#else
#define PERMISSION_WORKER_THREAD
#define PERMISSION_LOCK_FREE
#define PERMISSION_LOCKING
#endif
#endif
