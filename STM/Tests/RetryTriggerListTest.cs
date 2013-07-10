#if DEBUG

using System.Threading;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace STM.Core
{
	[TestClass]
	public partial class RetryTriggerList
	{

		[TestMethod]
		public void TestRegister()
		{
			object o1 = 1;
			object o2 = 2;

			var are1 = new AutoResetEvent(false);
			var are2 = new AutoResetEvent(false);
			
			RegisterRetry(o1, are1);
			Assert.AreEqual(1, _triggers.Count);
			Assert.IsTrue(_triggers.ContainsKey(o1));
			Assert.IsTrue(((Hashtable)_triggers[o1]).ContainsKey(are1));

			RegisterRetry(o2, are1);
			Assert.AreEqual(2, _triggers.Count);
			Assert.IsTrue(_triggers.ContainsKey(o2));
			Assert.IsTrue(((Hashtable)_triggers[o2]).ContainsKey(are1));

			RegisterRetry(o1, are2);
			Assert.AreEqual(2, _triggers.Count);
			Assert.IsTrue(((Hashtable)_triggers[o1]).ContainsKey(are2));
		}


		[TestMethod]
		public void TestRemove()
		{
			object o1 = 1;
			object o2 = 2;

			var are1 = new AutoResetEvent(false);
			var are2 = new AutoResetEvent(false);

			RegisterRetry(o1, are1);
			RegisterRetry(o2, are1);
			RegisterRetry(o1, are2);

			RemoveRetry(are2);
			Assert.AreEqual(2, _triggers.Count);
			Assert.IsTrue(((Hashtable)_triggers[o1]).ContainsKey(are1));
			Assert.IsFalse(((Hashtable)_triggers[o1]).ContainsKey(are2));
			Assert.IsFalse(((Hashtable)_triggers[o2]).ContainsKey(are2));

			RemoveRetry(are1);
			Assert.AreEqual(2, _triggers.Count);
			Assert.IsFalse(((Hashtable)_triggers[o1]).ContainsKey(are1));
			Assert.IsFalse(((Hashtable)_triggers[o2]).ContainsKey(are1));

			Assert.AreEqual(0, ((Hashtable)_triggers[o1]).Count);
			Assert.AreEqual(0, ((Hashtable)_triggers[o2]).Count);
		}


		[TestMethod]
		public void TestTake()
		{
			object o1 = 1;
			object o2 = 2;

			var are1 = new AutoResetEvent(false);
			var are2 = new AutoResetEvent(false);

			Assert.AreEqual(0, TakeAllRetries(o1).Count);

			RegisterRetry(o1, are1);
			RegisterRetry(o2, are1);
			RegisterRetry(o1, are2);

			Assert.AreEqual(2, TakeAllRetries(o1).Count);
			Assert.IsFalse(_triggers.ContainsKey(o1));
			Assert.AreEqual(1, TakeAllRetries(o2).Count);
			Assert.IsFalse(_triggers.ContainsKey(o2));
		}


		[TestMethod]
		public void TestNotifyRetriesForTrigger()
		{
			object o1 = 1;
			object o2 = 2;

			var are1 = new AutoResetEvent(false);
			var are2 = new AutoResetEvent(false);

			RegisterRetry(o1, are1);
			RegisterRetry(o2, are1);
			RegisterRetry(o1, are2);

			NotifyRetriesForTrigger(o1);

			// Not valid in STA which is the model for the test classes
			//Assert.IsTrue(AutoResetEvent.WaitAll(new WaitHandle[] { are1, are2 }, 0, false));

			
			foreach (var e in new WaitHandle[] {are1, are2}) 
				Assert.IsTrue(e.WaitOne(0, false));

			Assert.IsFalse(_triggers.ContainsKey(o1));
		}

		[TestMethod]
		public void TestWaitingRetriesSet()
		{
			object o1 = 1;
			object o2 = 2;

			var are1 = new AutoResetEvent(false);
			var are2 = new AutoResetEvent(false);
			var are3 = new AutoResetEvent(false);

			RegisterRetry(o1, are1);
			RegisterRetry(o2, are1);
			RegisterRetry(o1, are2);
			RegisterRetry(o2, are3);

			var wrs = new WaitingRetriesSet();
			wrs.Add(TakeAllRetries(o2));
			wrs.Add(TakeAllRetries(o1));

			Assert.AreEqual(0, _triggers.Count);

			wrs.NotifyAll();

			// Not valid in STA which is the model for the test classes
			//Assert.IsTrue(AutoResetEvent.WaitAll(new WaitHandle[] { are1, are2, are3 }, 0, false));

			foreach (var e in new WaitHandle[] { are1, are2, are3 })
				Assert.IsTrue(e.WaitOne(0, false));
		}
	}
}

#endif