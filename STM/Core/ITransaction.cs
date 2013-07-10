using System;

namespace STM.Core
{
	public interface ITransaction : IDisposable
	{
		StmTransactionIsolationLevel IsolationLevel { get; }
		StmTransactionCloneMode CloneMode { get; }
		TransactionState ActivityMode { get; }
		bool IsNested { get; }

		void Commit();
		bool Commit(bool throwExceptionOnFailure);
		void Rollback();
	}
}
