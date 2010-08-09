/*
 * sgen-cardtable.c: Card table implementation for sgen
 *
 * Author:
 * 	Rodrigo Kumpera (rkumpera@novell.com)
 *
 * Copyright 2010 Novell, Inc (http://www.novell.com)
 *
 */

#define CARD_COUNT_BITS (32 - 9)
#define CARD_COUNT_IN_BYTES (1 << CARD_COUNT_BITS)


static guint8 *cardtable;


guint8*
sgen_card_table_get_card_address (mword address)
{
	return cardtable + (address >> CARD_BITS);
}


void
sgen_card_table_mark_address (mword address)
{
	*sgen_card_table_get_card_address (address) = 1;
}

static gboolean
sgen_card_table_address_is_marked (mword address)
{
	return *sgen_card_table_get_card_address (address) != 0;
}

void*
sgen_card_table_align_pointer (void *ptr)
{
	return (void*)((mword)ptr & ~(CARD_SIZE_IN_BYTES - 1));
}

void
sgen_card_table_reset_region (mword start, mword end)
{
	memset (sgen_card_table_get_card_address (start), 0, (end - start) >> CARD_BITS);
}

void
sgen_card_table_mark_range (mword address, mword size)
{
	mword end = address + size;
	do {
		sgen_card_table_mark_address (address);
		address += CARD_SIZE_IN_BYTES;
	} while (address < end);
}

gboolean
sgen_card_table_is_region_marked (mword start, mword end)
{
	while (start <= end) {
		if (sgen_card_table_address_is_marked (start))
			return TRUE;
		start += CARD_SIZE_IN_BYTES;
	}
	return FALSE;
}

static void
card_table_init (void)
{
	cardtable = mono_sgen_alloc_os_memory (CARD_COUNT_IN_BYTES, TRUE);
}


void major_scan_card_table (GrayQueue *queue);
void los_scan_card_table (GrayQueue *queue);
void major_clear_card_table (void);

static void
scan_from_card_tables (void *start_nursery, void *end_nursery, GrayQueue *queue)
{
	major_scan_card_table (queue);
	los_scan_card_table (queue);
}

static void
card_table_clear (void)
{
	/*XXX we could do this in 2 ways. using mincore or iterating over all sections/los objects */
	major_clear_card_table ();
	los_clear_card_table ();
}

#if 0

#include <unistd.h>
#include <sys/mman.h>
#include <sys/types.h>

static void
collect_faulted_cards (void)
{
#define CARD_PAGES (CARD_COUNT_IN_BYTES / 4096)
	int i, count = 0;
	unsigned char faulted [CARD_PAGES] = { 0 };
	mincore (cardtable, CARD_COUNT_IN_BYTES, faulted);

	for (i = 0; i < CARD_PAGES; ++i) {
		if (faulted [i])
			++count;
	}

	printf ("TOTAL card pages %d faulted %d\n", CARD_PAGES, count);
}
#endif
