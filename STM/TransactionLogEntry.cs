using System;
using System.Diagnostics;
using System.Threading;
using STM.Exceptions;

namespace STM
{
	internal class TransactionLogEntry<T> : ITransactionLogEntry
	{
		private TransactionLogEntry(StmObject<T> originalObject, StmObject<T> newObject, bool incrementVersionId)
		{
			OriginalObject = originalObject;
			NewObject = newObject;
			
			var versionId = originalObject.Element.Version;
			if (versionId is int)
			{
				OriginalVersionId = versionId;

				newObject.Element.Version = incrementVersionId ? (int)versionId + 1 : (int)versionId;
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

					newObject.Element.Version = incrementVersionId ? (int)versionId + 1 : (int)versionId; 
				}
				else
				{
					throw new InvalidStmObjectStateException("StmObject is not in valid state.");
				}
			}
		}

		internal static TransactionLogEntry<T> LogReadEntry(StmObject<T> originalObject, StmObject<T> newObject)
		{
			return new TransactionLogEntry<T>(originalObject, newObject, false);
		}

		internal static TransactionLogEntry<T> LogWriteEntry(StmObject<T> originalObject, StmObject<T> newObject)
		{
			return new TransactionLogEntry<T>(originalObject, newObject, true);
		}

		internal StmObject<T> OriginalObject;
		internal StmObject<T> NewObject;
		public bool HasConflict { get; private set; }
		public ConflictType ConflictType
		{
			get
			{
				if (!HasConflict)
				{
					return ConflictType.None;
				}

				return (int) OriginalVersionId == (int) NewStm.Element.Version ? ConflictType.Read : ConflictType.Write;
			}
		}


		public object OriginalVersionId { get; private set; }

		internal void UpdateNewStmObject(StmObject<T> newStmObject)
		{
			newStmObject.Element.Version = (int) OriginalVersionId + 1;
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

					break;
			}

			HasConflict = acquireState == AcquireState.Acquired;

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

		public StmObject<T> OriginalStm
		{
			get { return OriginalObject; }
		}

		public StmObject<T> NewStm
		{
			get { return NewObject; }
		}
	}
}
