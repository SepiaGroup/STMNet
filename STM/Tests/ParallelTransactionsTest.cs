#if DEBUG

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using STM;

namespace STM
{
	[TestClass]
	public class TestParallelTransactions
	{
		[TestMethod]
		public void TestWithoutCollision()
		{
			var o = (StmObject<int>)StmTransaction.CreateObject(0);

			using (var tx0 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
			using (var tx1 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
			{
				// simulate multithreading (i.e. object access with different tx) by using explicit tx
				Assert.AreEqual(0, o.Read(StmReadOption.ReadOnly, tx0));
				Assert.AreEqual(0, o.Read(StmReadOption.ReadOnly, tx1));

				Assert.IsTrue(tx1.Commit(false));
				Assert.IsTrue(tx0.Commit(false));
			}
		}


		[TestMethod]
		public void TestWithCollision()
		{
			var o = (StmObject<int>)StmTransaction.CreateObject(0);

			using (var tx0 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
			using (var tx1 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
			{
				Assert.AreEqual(0, o.Read(StmReadOption.ReadOnly, tx0));
				Assert.AreEqual(0, o.Read(StmReadOption.ReadOnly, tx1));

				o.Write(1, tx1);

				Assert.IsTrue(tx1.Commit(false));
				Assert.IsFalse(tx0.Commit(false));
			}

			Assert.AreEqual(1, o.Read());
		}


		[TestMethod]
		public void TestValidateOnRead()
		{
			var o = (StmObject<int>)StmTransaction.CreateObject(0);

			using (var tx0 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
			using (var tx1 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
			{
				Assert.AreEqual(0, o.Read(StmReadOption.ReadOnly, tx0));
				Assert.AreEqual(0, o.Read(StmReadOption.ReadOnly, tx1));

				o.Write(1, tx1);

				Assert.IsTrue(tx1.Commit(false));

				try
				{
					o.Read(StmReadOption.ReadOnly, tx0);
					Assert.Fail();
				}
				catch (Exception ex)
				{
					Assert.IsTrue(typeof(NstmValidationFailedException) == ex.GetType());
				}

				tx0.Rollback();
			}
		}


		[TestMethod]
		public void TestValidateOnPassingRead()
		{
			var o = (StmObject<int>)StmTransaction.CreateObject(0);

			using (var tx0 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
			using (var tx1 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
			{
				Assert.AreEqual(0, o.Read(StmReadOption.PassingReadOnly, tx0));
				Assert.AreEqual(0, o.Read(StmReadOption.ReadOnly, tx1));

				o.Write(1, tx1);

				Assert.IsTrue(tx1.Commit(false));

				Assert.AreEqual(1, o.Read(StmReadOption.PassingReadOnly, tx0));
				// no validation for PassingReadOnly!

				tx0.Rollback();
			}
		}


		[TestMethod]
		public void TestValidateOnCommit()
		{
			var o = (StmObject<int>)StmTransaction.CreateObject(0);

			using (var tx0 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnRead))
			using (var tx1 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
			{
				Assert.AreEqual(0, o.Read(StmReadOption.ReadOnly, tx0));
				Assert.AreEqual(0, o.Read(StmReadOption.ReadOnly, tx1));

				o.Write(1, tx1);

				Assert.IsTrue(tx1.Commit(false));

				Assert.AreEqual(0, o.Read(StmReadOption.ReadOnly, tx0));

				Assert.IsFalse(tx0.Commit(false));
			}
		}


		[TestMethod]
		public void TestValidateOnCommitForReadCommitted()
		{
			var o = (StmObject<int>)StmTransaction.CreateObject(0);

			using (var tx0 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.ReadCommitted, StmTransactionCloneMode.CloneOnRead))
			using (var tx1 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
			{
				Assert.AreEqual(0, o.Read(StmReadOption.ReadOnly, tx0));
				Assert.AreEqual(0, o.Read(StmReadOption.ReadOnly, tx1));

				o.Write(1, tx1);
				Assert.AreEqual(0, o.Read(StmReadOption.ReadOnly, tx0));

				Assert.IsTrue(tx1.Commit(false));
				Assert.IsFalse(tx0.Commit(false));
			}
		}


		[TestMethod]
		public void TestWithPassingReadCollision()
		{
			var o = (StmObject<int>)StmTransaction.CreateObject(0);

			using (var tx0 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
			using (var tx1 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
			{
				Assert.AreEqual(0, o.Read(StmReadOption.PassingReadOnly, tx0));
				Assert.AreEqual(0, o.Read(StmReadOption.ReadOnly, tx1));

				o.Write(1, tx1);

				Assert.IsTrue(tx1.Commit(false));
				Assert.IsTrue(tx0.Commit(false));
			}

			Assert.AreEqual(1, o.Read());
		}


		[TestMethod]
		public void TestWithSerializableOverwrite()
		{
			var o = (StmObject<int>)StmTransaction.CreateObject(0);

			using (var tx0 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
			using (var tx1 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
			{
				Assert.AreEqual(0, o.Read(StmReadOption.ReadOnly, tx0));
				Assert.AreEqual(0, o.Read(StmReadOption.ReadOnly, tx1));

				o.Write(1, tx1);
				o.Write(2, tx0);

				Assert.IsTrue(tx1.Commit(false));
				Assert.IsFalse(tx0.Commit(false));
			}

			Assert.AreEqual(1, o.Read());
		}


		[TestMethod]
		public void TestWithReadCommittedOverwrite()
		{
			var o = (StmObject<int>)StmTransaction.CreateObject(0);

			using (var tx0 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.ReadCommitted, StmTransactionCloneMode.CloneOnWrite))
			using (var tx1 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
			{
				Assert.AreEqual(0, o.Read(StmReadOption.ReadOnly, tx0));
				Assert.AreEqual(0, o.Read(StmReadOption.ReadOnly, tx1));

				o.Write(1, tx1);
				o.Write(2, tx0);

				Assert.IsTrue(tx1.Commit(false));
				Assert.IsTrue(tx0.Commit(false));
			}

			Assert.AreEqual(2, o.Read());
		}


		[TestMethod]
		public void TestAgainstDeadlocksOnCommit()
		{
			var objects = new List<StmObject<int>>();
			for (var i = 0; i < 3; i++)
				objects.Add((StmObject<int>)StmTransaction.CreateObject(i));

			using (var tx0 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
			using (var tx1 = StmTransaction.BeginTransaction(StmTransactionScopeOption.RequiresNew, StmTransactionIsolationLevel.Serializable, StmTransactionCloneMode.CloneOnWrite))
			{
				// write to the objects in different order and with just an overlap so that each tx could commit some objects - but not others
				// that way it will lock some successfully and fail on others
				for (var i = 0; i < 2; i++)
					objects[i].Write(i + 1, tx0);

				for (var i = 2; i >= 1; i--)
					objects[i].Write(i * 10, tx1);

				Assert.IsTrue(tx1.Commit(false));
				Assert.IsFalse(tx0.Commit(false));
			}

			Assert.AreEqual(0, objects[0].Read());
			Assert.AreEqual(10, objects[1].Read());
			Assert.AreEqual(20, objects[2].Read());
		}
	}
}

#endif
