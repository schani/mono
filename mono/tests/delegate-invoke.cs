using System;
using System.Reflection;
using System.Globalization;
using System.Diagnostics;

struct EmptyStruct {
	public int value;
	public int test() {
		return value - 100;
	}
}

delegate int ActionRef (ref EmptyStruct _this);

public class Driver
{
	public static int Main () {
		return test_0_valuetype_invokes ();
	}
    public static int test_0_valuetype_invokes ()
    {
		EmptyStruct es = default (EmptyStruct);
		es.value = 100;
		var ar = (ActionRef)Delegate.CreateDelegate(typeof (ActionRef), typeof (EmptyStruct).GetMethod ("test"));
		return ar (ref es);
	}
}
