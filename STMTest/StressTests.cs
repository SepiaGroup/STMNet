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

		private void ReadFromStm<T>(StmObject<T> stm)
		{
			using (Stm.BeginTransaction())
			{
				stm.Read();
			}
		}

		private void WriteToStm<T>(StmObject<T> stm, T newValue)
		{

			using (Stm.BeginTransaction())
			{
				stm.Write(newValue);
			}
		}

		private void DoAction()
		{
			var loop = 1000;

			var random = new Random();

			for (var i = 0; i < loop; i++)
			{
				if (random.Next(2) == 0)
				{
					ReadFromStm(StmInts[random.Next(nbrStms)]);
				}
				else
				{
					WriteToStm(StmInts[random.Next(nbrStms)], random.Next(100));
				}
			}
		}

		[TestMethod]
		public void FiveThreadTest()
		{
			const int nbrThreads = 5;

			var threadPool = new List<Thread>();

			for (var i = 0; i < nbrThreads; i++)
			{
				threadPool.Add(new Thread(() => DoAction()));
			}


			for (var i = 0; i < nbrStms; i++)
			{
				StmInts.Add(Stm.CreateObject(i));
			}

			var s = new Stopwatch();
			
			s.Start();

			foreach (var t in threadPool)
			{
				t.Start();
			}

			foreach (var t in threadPool)
			{
				t.Join();
			}
			
			s.Stop();

			Debug.WriteLine("Time {0}millisecondds", s.ElapsedMilliseconds);
		}
	}
}
