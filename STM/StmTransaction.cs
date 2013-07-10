using System;
using System.Threading;
using STM.Core;

namespace STM
{
	#region enum definitions
	public enum StmTransactionScopeOption
	{
		Required,
		RequiresNew,
		RequiresNested, // default
		RequiredOrNested
	}

	public enum StmTransactionIsolationLevel
	{
		ReadCommitted = 0,  // data read in tx0 can be changed by another tx1 for tx0 to still commit successfully
		Serializable = 1    // default; data read in tx0 must not be changed by another tx1 for tx0 to commit successfully; this pertains to data read and/or written in tx0
	}

	public enum StmTransactionCloneMode
	{
		CloneOnWrite = 0, // default - data of a tx0 object will be automatically cloned only if opened in ReadWrite mode; thus tx0 might see changes in data committed by other tx if it reads them several times
		CloneOnRead = 1   // this ensures that tx0 does not even see any commtted changes by another tx on data it has read
	}

	#endregion


	public static class StmTransaction
	{

		#region BeginTransaction()
		public static ITransaction BeginTransaction()
		{
			return BeginTransaction(
				StmTransactionScopeOption.RequiresNested,
				StmTransactionIsolationLevel.Serializable,
				StmTransactionCloneMode.CloneOnRead);
		}

		public static ITransaction BeginTransaction(StmTransactionIsolationLevel isolationLevel)
		{
			return BeginTransaction(
				StmTransactionScopeOption.RequiresNested,
				isolationLevel,
				StmTransactionCloneMode.CloneOnWrite);
		}


		public static ITransaction BeginTransaction(StmTransactionScopeOption scopeOption, StmTransactionIsolationLevel isolationLevel, StmTransactionCloneMode cloneMode)
		{
			bool newTxCreated;
			return BeginTransaction(scopeOption, isolationLevel, cloneMode, out newTxCreated);
		}


		public static ITransaction BeginTransaction(StmTransactionScopeOption scopeOption, StmTransactionIsolationLevel isolationLevel, StmTransactionCloneMode cloneMode, out bool newTxCreated)
		{
			ITransaction tx = null;
			newTxCreated = true;

			var txStack = new ThreadTransactionStack();
			switch (scopeOption)
			{
				// check if there is an active tx on the thread. if so, don´t start a new tx, but return the current one
				case StmTransactionScopeOption.Required:
					if (txStack.Count == 0)
						// no tx active, create a new one
						tx = CreateTransaction(txStack, null, isolationLevel, cloneMode);
					else
					{
						// there is an active tx...
						tx = txStack.Peek();
						if (tx.IsolationLevel < isolationLevel || tx.CloneMode < cloneMode)
							// ...but the current tx does not match the requirements; create a new tx (not nested)
							tx = CreateTransaction(txStack, null, isolationLevel, cloneMode);
						else
							newTxCreated = false;
					}
					break;

				// start a new independent tx in any case
				case StmTransactionScopeOption.RequiresNew:
					tx = CreateTransaction(txStack, null, isolationLevel, cloneMode);
					break;

				// start a new tx which is nested in the current one
				case StmTransactionScopeOption.RequiresNested:
					tx = CreateTransaction(txStack, txStack.Peek(), isolationLevel, cloneMode);
					break;

				// use existing tx if its compatible with current settings - otherwise create a nested tx
				case StmTransactionScopeOption.RequiredOrNested:
					if (txStack.Count == 0)
						// no existing tx - create a new one
						tx = CreateTransaction(txStack, null, isolationLevel, cloneMode);
					else
					{
						// there is a tx: check if its settings are ok...
						tx = txStack.Peek();
						if (tx.IsolationLevel < isolationLevel || tx.CloneMode < cloneMode)
							// ...the settings are not strict enough; create a new nested tx
							tx = CreateTransaction(txStack, tx, isolationLevel, cloneMode);
						else
							newTxCreated = false;
					}
					break;
			}

			return tx;
		}

		internal static ITransaction CreateTransaction(ThreadTransactionStack txStack, ITransaction txParent, StmTransactionIsolationLevel isolationLevel, StmTransactionCloneMode copyMode)
		{
			var tx = new Transaction(txParent, isolationLevel, copyMode);
			
			txStack.Push(tx);

			return tx;
		}
		#endregion


		#region ExecuteAtomically()
		public static void ExecuteAtomically(ThreadStart task)
		{
			ExecuteAtomically(false, task);
		}

