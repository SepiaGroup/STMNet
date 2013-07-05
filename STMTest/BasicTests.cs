using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using STM;

namespace STMTest
{
	[TestClass]
	public class BasicTests
	{
		[TestMethod]
		public void CreateStmIntObject()
		{
			var s = Stm.CreateObject(1);

			Assert.IsTrue(s.Value == 1);
		}

		private class MyTestClass : ICloneable
		{
			public int MyProp { get; set; }


			public object Clone()
			{
				return new MyTestClass {MyProp = MyProp};
			}
		}

		[TestMethod]
		public void CreateStmClassObject()
		{
			var s = Stm.CreateObject(new MyTestClass { MyProp = 1 });

			Assert.IsTrue(s.Value.MyProp == 1);
		}

		[TestMethod]
		public void ReadStmIntObject()
		{
			var s = Stm.CreateObject(1);

			var t = Stm.BeginTransaction();

			var r = s.Read();

			Assert.IsTrue(r == 1);
		}

		[TestMethod]
		public void ReadStmClassObject()
		{
			var s = Stm.CreateObject(new MyTestClass { MyProp = 1 });

			var t = Stm.BeginTransaction();

			var r = s.Read();

			Assert.IsTrue(r.MyProp == 1);
		}

		[TestMethod]
		public void WriteStmIntObject()
		{
			var s = Stm.CreateObject(1);

			var t = Stm.BeginTransaction();

			s.Write(2);

			var r = s.Read();

			Assert.IsTrue(r == 2);
		}

		[TestMethod]
		public void WriteStmClassObject()
		{
			var s = Stm.CreateObject(new MyTestClass { MyProp = 1 });

			var t = Stm.BeginTransaction();

			s.Write(new MyTestClass {MyProp = 2});

			var r = s.Read();

			Assert.IsTrue(r.MyProp == 2);
		}
		
		[TestMethod]
		public void ThreadWriteStmIntObject()
		{
			var s = Stm.CreateObject(1);

			var are = new AutoResetEvent(false);

			new Thread(() =>
				{
					var t1 = Stm.BeginTransaction();
					s.Write(2);

					t1.Commit();

					are.Set();
				}).Start();

			are.WaitOne();

			var v = s.Value;

			Assert.IsTrue(v == 2);
		}

		[TestMethod]
		public void ThreadWriteStmClassObject()
		{
			var s = Stm.CreateObject(new MyTestClass { MyProp = 1 });

			var are = new AutoResetEvent(false);

			new Thread(() =>
			{
				using (Stm.BeginTransaction())
				{
					s.Write(new MyTestClass {MyProp = 2});
				}

				are.Set();
			}).Start();

			are.WaitOne();

			var v = s.Value;

			Assert.IsTrue(v.MyProp == 2);
		}


		private void WaitOnMRE(ManualResetEvent re)
		{
			re.WaitOne();
			Debug.WriteLine("*** Done Waiting ***");
		}

		[TestMethod]
		public void ThreadMRETest()
		{
			var re = new ManualResetEvent(false);

			var t1 = new Thread(() => WaitOnMRE(re));
			var t2 = new Thread(() => WaitOnMRE(re));

			t1.Start();
			t2.Start();

			re.Set();

			Thread.Sleep(5000);
		}

		
	}
}
