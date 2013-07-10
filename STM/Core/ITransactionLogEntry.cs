using System;

namespace STM.Core
{
    public interface ITransactionLogEntry
    {
	    int Id { get; } // Unique Id - like a Hashcode
	    int OriginalVersionId { get; }
	    bool Aquire();
    }
}
