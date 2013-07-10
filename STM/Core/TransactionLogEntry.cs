
using System.Threading;

namespace STM.Core
{
	internal class TransactionLogEntry<T> : ITransactionLogEntry
	{
		//internal object TempValue;
		
		//internal TransactionLogEntry(INstmVersioned instance) : base(instance) { }

		//private StmObject<T> _originalObject;
		//private StmObject<T> _newObject;

		internal TransactionLogEntry(StmObject<T> originalObject, StmObject<T> newObject, StmReadOption readOption = StmReadOption.PassingReadOnly)
		{
			//_originalObject = originalObject;
			//_newObject = newObject;

			OriginalObject = originalObject;
			NewObject = newObject;
			ReadOption = readOption;

			var versionId = originalObject.Version;
			if (versionId is int)
			{
				OriginalVersionId = (int) versionId;
			}
			else
			{
				throw new System.Exception("StmObject is not in valid state.");
			}
		}

		internal StmObject<T> OriginalObject { get; private set; }
		internal StmObject<T> NewObject { get; private set; }
		internal StmReadOption ReadOption { get; private set; }

		public int Id 
		{
			get { return OriginalObject.Id; }
		}

		public int OriginalVersionId { get; private set; }

		internal void UpdateNewStmObject(StmObject<T> newStmObject)
		{
			NewObject = newStmObject;
		}

		//internal override void Commit()
			//{
			//	//((IStmObject)Instance).Value = TempValue;
			//	//Instance.IncrementVersion();
			//}

			//#region ICloneable Members

		//public object Clone()
		//{
		//	//var logEntryClone = new TransactionLogEntry(Instance)
		//	//	{
		//	//		IsLocked = IsLocked,
		//	//		Version = Version,
		//	//		TempValue = TempValue,
		//	//		ReadOption = ReadOption
		//	//	};

		//	//TODO: THINK ABOUT - clone on txlog clone? some tests done run anymore if the value is cloned. check!
		//	//logEntryClone.tempValue = (this.tempValue as ICloneable).Clone();

		//	//return logEntryClone;

		//	return null;
		//}

			//#endregion
		}
}
