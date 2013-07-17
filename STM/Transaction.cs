﻿/* Validation matrix
 *  validate on each read
 *    and on commit:            serializable+cloneOnWrite (ReadOnly values on read, ReadOnly+ReadWrite values on commit)
 *  validate on commit only:    seriablizable+cloneOnRead (ReadOnly+ReadWrite values)
 *  validate on commit,
 *    but only readOnly
 *    objects:                  readCommitted+cloneOnRead (ReadOnly values)
 *  no validation:              readCommitted+cloneOnWrite
 * 
 *  Validation means checking if a value has been changed by another tx. If that´s the case
 *  a tx needs to abort since it (possibly) worked with inconsistent data.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using STM.Exceptions;

namespace STM
{
	public delegate void TransactionDelegate(Transaction transaction);
	public delegate bool RetryDelegate(Transaction transaction);

	public class Transaction : IDisposable
	{
		#region Data and Ctor

		public TransactionState State { get; private set; }

		private readonly TransactionLog _txLog = new TransactionLog();

		internal TransactionLog TransactionLog { get { return _txLog; } }

		private readonly TransactionDelegate _transactionDelegate;
		private readonly RetryDelegate _retryDelegate;

		public int RetryCount { get; private set; }

		internal Transaction()
		{
			State = TransactionState.Active;
			_transactionDelegate = null;
			_retryDelegate = null;
		}

		internal Transaction(TransactionDelegate transactionDelegate, RetryDelegate retryDelegate)
		{
			State = TransactionState.Active;
			_transactionDelegate = transactionDelegate;
			_retryDelegate = retryDelegate;
		}

		#endregion

		#region log access to tx values in txlog
		internal T LogRead<T>(StmObject<T> stmObject)
		{
			var txEntry = _txLog.GetObject(stmObject);
			if (txEntry == null)
			{
				// first read on object: create txlog entry...
				var newStmObject = stmObject.Clone();

				txEntry = TransactionLogEntry<T>.LogReadEntry(stmObject, newStmObject);
				_txLog.Add(txEntry);

				// return new value
				return newStmObject.Value;
			}

			if (IsValidVersion(txEntry))
			{
				return txEntry.NewObject.Value;
			}

			throw new StmObjectValueChangedException("StmObject value has been changed by another transaction since it was last read. (Use isolation level 'ReadCommitted' to allow such changes to happen.)");
		}

		private bool IsValidVersion<T>(TransactionLogEntry<T> txEntry)
		{
			//// validate on each read only if 
			////    a.) serializability is asked for
			////    b.) the object´s data working set
			////        could have changed since the last read. that´s only the case if clonemode=cloneOnWrite.
			////		  (no validation for PassingReadOnly reads!)

			//if (txEntry.ReadOption > StmReadOption.PassingReadOnly 
			//		&& _isolationLevel == StmTransactionIsolationLevel.Serializable 
			//		&& _cloneMode == StmTransactionCloneMode.CloneOnWrite)
			//{
			//	// isolevel "serializable": the value must not have changed since it was last read
			//	return stmObject.Version == txEntry.Version;
			//}

			//// isolevel "readcommitted": the value is allowed to be changed by another tx
			//return true;

			var versionId = txEntry.OriginalObject.Element.Version;
			//var loop = 0;
			//while (versionId is int == false && loop < 3)
			//{
			//	Thread.Sleep(300);
			//	loop++;
			//}

			bool waitMore;
			do
			{
				if (versionId is int)
				{
					return (int)versionId == (int)txEntry.OriginalVersionId;
				}

				waitMore = txEntry.OriginalObject.ResetEvent.Wait(1000);
				versionId = txEntry.OriginalObject.Element.Version;

			} while (versionId is int == false && waitMore);


			throw new StmObjectBusyException("StmObject busy could not read");
		}

		internal void LogWrite<T>(StmObject<T> stmObject, T newValue)
		{
			var newStmObject = new StmObject<T>(newValue);
			var txEntry = _txLog.GetObject(stmObject);

			if (txEntry == null)
			{
				txEntry = TransactionLogEntry<T>.LogWriteEntry(stmObject, newStmObject);
				_txLog.Add(txEntry);

				return;
			}

			txEntry.UpdateNewStmObject(newStmObject);
		}

		#endregion

		public bool Commit()
		{
			if (DoCommit())
			{
				return true;
			}

			if (_retryDelegate == null)
			{
				return false;
			}

			while (_retryDelegate(this))
			{
				if (DoCommit())
				{
					return true;
				}

				RetryCount++;
			}

			return false;
		}

		private bool DoCommit()
		{
			if (State == TransactionState.Active)
			{
				var acquireError = false;

				foreach (var element in _txLog.Transactions.Select(s => s.Value))
				{
					var acquireState = element.Acquire();

					if (acquireState == AcquireState.Failed)
					{
						acquireError = true;
						break;
					}

					if (acquireState != AcquireState.Acquired)
					{
						acquireError = true;
						break;
					}
				}

				if (acquireError)
				{
					foreach (var t in _txLog.Transactions)
					{
						t.Value.Release();
					}

					return false;
				}

				State = TransactionState.Committed;
				foreach (var t in _txLog.Transactions)
				{
					t.Value.Commit();
				}

				return true;
			}

			throw new TransactionNotActiveException("Transaction must be 'Active' in order to commit");
		}
		
		#region IDisposable Members

		public void Dispose()
		{
			Commit();
		}

		#endregion
	}
}
