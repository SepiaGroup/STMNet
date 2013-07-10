using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using STM;

namespace TestSTMNet
{
	[TestClass]
	public class TestErrorReproduction
	{
		[TestMethod]
		public void ReproduceEmptyTempvalue()
		{
			// s. NstmList.RemoveAt(): After commit the value of some objects is null because the logentry tempvalue seems to not have been set despite ReadWrite access

			var o = StmTransaction.CreateObject<MyInt>();
			var i = new MyInt {Value = 1};
			o.Write(i);

			using (var tx = StmTransaction.BeginTransaction(
													StmTransactionScopeOption.RequiresNested,
													StmTransactionIsolationLevel.ReadCommitted,
													StmTransactionCloneMode.CloneOnWrite))
			{
				var j = o.Read(StmReadOption.PassingReadOnly);
				Assert.AreEqual(i, j);

				j = o.Read(StmReadOption.ReadWrite);
				Assert.AreNotEqual(i, j);
				j.Value = 2;

				tx.Commit();
			}

			Assert.AreEqual(2, o.Read().Value);
		}

		private class MyInt : ICloneable
		{
			public int Value;

			#region ICloneable Members

			public object Clone()
			{
				var o = new MyInt {Value = Value};
				return o;
			}

			#endregion
		}
	}
}