		public static void ExecuteAtomically(bool autoRetry, ThreadStart task)
		{

			ExecuteAtomically(StmTransactionScopeOption.RequiresNested,
							  StmTransactionIsolationLevel.Serializable,
							  StmTransactionCloneMode.CloneOnWrite,
							  autoRetry ? int.MaxValue : 0,
							  Timeout.Infinite,
							  Timeout.Infinite,
							  task);
		}

		public static void ExecuteAtomically(
			StmTransactionScopeOption scope,
			StmTransactionIsolationLevel isolationLevel,
			StmTransactionCloneMode cloneMode,
			bool autoRetry,
			ThreadStart task)
		{
			ExecuteAtomically(scope,
							  isolationLevel,
							  cloneMode,
							  autoRetry ? int.MaxValue : 0,
							  Timeout.Infinite,
							  Timeout.Infinite,
							  task);
		}

		public static void ExecuteAtomically(
			StmTransactionScopeOption scope,
			StmTransactionIsolationLevel isolationLevel,
			StmTransactionCloneMode cloneMode,
			int maxRetries,
			int sleepBeforeRetryMsec,
			int maxProcessingTimeMsec,
			ThreadStart task
			)
		{
			var retryAtAll = maxRetries > 0 || maxProcessingTimeMsec != Timeout.Infinite;
			var executionStart = DateTime.Now;
			var nRetries = 0;

			while (true)
			{
				var tx = (Transaction)BeginTransaction(scope, isolationLevel, cloneMode);
				try
				{
					task.Invoke();

					tx.Commit();
					break;
				}
				catch (NstmRetryException ex)
				{
					var areRetry = new AutoResetEvent(false);
					if (tx.ValidateForRetry(areRetry))
					{
						// only wait for changes to working set if validation succeeded
						// because if it did not succeed then the manual retry was probably thrown on the basis of a wrong assumption
						// this is to catch changes that would otherwise go unnoticed because of racing conditions
						//TODO: THINK ABOUT - is this the best way to catch changes upon retry? (with calling ValidateForRetry() twice)
						if (!areRetry.WaitOne(ex.Timeout, false))
						{
							if (tx.ValidateForRetry(null))
							{
								// only really abort retry if after a timeout nothing in the working set has changed
								// this is to catch changes that would otherwise go unnoticed due to racing conditions
								RetryTriggerList.Instance.RemoveRetry(areRetry);
								tx.Rollback();

								// no changes to relevant tx values until timeout; abort retry!
								throw new NstmRetryFailedException("Manual retry for transaction timed out! No changes were made to relevant transactional values.");
							}
						}
					}

					RetryTriggerList.Instance.RemoveRetry(areRetry);
					tx.Rollback();
				}
				catch (NstmValidationFailedException)
				{
					tx.Rollback();

					if (retryAtAll)
					{
						nRetries++;
						if (nRetries > maxRetries)
							throw new NstmRetryFailedException(string.Format("Could not commit transaction for operation within the limit of {0} retries!", maxRetries));

						if (maxProcessingTimeMsec != Timeout.Infinite)
						{
							TimeSpan processingTime = DateTime.Now.Subtract(executionStart);
							if (processingTime.Milliseconds > maxProcessingTimeMsec)
								throw new NstmRetryFailedException(string.Format("Could not commit transaction for operation within the limit of {0} msec!", maxProcessingTimeMsec));
						}
					}
					else
						throw;
				}
				catch
				{
					tx.Rollback();
					throw;
				}

				if (sleepBeforeRetryMsec != Timeout.Infinite)
					Thread.Sleep(sleepBeforeRetryMsec);
			}
		}


		public static void Retry()
		{
			Retry(1000); // default: timeout retry wait after 1sec
		}

		public static void Retry(int timeout)
		{
			throw new NstmRetryException(timeout);
		}

		#endregion


		#region Properties
		public static ITransaction Current
		{
			get
			{
				return new ThreadTransactionStack().Peek();
			}
		}


		public static int ActiveTransactionCount
		{
			get
			{
				return new ThreadTransactionStack().Count;
			}
		}
		
		public static IStmObject<T> CreateObject<T>()
		{
			return new StmObject<T>();
		}

		public static IStmObject<T> CreateObject<T>(T initialValue)
		{
			return new StmObject<T>(initialValue);
		}
		#endregion
	}
}
