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
			_trasaction = new Transaction(new TransactionOptions());

			return _trasaction;
		}

		public static Transaction BeginTransaction(TransactionOptions options)
		{
			_trasaction = new Transaction(options);

			return _trasaction;
		}

		public static Transaction ExecuteTransaction(TransactionDelegate transactionDelegate)
		{
			return ExecuteTransaction(transactionDelegate, null, new TransactionOptions());
		}

		public static Transaction ExecuteTransaction(TransactionDelegate transactionDelegate, TransactionOptions options)
		{
			return ExecuteTransaction(transactionDelegate, null, options);
		}

		public static Transaction ExecuteTransaction(TransactionDelegate transactionDelegate,  RetryDelegate retryDelegate)
		{
			return ExecuteTransaction(transactionDelegate, retryDelegate, new TransactionOptions());
		}

		public static Transaction ExecuteTransaction(TransactionDelegate transactionDelegate, RetryDelegate retryDelegate, TransactionOptions options)
		{
			_trasaction = new Transaction(transactionDelegate, retryDelegate);


			_trasaction.TransactionDelegate.Invoke(_trasaction);

			while (_trasaction.State == TransactionState.Aborted && _trasaction.RetryDelegate != null && _trasaction.RetryDelegate.Invoke(_trasaction))
			{
				_trasaction.TransactionDelegate.Invoke(_trasaction);
			}

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
