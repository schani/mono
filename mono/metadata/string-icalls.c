/*
 * string-icalls.c: String internal calls for the corlib
 *
 * Author:
 *   Patrik Torstensson (patrik.torstensson@labs2.com)
 *   Duncan Mak  (duncan@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 */
#include <config.h>
#include <stdlib.h>
#include <stdio.h>
#include <signal.h>
#include <string.h>
#include "mono/utils/mono-membar.h"
#include <mono/metadata/string-icalls.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/object.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/profiler.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/gc-internal.h>

/* This function is redirected to String.CreateString ()
   by mono_marshal_get_native_wrapper () */
void
ves_icall_System_String_ctor_RedirectToCreateString (void)
{
	g_assert_not_reached ();
}

MonoString *
ves_icall_System_String_InternalAllocateStr (gint32 length)
{
	return mono_string_new_size(mono_domain_get (), length);
}

MonoString  *
ves_icall_System_String_InternalIntern (MonoString *str)
{
	MonoString *res;
	MONO_ARCH_SAVE_REGS;

	res = mono_string_intern(str);
	if (!res)
		mono_raise_exception (mono_domain_get ()->out_of_memory_ex);
	return res;
}

MonoString * 
ves_icall_System_String_InternalIsInterned (MonoString *str)
{
	MONO_ARCH_SAVE_REGS;

	return mono_string_is_interned(str);
}

int
ves_icall_System_String_GetLOSLimit (void)
{
	int limit = mono_gc_get_los_limit ();

	return (limit - 2 - sizeof (MonoString)) / 2;
}

void
ves_icall_System_String_InternalSetLength (MonoString *str, gint32 new_length)
{
	mono_unichar2 *new_end = (str->chars + new_length);
	g_assert (new_length <= str->length);
	
	/* zero terminate, we can pass string objects directly via pinvoke
	 * we also zero the rest of the string, since SGen needs to be
	 * able to handle the changing size (it will skip the 0 bytes). */
	 
	if (str->length < ves_icall_System_String_GetLOSLimit()) {
		CHECK_HIGH_CANARY ((str->chars + str->length +1))
		memset (new_end, 0, (str->length - new_length +1) * sizeof (mono_unichar2) + CANARY_SIZE);
		memcpy (new_end +1, CANARY_OVER_STRING, CANARY_SIZE);
	}
	else {
		memset (new_end, 0, (str->length - new_length +1) * sizeof (mono_unichar2));
	}
	
	str->length = new_length;
}

