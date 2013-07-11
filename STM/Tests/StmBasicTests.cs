using System;
using System.Collections;
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
	public class StmBasicTests
	{

		StmObject<int> s1 = Stm.CreateObject(2);
		private ConcurrentBag<Tuple<AcquireState, StmObject<int>>> acquireStates = new ConcurrentBag<Tuple<AcquireState, StmObject<int>>>();
		private int _commitedValue;

		private void ThreadAcquire()
		{
			var t = Stm.BeginTransaction();

			s1.Read();

			acquireStates.Add(Tuple.Create( t.TransactionLog.Transactions.First().Value.Acquire(), s1));
		}

		private void ThreadAcquireAndCommit(int newValue)
		{
			var t = Stm.BeginTransaction();

			s1.Write(newValue);

			var tle = t.TransactionLog.Transactions.First().Value;
			var acquireStatus = tle.Acquire();

			if (acquireStatus == AcquireState.Acquired)
			{
				tle.Commit();
				_commitedValue = s1.Value;
			}

			acquireStates.Add(Tuple.Create(acquireStatus, s1));
		}

		[TestMethod]
		public void ThreadWriteTestTimeout()
		{
			var thread1 = new Thread(ThreadAcquire);

			var thread2 = new Thread(ThreadAcquire);

			thread1.Start();
			thread2.Start();

			foreach (var t in new List<Thread> {thread1, thread2})
			{
				t.Join();
			}


			Assert.IsTrue(acquireStates.Count == 2);
			Assert.IsTrue(acquireStates.Any(a => a.Item1 == AcquireState.Acquired));
			Assert.IsTrue(acquireStates.Any(a => a.Item1 == AcquireState.Busy));
		}

		[TestMethod]
		public void ThreadWriteTest()
		{
			var thread1 = new Thread(() => ThreadAcquireAndCommit(10));

			var thread2 = new Thread(() => ThreadAcquireAndCommit(11));

			thread1.Start();
			thread2.Start();

			foreach (var t in new List<Thread> { thread1, thread2 })
			{
				t.Join();
			}

			Assert.IsTrue(acquireStates.Count == 2);
			Assert.IsTrue(acquireStates.First(f => f.Item1 == AcquireState.Acquired).Item2.Value == _commitedValue);
			Assert.IsTrue(acquireStates.First(f => f.Item1 == AcquireState.Failed).Item2.Value == _commitedValue);
		}
	}
}
