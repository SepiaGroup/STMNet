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
	public class StmRetryTests
	{
		private int _retryCount;

		public bool RetryCount(Transaction transaction)
		{
			_retryCount++;

			return transaction.RetryCount < 3;

		}

		[TestMethod]
		public void TransactionRetryCount()
		{
			var s1 = Stm.CreateObject(1);

			var t1 = Stm.BeginTransaction();
			var t2 = Stm.BeginTransaction(RetryCount);

			t1.LogWrite(s1, 2);

			t2.LogWrite(s1, 3);

			Assert.IsTrue(t1.Commit());

			_retryCount = 0;

			Assert.IsFalse(t2.Commit());

			Assert.IsTrue(_retryCount == 4);

		}
	}
}
