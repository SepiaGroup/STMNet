using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace STM.Tests
{
	[TestClass]
	public class SMTBasicTests
	{

		StmObject<int> s1 = Stm.CreateObject(2);
		private ConcurrentBag<Tuple<AcquireState, StmObject<int>>> acquireStates = new ConcurrentBag<Tuple<AcquireState, StmObject<int>>>();
		private int commitedValue;

		private void ThreadAcquire(AutoResetEvent are)
		{
			var t = Stm.BeginTransaction();

			s1.Read();

			acquireStates.Add(Tuple.Create( t.TransactionLog.Transactions.First().Value.Acquire(), s1));

			are.Set();
		}

		private void ThreadAcquireAndCommit(AutoResetEvent are, int newValue)
		{
			var t = Stm.BeginTransaction();

			s1.Write(newValue);

			var tle = t.TransactionLog.Transactions.First().Value;
			var acquireStatus = tle.Acquire();

			if (acquireStatus == AcquireState.Acquired)
			{
				tle.Commit();
				commitedValue = s1.Value;
			}

			acquireStates.Add(Tuple.Create(acquireStatus, s1));

			are.Set();
		}

		[TestMethod]
		public void ThreadWriteTestTimeout()
		{

			var are1 = new AutoResetEvent(false);
			var are2 = new AutoResetEvent(false);

			var thread1 = new Thread(() => ThreadAcquire(are1));

			var thread2 = new Thread(() => ThreadAcquire(are2));

			thread1.Start();
			thread2.Start();

			foreach (var e in new WaitHandle[] { are1, are2 })
			{
				Assert.IsTrue(e.WaitOne());
			}


			Assert.IsTrue(acquireStates.Count == 2);
			Assert.IsTrue(acquireStates.Any(a => a.Item1 == AcquireState.Acquired));
			Assert.IsTrue(acquireStates.Any(a => a.Item1 == AcquireState.Busy));
		}

		[TestMethod]
		public void ThreadWriteTest()
		{

			var are1 = new AutoResetEvent(false);
			var are2 = new AutoResetEvent(false);

			var thread1 = new Thread(() => ThreadAcquireAndCommit(are1, 10));

			var thread2 = new Thread(() => ThreadAcquireAndCommit(are2, 11));

			thread1.Start();
			thread2.Start();

			foreach (var e in new WaitHandle[] { are1, are2 })
			{
				Assert.IsTrue(e.WaitOne());
			}

			Assert.IsTrue(acquireStates.Count == 2);
			Assert.IsTrue(acquireStates.First(f => f.Item1 == AcquireState.Acquired).Item2.Value == commitedValue);
			Assert.IsTrue(acquireStates.First(f => f.Item1 == AcquireState.Failed).Item2.Value == commitedValue);

		}
	}
}
