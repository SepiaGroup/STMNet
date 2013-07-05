using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using STM;

namespace STMTest
{
	[TestClass]
	public class StressTests
	{
		private List<StmObject<int>> StmInts = new List<StmObject<int>>();
		private const int nbrStms = 10;

		private void ReadFromStm<T>(StmObject<T> stm, AutoResetEvent are)
		{
			using (Stm.BeginTransaction())
			{
				stm.Read();
			}

			are.Set();
		}

		private void WriteToStm<T>(StmObject<T> stm, T newValue, AutoResetEvent are)
		{

			using (Stm.BeginTransaction())
			{
				stm.Write(newValue);
			}

			are.Set();
		}

		private void DoAction(AutoResetEvent doAre)
		{

			var loop = 100;

			var random = new Random();
			var randomStm = random.Next(nbrStms);

			var are = new AutoResetEvent(false);

			for (var i = 0; i < loop; i++)
			{
				var t = random.Next(2) == 0 ?
							new Thread(() => ReadFromStm(StmInts[random.Next(nbrStms)], are))
							: new Thread(() => WriteToStm(StmInts[random.Next(nbrStms)], random.Next(100), are));

				t.Start();

				are.WaitOne();
			}

			doAre.Set();
		}

		[TestMethod]
		public void FiveThreadTest()
		{
			const int nbrThreads = 5;

			for (var i = 0; i < nbrStms; i++)
			{
				StmInts.Add(Stm.CreateObject(i));
			}

			var s = new Stopwatch();
			var Ares = new List<AutoResetEvent>();

			s.Start();

			
			for (var i = 0; i < nbrThreads; i++)
			{
				var are = new AutoResetEvent(false);
				Ares.Add(are);
				new Thread(() => DoAction(are)).Start();
			}

			foreach (var a in Ares)
			{
				a.WaitOne();
			}
			
			s.Stop();

			Debug.WriteLine("Time {0}millisecondds", s.ElapsedMilliseconds);

		}
	}
}
