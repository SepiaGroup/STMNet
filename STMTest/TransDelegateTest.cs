using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using STM;

namespace STMTest
{
	[TestClass]
	public class TransDelegateTest
	{
		public StmObject<int> StmInt = Stm.CreateObject(1);

		public void TransDelegate(Transaction transaction)
		{
			using (transaction)
			{
				StmInt.Write(2);
			}
		}

		public bool RetryDelegate(Transaction transaction)
		{
			return transaction.RetryCount < 3;
		}

		[TestMethod]
		public void ExecuteTransTest()
		{
			var t = Stm.ExecuteTransaction(TransDelegate);

			Assert.IsTrue(StmInt.Value == 2);
		}
	}
}
