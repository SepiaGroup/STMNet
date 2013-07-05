using System;

namespace STM
{
	public interface ITransaction : IDisposable
	{
		TransactionState State { get; }

		void Commit();
		bool Commit(bool throwExceptionOnFailure);
		void Rollback();
	}
}