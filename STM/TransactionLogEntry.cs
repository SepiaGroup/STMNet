using System;
using System.Diagnostics;
using System.Threading;
using STM.Exceptions;

namespace STM
{
	internal class TransactionLogEntry<T> : ITransactionLogEntry
	{
		internal TransactionLogEntry(StmObject<T> originalObject, StmObject<T> newObject)
		{
			OriginalObject = originalObject;
			NewObject = newObject;

			var versionId = originalObject.Element.Version;
			if (versionId is int)
			{
				OriginalVersionId = versionId;
			}
			else
			{
				// this object is being updated. wait until the update is complete or error out.
				bool waitMore;
				do
				{
					waitMore = OriginalObject.ResetEvent.Wait(1000);

					versionId = originalObject.Element.Version;

				} while ( !(versionId is int)  && waitMore);

				if (versionId is int)
				{
					OriginalVersionId = versionId;
				}
				else
				{
					throw new InvalidStmObjectStateException("StmObject is not in valid state.");
				}
			}
		}

		internal StmObject<T> OriginalObject;
		internal StmObject<T> NewObject;

		public object OriginalVersionId { get; private set; }

		internal void UpdateNewStmObject(StmObject<T> newStmObject)
		{
			NewObject = newStmObject;
		}

		public bool IsAcquired
		{
			get { return OriginalObject.Element.Version == this; }
		}

		public AcquireState Acquire()
		{
			var acquireState = GetAcquireState();

			switch (acquireState)
			{
				case AcquireState.Acquired:
					break;

				case AcquireState.Failed:
					break;

				case AcquireState.Busy:
					bool waitMore;
					do
					{
						waitMore = OriginalObject.ResetEvent.Wait(1000);

						acquireState = GetAcquireState();

					} while (acquireState == AcquireState.Busy && waitMore);

					return acquireState;
			}

			return acquireState;
		}

		private AcquireState GetAcquireState()
		{
			Interlocked.CompareExchange(ref OriginalObject.Element.Version, this, OriginalVersionId);

			if (OriginalObject.Element.Version == this)
			{
				OriginalObject.ResetEvent.Reset();
				return AcquireState.Acquired;
			}

			if (OriginalObject.Element.Version is int)
			{
				return AcquireState.Failed;
			}

			return AcquireState.Busy;
		}

		public int UniqueId
		{
			get { return OriginalObject.UniqueId; }
		}
		
		public void Commit()
		{
			if (!IsAcquired)
			{
				throw new CommitBeforeAcquireException("StmObject must be acquired before commit.");
			}

			Interlocked.Exchange(ref OriginalObject.Element, NewObject.Element);

			OriginalObject.ResetEvent.Set();
		}

		public void Release()
		{
			Interlocked.CompareExchange(ref OriginalObject.Element.Version, OriginalVersionId, this);

			OriginalObject.ResetEvent.Set();
		}
	}
}
