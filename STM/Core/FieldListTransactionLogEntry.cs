using System;
using System.Collections.Generic;

namespace STM.Core
{
	internal class FieldlistTransactionLogEntry : ITransactionLogEntry
	{
		internal Dictionary<string, object> TempFieldvalues;


		internal FieldlistTransactionLogEntry(INstmVersioned instance) : base(instance)
		{
			TempFieldvalues = new Dictionary<string, object>();
		}


		internal override void Commit()
		{
			Type t = Instance.GetType();
			foreach (var key in TempFieldvalues.Keys)
			{
				System.Reflection.FieldInfo fi = t.GetField(key, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				fi.SetValue(Instance, TempFieldvalues[key]);
			}
			Instance.IncrementVersion();
		}


		#region ICloneable Members

		public override object Clone()
		{
			var logEntryClone = new FieldlistTransactionLogEntry(Instance)
			{
				IsLocked = IsLocked,
				Version = Version,
				TempFieldvalues = new Dictionary<string, object>(TempFieldvalues),
				ReadOption = ReadOption
			};

			// the dict needs to be cloned when the txlog is cloned; otherwise changes in the nested tx are registered in the same dicts as in the parent tx.

			return logEntryClone;
		}

		#endregion
	}
}
