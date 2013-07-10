using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using STM;

namespace TestSTMNet
{
	// internal value/version set


	[TestClass]
	public class TestNstmObject
	{
		[TestMethod]
		public void TestNonTxReadWrite()
		{
			var o = StmTransaction.CreateObject<int>();
			o.Write(1);
			Assert.AreEqual(1, o.Read(StmReadOption.ReadOnly));
		}


		[TestMethod]
		public void TestVersion()
		{
			var o = StmTransaction.CreateObject<int>();
			Assert.AreEqual(0, o.Version);
			o.Write(1);
			Assert.AreEqual(1, o.Version);

			using (var tx = StmTransaction.BeginTransaction())
			{
				o.Write(2);
				Assert.AreEqual(1, o.Version);
				tx.Commit();
			}
			Assert.AreEqual(2, o.Version);

			using (var tx = StmTransaction.BeginTransaction())
			{
				o.Write(3);
				Assert.AreEqual(2, o.Version);
			}
			Assert.AreEqual(2, o.Version);
		}


		[TestMethod]
		public void TestReadMultiple()
		{
			var o = StmTransaction.CreateObject(1);
			using (var tx = StmTransaction.BeginTransaction())
			{
				Assert.AreEqual(1, o.Read(StmReadOption.ReadOnly));
				Assert.AreEqual(1, o.Read(StmReadOption.ReadOnly));
			}
		}


		[TestMethod]
		public void TestCloneScalarValues()
		{
			var oString = StmTransaction.CreateObject("hello");
			var oDouble = StmTransaction.CreateObject(3.14);
			var oDateTime = StmTransaction.CreateObject(new DateTime(2007, 6, 27));

			using (var tx = StmTransaction.BeginTransaction())
			{
				Assert.AreEqual("hello", oString.Read(StmReadOption.ReadWrite));
				Assert.AreEqual(3.14, oDouble.Read(StmReadOption.ReadWrite));
				Assert.AreEqual(new DateTime(2007, 6, 27), oDateTime.Read(StmReadOption.ReadWrite));
			}
		}


		[TestMethod]
		public void TestCloneCustomValueType()
		{
			var vt = new MyValueType {I = 1, S = "hello"};
			var oVt = StmTransaction.CreateObject(vt);

			using (var tx = StmTransaction.BeginTransaction())
			{
				var vt2 = oVt.Read(StmReadOption.ReadWrite);
				Assert.AreEqual(vt.I, vt2.I);
				Assert.AreEqual(vt.S, vt2.S);
			}
		}


		[TestMethod]
		public void TestCloneClass()
		{
			var c = new MyCloneableClass {I = 1, S = "hello"};
			var oC = StmTransaction.CreateObject(c);
			using (var tx = StmTransaction.BeginTransaction())
			{
				var c2 = oC.Read(StmReadOption.ReadWrite);
				Assert.AreNotEqual(c, c2);
				Assert.AreEqual(c.I, c2.I);
				Assert.AreEqual(c.S, c2.S);
			}

			using (var tx = StmTransaction.BeginTransaction())
			{
				var c2 = oC.Read(); // the default read option is "ReadWrite"
				Assert.AreNotEqual(c, c2);
			}
		}


		[TestMethod]
		public void TestNonCloneableClass()
		{
			try
			{
				var c = StmTransaction.CreateObject<MyNonCloneableClass>();
				Assert.Fail();
			}
			catch (TypeInitializationException ex)
			{
				Assert.AreEqual(typeof (InvalidCastException), ex.InnerException.GetType());
			}
			catch
			{
				Assert.Fail();
			}
		}
	}

	internal struct MyValueType
	{
		public int I;
		public string S;
	}

	internal class MyCloneableClass : ICloneable
	{
		public int I;
		public string S;

		#region ICloneable Members

		public object Clone()
		{
			var c = new MyCloneableClass {I = I, S = S};
			return c;
		}

		#endregion
	}

	internal class MyNonCloneableClass
	{
		public int I = 0;
	}
}