using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using STM;
using STM.Core;

namespace TestSTMNet
{
	[TestClass]
	public class TestNstmMemoryWithTxStack
	{
		[TestMethod]
		public void TestSingleTx()
		{
			Assert.AreEqual(0, StmTransaction.ActiveTransactionCount);
			using (var tx0 = StmTransaction.BeginTransaction())
			{
				Assert.AreEqual(1, StmTransaction.ActiveTransactionCount);
			}
			Assert.AreEqual(0, StmTransaction.ActiveTransactionCount);
		}


		[TestMethod]
		public void TestRequiresNestedTx()
		{
			// create nested tx
			using (var tx0 = StmTransaction.BeginTransaction())
			{
				Assert.AreEqual(1, StmTransaction.ActiveTransactionCount);
				using (var tx1 = StmTransaction.BeginTransaction())
				{
					Assert.AreEqual(2, StmTransaction.ActiveTransactionCount);
					Assert.IsTrue(tx1.IsNested);
				}
				Assert.AreEqual(1, StmTransaction.ActiveTransactionCount);
			}
			Assert.AreEqual(0, StmTransaction.ActiveTransactionCount);
		}


		[TestMethod]
		public void TestRequiredTx()
		{
			using (ITransaction tx0 = StmTransaction.BeginTransaction())
			{
				Assert.AreEqual(1, StmTransaction.ActiveTransactionCount);
				bool newTxCreated;
				using (ITransaction tx1 = StmTransaction.BeginTransaction(StmTransactionScopeOption.Required, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite, out newTxCreated))
				{
					Assert.IsFalse(newTxCreated);
					Assert.AreEqual(1, StmTransaction.ActiveTransactionCount);
					Assert.IsFalse(tx1.IsNested);
					Assert.AreEqual(tx0, tx1);
				}
				Assert.AreEqual(0, StmTransaction.ActiveTransactionCount);
			}
			Assert.AreEqual(0, StmTransaction.ActiveTransactionCount);


			//-- tx required: no tx active & current tx has compatible settings for second tx
			using (var tx0 = StmTransaction.BeginTransaction(StmTransactionScopeOption.Required, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnRead))
			{
				Assert.AreEqual(1, StmTransaction.ActiveTransactionCount);
				using (var tx1 = StmTransaction.BeginTransaction(StmTransactionScopeOption.Required, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
				{
					Assert.AreEqual(1, StmTransaction.ActiveTransactionCount);
				}
			}
			Assert.AreEqual(0, StmTransaction.ActiveTransactionCount);

			//-- tx required: no tx active & current tx does not match requirements
			using (var tx0 = StmTransaction.BeginTransaction(StmTransactionScopeOption.Required, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
			{
				Assert.AreEqual(1, StmTransaction.ActiveTransactionCount);
				using (var tx1 = StmTransaction.BeginTransaction(StmTransactionScopeOption.Required, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnRead))
				{
					Assert.AreEqual(2, StmTransaction.ActiveTransactionCount);
				}
			}
			Assert.AreEqual(0, StmTransaction.ActiveTransactionCount);
		}


		[TestMethod]
		public void TestRequiresNewTx()
		{
			// create new, independent tx while another is still active
			using (var tx0 = StmTransaction.BeginTransaction())
			{
				Assert.AreEqual(1, StmTransaction.ActiveTransactionCount);
				bool newTxCreated;
				using (var tx1 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite, out newTxCreated))
				{
					Assert.IsTrue(newTxCreated);
					Assert.AreEqual(2, StmTransaction.ActiveTransactionCount);
					Assert.IsFalse(tx1.IsNested);
					Assert.AreNotEqual(tx0, tx1);
				}
				Assert.AreEqual(1, StmTransaction.ActiveTransactionCount);
			}
			Assert.AreEqual(0, StmTransaction.ActiveTransactionCount);
		}


		[TestMethod]
		public void TestRequiredOrNested()
		{
			bool newTxCreated;

			// inner tx compatible with outer; no new tx created
			using (var tx0 = StmTransaction.BeginTransaction(StmTransactionScopeOption.Required, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite, out newTxCreated))
			{
				Assert.IsTrue(newTxCreated);

				using (var tx1 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiredOrNested, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite, out newTxCreated))
				{
					Assert.IsFalse(newTxCreated);
					Assert.AreEqual(1, StmTransaction.ActiveTransactionCount);
					Assert.IsFalse(tx1.IsNested);
					Assert.AreEqual(tx0, tx1);
				}
				Assert.AreEqual(0, StmTransaction.ActiveTransactionCount);
			}

			// inner tx not compatible with outer; create new nested tx
			using (var tx0 = StmTransaction.BeginTransaction(StmTransactionScopeOption.Required, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
			{
				using (var tx1 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiredOrNested, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnRead, out newTxCreated))
				{
					Assert.IsTrue(newTxCreated);
					Assert.AreEqual(2, StmTransaction.ActiveTransactionCount);
					Assert.IsTrue(tx1.IsNested);
					Assert.AreNotEqual(tx0, tx1);
				}
				Assert.AreEqual(1, StmTransaction.ActiveTransactionCount);
			}
		}


		[TestMethod]
		public void TestCloseTxInWrongOrder()
		{
			var txA = StmTransaction.BeginTransaction();
			ITransaction txB = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite);
			try
			{
				txA.Commit(); // this is wrong; the tx created first needs to be finished last (FILO)
			}
			catch (InvalidOperationException ex)
			{
				Assert.IsTrue(ex.Message.IndexOf("overlapping") >= 0);
				txB.Rollback();
				txA.Rollback();
			}
		}
	}
}
