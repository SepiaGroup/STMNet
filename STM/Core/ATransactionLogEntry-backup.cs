using System;

namespace STM.Core
{
    internal abstract class ATransactionLogEntryBackup<T> //: ICloneable
    {
		//internal INstmVersioned Instance;
		//internal long Version = -1;

        //internal bool IsLocked = false;

		internal abstract int Id { get; } // Unique Id - like a Hashcode
		internal StmReadOption ReadOption = StmReadOption.PassingReadOnly;

		//internal ATransactionLogEntry(INstmVersioned instance)
		//{
		//	Instance = instance;
		//	Version = instance.Version;
		//}

		//internal ATransactionLogEntry(StmObject<T> originalObject, StmObject<T> newObject)
		//{
			
		//}
		
        internal abstract void Commit();
		
		//#region ICloneable Members

		//public virtual object Clone()
		//{
		//	throw new NotImplementedException("Method needs to be overridden in derived classes!");
		//}

		//#endregion
    }
}
