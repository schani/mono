using System;
using System.Threading;

public class Test
{
	public static void recursive_locker (object o, int depth)
	{
		if (depth <= 0)
			return;

		lock (o)
			recursive_locker (o, depth - 1);
	}

	public static int test_0_recursive_lock_abort ()
	{
		for (int i = 0; i < 100; ++i)
		{
			object o = new object ();
			Thread t = new Thread (delegate ()
					       {
						       while (true)
							       recursive_locker (o, 10000);
					       });
			t.Start ();
			Thread.Sleep (10);
			t.Abort ();
			t.Join ();
		}
		return 0;
	}

	public static int Main () {
		return TestDriver.RunTests (typeof (Test));
	}
}
