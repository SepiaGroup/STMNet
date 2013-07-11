using System;
using System.Diagnostics;
using System.Threading;

namespace STM
{
	public static class Stm
	{

		[ThreadStatic]
		private static Transaction _trasaction;

		public static Transaction Transaction
		{
			get { return _trasaction; }
		}

		#region BeginTransaction()

		public static Transaction BeginTransaction()
		{
			return BeginTransaction(null);
		}

		public static Transaction BeginTransaction(RetryDelegate retryDelegate)
		{
			_trasaction = new Transaction(retryDelegate);

			return _trasaction;
		}

		#endregion

		# region CreateObject

		public static StmObject<T> CreateObject<T>(T initialValue)
		{
			return new StmObject<T>(initialValue);
		}

		#endregion
	}
}
