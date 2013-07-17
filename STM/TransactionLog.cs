using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace STM
{
	internal class TransactionLog //: ICloneable
	{
		private readonly Dictionary<int, ITransactionLogEntry> _entries = new Dictionary<int, ITransactionLogEntry>();
		private List<ITransactionLogEntry> _conflicts;

		internal void Add(ITransactionLogEntry txEntry)
		{
			_entries.Add(txEntry.UniqueId, txEntry);
		}

		internal TransactionLogEntry<T> GetObject<T>(StmObject<T> stmObject)
		{

			ITransactionLogEntry tle;
			if (_entries.TryGetValue(stmObject.UniqueId, out tle))
			{
				return (TransactionLogEntry<T>)tle;
			}

			return null;
		}


		internal int Count
		{
			get
			{
				return _entries.Count;
			}
		}

		internal ReadOnlyDictionary<int, ITransactionLogEntry> Transactions
		{
			get { return new ReadOnlyDictionary<int, ITransactionLogEntry>(_entries); }
		}

		internal void AddConflict(ITransactionLogEntry entry)
		{
			_conflicts.Add(entry);
		}
	}
}
