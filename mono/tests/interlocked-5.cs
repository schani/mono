using System.Threading;
using System;

public class Test {
	public static object Null2 (int i) {
		if (i == 0)
			return null;
		return Null2 (i - 1);
	}

	/* force non-inline */
	public static object Null () {
		return Null2 (123);
	}

	public static bool Correctness () {
		object a = "abc";
		object b = a;
		object c = Null ();
		Interlocked.CompareExchange<object> (ref a, b, c);
		if (c != null) {
			Console.WriteLine ("error");
			return false;
		}
		return true;
	}

	public static int Main () {
		if (!Correctness ())
			return 1;
		return 0;
	}
}
