using System;

namespace STM
{
	internal interface ITransactionLogEntry
	{
		bool IsAcquired { get; }
		AcquireState Acquire();
		int UniqueId { get; }
		void Commit();
		void Release();
		ConflictType ConflictType { get; }
	}
}
