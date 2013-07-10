#if DEBUG

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using STM.Core;

namespace STM
{
	// unlock acquired locks - konkurrierende tx

	[TestClass]
	public class TestNstmTransactionBasics
	{
		[TestMethod]
		public void TestTxActivityMode()
		{
			using (var tx = StmTransaction.BeginTransaction())
			{
				Assert.AreEqual(1, StmTransaction.ActiveTransactionCount);
				Assert.AreEqual(TransactionState.Active, tx.ActivityMode);

				tx.Commit();

				Assert.AreEqual(TransactionState.Committed, tx.ActivityMode);
			}

			using (var tx = StmTransaction.BeginTransaction())
			{
				Assert.AreEqual(1, StmTransaction.ActiveTransactionCount);
				Assert.AreEqual(TransactionState.Active, tx.ActivityMode);

				tx.Rollback();

				Assert.AreEqual(TransactionState.Aborted, tx.ActivityMode);
			}
		}


		[TestMethod]
		public void TestTxLog()
		{
			var p = StmTransaction.CreateObject<string>();
			p.Write("hello");

			using (var tx = (Transaction)StmTransaction.BeginTransaction())
			{
				Assert.AreEqual(0, tx.LogCount);

				var o = StmTransaction.CreateObject<int>();
				Assert.AreEqual(0, tx.LogCount);

				o = StmTransaction.CreateObject(1);
				Assert.AreEqual(1, tx.LogCount);

				Assert.AreEqual("hello", p.Read(StmReadOption.ReadOnly));
				Assert.AreEqual(2, tx.LogCount);

				Assert.AreEqual(1, o.Read(StmReadOption.ReadOnly));
				Assert.AreEqual(2, tx.LogCount);
			}
		}


		[TestMethod]
		public void TestCommitWithBoolReturn()
		{
			using (var tx = (Transaction)StmTransaction.BeginTransaction())
			{
				Assert.IsTrue(tx.Commit(false));
			}
		}


		[TestMethod]
		public void TestReadWriteCommit()
		{
			var l = new List<IStmObject<int>> {StmTransaction.CreateObject(1), StmTransaction.CreateObject<int>()};

			using (var tx = (Transaction)StmTransaction.BeginTransaction())
			{
				l.Add(StmTransaction.CreateObject(3)); // create inside tx and write

				l[0].Write(10);
				l[1].Write(20);
				l[2].Write(30);

				tx.Commit();
			}
			Assert.AreEqual(10, l[0].Read());
			Assert.AreEqual(20, l[1].Read());
			Assert.AreEqual(30, l[2].Read());
		}


		[TestMethod]
		public void TestReadWriteRollback()
		{
			var l = new List<IStmObject<int>> {StmTransaction.CreateObject(1), StmTransaction.CreateObject<int>()};

			using (var tx = (Transaction)StmTransaction.BeginTransaction())
			{
				l.Add(StmTransaction.CreateObject(3));

				l[0].Write(10);
				l[1].Write(20);
				l[2].Write(30);

				tx.Rollback();
			}
			Assert.AreEqual(1, l[0].Read());
			Assert.AreEqual(0, l[1].Read());
			Assert.AreEqual(0, l[2].Read()); // object exists outside tx, but value from inside tx has been rolled back

			using (var tx = (Transaction)StmTransaction.BeginTransaction())
			{
				l[0].Write(100);
			} // default at end of using() is to rollback
			Assert.AreEqual(1, l[0].Read());
		}


		[TestMethod]
		public void TestNestedTxCommit()
		{
			var o = StmTransaction.CreateObject(1);
			using (var tx = (Transaction)StmTransaction.BeginTransaction())
			{
				o.Write(2);
				Assert.AreEqual(2, o.Read(StmReadOption.ReadOnly));
				using (var tx2 = (Transaction)StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNested, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
				{
					o.Write(3);
					Assert.AreEqual(3, o.Read(StmReadOption.ReadOnly));
					tx2.Commit();
				}
				Assert.AreEqual(3, o.Read(StmReadOption.ReadOnly));
				tx.Commit();
			}
			Assert.AreEqual(3, o.Read(StmReadOption.ReadOnly));
		}


		[TestMethod]
		public void TestNestedTxRollback()
		{
			var o = StmTransaction.CreateObject(1);
			using (var tx = (Transaction)StmTransaction.BeginTransaction())
			{
				o.Write(2);
				Assert.AreEqual(2, o.Read(StmReadOption.ReadOnly));
				using (var tx2 = (Transaction)StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNested, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
				{
					o.Write(3);
					Assert.AreEqual(3, o.Read(StmReadOption.ReadOnly));
					tx2.Commit();
				}
				Assert.AreEqual(3, o.Read(StmReadOption.ReadOnly));
				tx.Rollback();
			}
			Assert.AreEqual(1, o.Read(StmReadOption.ReadOnly));
		}


		[TestMethod]
		public void TestCommitReadOnlyObjects()
		{
			var o = StmTransaction.CreateObject(1);
			var p = StmTransaction.CreateObject(2);
			using (var tx = (Transaction)StmTransaction.BeginTransaction())
			{
				Assert.AreEqual(1, o.Read(StmReadOption.ReadOnly));
				Assert.AreEqual(2, p.Read(StmReadOption.PassingReadOnly));
				tx.Commit();
			}
		}


		[TestMethod]
		public void TestMultipleCommitRollback()
		{
			var o = StmTransaction.CreateObject(1);
			using (var tx = (Transaction)StmTransaction.BeginTransaction())
			{
				o.Write(2);
				tx.Commit();
				tx.Commit();
			}
			Assert.AreEqual(2, o.Read());

			using (var tx = (Transaction)StmTransaction.BeginTransaction())
			{
				o.Write(3);
				tx.Commit();
				tx.Rollback();
			}
			Assert.AreEqual(3, o.Read());

			using (var tx = (Transaction)StmTransaction.BeginTransaction())
			{
				o.Write(4);
				tx.Rollback();
				tx.Commit();
			}
			Assert.AreEqual(3, o.Read());

			using (var tx = (Transaction)StmTransaction.BeginTransaction())
			{
				o.Write(5);
				tx.Rollback();
				tx.Rollback();
			}
			Assert.AreEqual(3, o.Read());
		}
	}
}

#endif