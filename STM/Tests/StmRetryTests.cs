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

		//[TestMethod]
		//public void TransactionRetryCount()
		//{
		//	var s1 = Stm.CreateObject(1);

		//	var t1 = new Transaction();
		//	var t2 = new Transaction();

		//	t1.LogWrite(s1, 2);

		//	t2.LogWrite(s1, 3);

		//	Assert.IsTrue(t1.Commit());

		//	_retryCount = 0;

		//	Assert.IsFalse(t2.Commit());

		//	Assert.IsTrue(_retryCount == 4);

		//}


		public class MyObj : IStmObject<MyObj>
		{
			public int Index { get; private set; }

			public MyObj(int index)
			{
				Index = index;
			}

			public MyObj Clone()
			{
				return new MyObj(Index);
			}
		}

		private StmObject<int> stmInt = Stm.CreateObject(1);

		private StmObject<MyObj> stmMyObj = Stm.CreateObject(new MyObj(0));


		public void TransactionActions(Transaction transaction)
		{
			transaction.LogWrite(stmInt, 3);

			transaction.Commit();
		}

		[TestMethod]
		public void TransactionRetryWithTransDelegate()
		{
			var t1 = new Transaction();
			var t2 = new Transaction(TransactionActions, RetryCount);

			t1.LogWrite(stmInt, 2);

			t2.LogWrite(stmInt, 3);

			Assert.IsTrue(t1.Commit());

			_retryCount = 0;

			Assert.IsFalse(t2.Commit());

			Assert.IsTrue(_retryCount == 4);

		}


		[TestMethod]
		public void TransactionRetryWithTransDelegateObject()
		{
			var t1 = new Transaction();
			var t2 = new Transaction(TransactionActions, RetryCount);

			t1.LogWrite(stmMyObj, new MyObj(1));

			t2.LogWrite(stmMyObj, new MyObj(2));

			Assert.IsTrue(t1.Commit());

			_retryCount = 0;

			Assert.IsFalse(t2.Commit());

			Assert.IsTrue(_retryCount == 4);

			Assert.IsTrue(stmMyObj.Element.Value.Index == 1);
		}
	}
}
