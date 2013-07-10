/* Validation matrix
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
using STM.Core;

namespace STM.Core
{

	internal class Transaction : ITransaction
	{
		#region Data and Ctor
		private readonly Transaction _txParent;

		private readonly StmTransactionIsolationLevel _isolationLevel;
		private readonly StmTransactionCloneMode _cloneMode;

		private TransactionState _activityMode;

		private TransactionLog _txLog;


		internal Transaction(ITransaction txParent, StmTransactionIsolationLevel isolationLevel, StmTransactionCloneMode cloneMode)
		{
			_txParent = (Transaction)txParent;
			if (txParent != null)
			{
				// need to clone tx log so the inner tx can see all changes already made to the memory by outer tx
				//_txLog = (TransactionLog)_txParent._txLog.Clone();
				throw new Exception("Not implemented yet");
			}
			else
			{
				_txLog = new TransactionLog();
			}

			_isolationLevel = isolationLevel;
			_cloneMode = cloneMode;

			_activityMode = TransactionState.Active;
		}
		#endregion
		
		#region log access to tx values in txlog
		internal T LogRead<T>(StmObject<T> stmObject, StmReadOption readOption)
		{
			//lock (this)
			//{
				//lock (instance)
				//{
					var txEntry = _txLog.GetObject(stmObject);
					if (txEntry == null)
					{
						// first read on object: create txlog entry...

						var newStmObject = stmObject.CloneValue();

						txEntry = new TransactionLogEntry<T>(stmObject, newStmObject, readOption);
						_txLog.Add(txEntry);
						
						// return new value
						return newStmObject.Value;
					}

					if (ValidateObjectValueOnRead(stmObject, txEntry))
					{
						return txEntry.NewObject.Value;
					}
	
					throw new NstmValidationFailedException("Cannot read NSTM transactional object value! Value has been changed by another transaction since it was last read. (Use isolation level 'ReadCommitted' to allow such changes to happen.)");
				//}
			//}
		}
		
		//// Log read access to NstmTransactionalAttribute objects
		//internal object LogRead(INstmVersioned instance, string fieldname, object currentValue)
		//{
		//	lock (this)
		//	{
		//		lock (instance)
		//		{
		//			var txEntry = (FieldlistTransactionLogEntry)_txLog[instance];
		//			if (txEntry == null)
		//			{
		//				txEntry = new FieldlistTransactionLogEntry(instance);
		//				_txLog.Add(txEntry);
		//				txEntry.ReadOption = StmReadOption.ReadOnly;

		//				if (_cloneMode == StmTransactionCloneMode.CloneOnRead)
		//				{
		//					// THINK ABOUT - is cloning necessary for field values?
		//					// ANSWER: no. field values are either scalar, then adding them to tempFieldValues is a natural copy.
		//					//         or they are an object reference. then their value is the reference which gets copied.
		//					//         the object referenced can of course be transactional itself.
		//					txEntry.TempFieldvalues.Add(fieldname, currentValue);
		//					return txEntry.TempFieldvalues[fieldname];
		//				}

		//				return currentValue;
		//			}
		//			if (txEntry.TempFieldvalues.ContainsKey(fieldname))
		//			{
		//				return txEntry.TempFieldvalues[fieldname];
		//			}
		//			if (ValidateObjectValueOnRead(instance, txEntry))
		//			{
		//				if (txEntry.ReadOption == StmReadOption.ReadWrite)
		//				{
		//					// if the readoption was stepped up from a lower level and there is no
		//					// clone yet, copy the current value now
		//					// THINK ABOUT - is cloning necessary for field values? ANSER: see above.
		//					txEntry.TempFieldvalues.Add(fieldname, currentValue);
		//					return txEntry.TempFieldvalues[fieldname];
		//				}

		//				return currentValue;
		//			}

		//			throw new NstmValidationFailedException("Cannot read NSTM transactional object value! Value has been changed by another transaction since it was last read. (Use isolation level 'ReadCommitted' to allow such changes to happen.)");
		//		}
		//	}
		//}

		private bool ValidateObjectValueOnRead<T>(IStmObject<T> stmObject, ITransactionLogEntry txEntry)
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


			var versionId = stmObject.Version;
			var loop = 0;
			while (versionId is int == false && loop < 3)
			{
				Thread.Sleep(333);
				loop++;
			}

			if (versionId is int)
			{
				return (int) versionId == txEntry.OriginalVersionId;
			}
			
			throw new Exception("StmObject busy could not read");
		}


		// Log write for INstmObject<T> objects
		internal void LogWrite<T>(StmObject<T> stmObject, T newValue)
		{
			////lock (this)
			////{
			//	var txEntry = (TransactionLogEntry)_txLog[instance];
			//	if (txEntry == null)
			//	{
			//		lock (instance)
			//		{
			//			txEntry = new TransactionLogEntry(instance);
			//			_txLog.Add(txEntry);
			//		}
			//	}

			//	txEntry.ReadOption = StmReadOption.ReadWrite;
			//	txEntry.TempValue = newValue;
			////}

			var newStmObject = new StmObject<T>(newValue);
			var txEntry = _txLog.GetObject(stmObject);
			if (txEntry == null)
			{
				txEntry = new TransactionLogEntry<T>(stmObject, newStmObject);
				_txLog.Add(txEntry);

				return;
			}

			txEntry.UpdateNewStmObject(newStmObject);
		}


		//// Log write access to NstmTransactionalAttribute objects from outside
		//internal void LogWrite(INstmVersioned instance, string fieldname, object newValue)
		//{
		//	lock (this)
		//	{
		//		var txEntry = (FieldlistTransactionLogEntry)_txLog[instance];
		//		if (txEntry == null)
		//		{
		//			lock (instance)
		//			{
		//				txEntry = new FieldlistTransactionLogEntry(instance);
		//				_txLog.Add(txEntry);
		//			}
		//		}

		//		txEntry.ReadOption = StmReadOption.ReadWrite;
		//		txEntry.TempFieldvalues[fieldname] = newValue;
		//	}
		//}

		// Log write access to NstmTransactionalAttribute objects during tx commit

		internal void LogWrite<T>(StmObject<T> instance, Dictionary<string, ITransactionLogEntry> newFieldValues)
		{
			//lock (this)
			//{
			//	var txEntry = (FieldlistTransactionLogEntry)_txLog[instance];
			//	if (txEntry == null)
			//	{
			//		lock (instance)
			//		{
			//			txEntry = new FieldlistTransactionLogEntry(instance);
			//			_txLog.Add(txEntry);
			//		}
			//	}

			//	txEntry.ReadOption = StmReadOption.ReadWrite;
			//	txEntry.TempFieldvalues = newFieldValues;
			//}
		}

		#endregion
		
		#region internal properties

		public int LogCount
		{
			get
			{
				lock (this)
				{
					return _txLog.Count;
				}
			}
		}
		
		#endregion

		#region INstmTransaction Properties

		public StmTransactionIsolationLevel IsolationLevel
		{
			get { return _isolationLevel; }
		}

		public StmTransactionCloneMode CloneMode
		{
			get { return _cloneMode; }
		}

		public TransactionState ActivityMode
		{
			get { return _activityMode; }
		}

		public bool IsNested
		{
			get { return _txParent != null; }
		}
		#endregion

		#region commit/rollback

		public void Commit()
		{
			Commit(true);
		}

		public bool Commit(bool throwExceptionOnFailure)
		{
			if (_activityMode == TransactionState.Active)
			{
				lock (this)
				{
					if (PrepareLogEntries(throwExceptionOnFailure))
					{
						CommitLogEntries();
					}
					else
					{
						return false;
					}
				}
			}

			return true;
		}

		private bool PrepareLogEntries(bool throwExceptionOnFailure)
		{
			var txStack = new ThreadTransactionStack();
			if (txStack.Peek() != this)
			{
				if (throwExceptionOnFailure)
				{
					throw new InvalidOperationException("STM transaction to be committed is not the current transaction! Check for overlapping transaction Commit()/Abort(). Recommendation: Create transactions within the scope of a using() statement.");
				}

				return false;
			}


			// Validation / Preparation
			foreach (var logEntry in _txLog.Transactions)
			{
				//switch (logEntry.ReadOption)
				//{
				//	case StmReadOption.PassingReadOnly:
				//		// do nothing; we don´t care if anything has been changed by another tx
				//		break;

				//	case StmReadOption.ReadOnly:
				//		if (!ValidateLogEntryOnCommit(logEntry, throwExceptionOnFailure, false))
				//		{
				//			return false; // abort commit, if validation failed. the tx then has been rolled back anyway.
				//		}

				//		break;

				//	case StmReadOption.ReadWrite:
				//		if (!ValidateLogEntryOnCommit(logEntry, throwExceptionOnFailure, true))
				//		{
				//			return false;
				//		}

				//		logEntry.IsLocked = true;
				//		break;

				//	//TODO: THINK ABOUT - should there be a fourth option "WriteOnly" with no validation? look at validation matrix!
				//}

				if (!logEntry.Aquire())
				{

					break;
				}
			}

			_activityMode = TransactionState.Committing;
			return true;
		}
		
		private void CommitLogEntries()
		{
			lock (this)
			{
				if (_activityMode == TransactionState.Committing)
				{
					// list of retries to notify about changes committed by the tx
					var waitingRetriesForTx = new RetryTriggerList.WaitingRetriesSet();

					// Commit
					//      for all acquired log entries commit the temp value to the object
					//      no error must happen during this phase!
					foreach (ITransactionLogEntry logEntry in _txLog.SortedEntries.Where(logEntry => logEntry.IsLocked))
					{
						if (_txParent == null)
						{
							// commit new value to object
							logEntry.Commit();
						}
						else
						{
							// in nested tx don´t commit new value to object itself, 
							// but just to enclosing tx´s log.
							if (logEntry is TransactionLogEntry)
							{
								_txParent.LogWrite((IStmObject) logEntry.Instance, ((TransactionLogEntry) logEntry).TempValue);
							}
							else
							{
								_txParent.LogWrite(logEntry.Instance, ((FieldlistTransactionLogEntry) logEntry).TempFieldvalues);
							}
						}

						Monitor.Exit(logEntry.Instance);
						logEntry.IsLocked = false;

						// remember all retries to notify about this change
						waitingRetriesForTx.Add(RetryTriggerList.Instance.TakeAllRetries(logEntry.Instance));
					}

					_activityMode = TransactionState.Committed;
					new ThreadTransactionStack().Remove(this);

					// the tx is committed, now notify all retries waiting for changes made to transactional values
					// by this tx
					waitingRetriesForTx.NotifyAll();
				}
			}
		}


		private bool ValidateLogEntryOnCommit(ITransactionLogEntry logEntry, bool throwExceptionOnFailure, bool keepLogEntryLockedOnSuccess)
		{
			// check if value has not been changed by another tx since it was originally read
			// (see validation matrix at top of file)
			Monitor.Enter(logEntry.Instance);
			if ((_isolationLevel == StmTransactionIsolationLevel.Serializable 
					|| (_isolationLevel == StmTransactionIsolationLevel.ReadCommitted 
						&& _cloneMode == StmTransactionCloneMode.CloneOnRead 
						&& logEntry.ReadOption == StmReadOption.ReadOnly)
				 ) && logEntry.Version != logEntry.Instance.Version)

			{
				// we´ve read another version than the current. some other tx must have
				// committed a change to the object. we need to abort this tx.
				Monitor.Exit(logEntry.Instance);
				UnlockAcquiredLogEntries();
				Rollback();

				if (throwExceptionOnFailure)
				{
					throw new NstmValidationFailedException("Cannot commit NSTM transaction! Transactional object has been changed by another transaction. (Use the transaction isolation level 'ReadCommitted' to allow for such changes to be ok.)");
				}
				return false;
				
			}

			if (!keepLogEntryLockedOnSuccess)
			{
				Monitor.Exit(logEntry.Instance);
			}

			return true;
		}


		private void UnlockAcquiredLogEntries()
		{
			foreach (ITransactionLogEntry logEntry in _txLog.SortedEntries)
				if (logEntry.IsLocked)
				{
					Monitor.Exit(logEntry.Instance);
					logEntry.IsLocked = false;
				}
		}


		public void Rollback()
		{
			if (_activityMode == TransactionState.Active)
			{
				lock (this)
				{
					_txLog = new TransactionLog();

					_activityMode = TransactionState.Aborted;
					new ThreadTransactionStack().Remove(this);
				}
			}
			else
			{
				// this is to help clearing the tx stack in case tx have been closed in the wrong order
				// and someone is now calling rollback in the correct order
				var txStack = new ThreadTransactionStack();
				if (txStack.Peek() == this)
				{
					txStack.Pop();
				}
			}
		}


		internal bool ValidateForRetry(AutoResetEvent areRetry)
		{
			lock (this)
			{
				// register the retry waithandle with all values touched by the tx
				// if one of the values later is changed the retry waithandle is signaled
				var txIsValid = true;
				foreach (var logEntry in _txLog.SortedEntries)
				{
					txIsValid = txIsValid && logEntry.Version == logEntry.Instance.Version;
					if (areRetry != null)
					{
						RetryTriggerList.Instance.RegisterRetry(logEntry.Instance, areRetry);
					}
				}
				return txIsValid;
			}
		}


		//internal void RollbackForRetry(AutoResetEvent areRetry)
		//{
		//    if (this.activityMode == NstmTransactionActivityMode.Active)
		//    {
		//        lock (this)
		//        {
		//            // register the retry waithandle with all values touched by the tx
		//            // if one of the values later is changed the retry waithandle is signaled
		//            foreach (Infrastructure.TransactionLogEntry logEntry in this.txLog.SortedEntries)
		//                if (logEntry.ReadOption != NstmReadOption.PassingReadOnly)
		//                    Infrastructure.RetryTriggerList.Instance.RegisterRetry(logEntry.Instance, areRetry);

		//            // now the real rollback can be done
		//            this.Rollback();
		//        }
		//    }
		//}
		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			Rollback();
		}

		#endregion
	}
}
