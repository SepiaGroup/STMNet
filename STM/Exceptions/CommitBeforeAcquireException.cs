using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STM.Exceptions
{
	public class CommitBeforeAcquireException : ApplicationException
	{
			public CommitBeforeAcquireException() { }

			public CommitBeforeAcquireException(string message) : base(message) { }

			public CommitBeforeAcquireException(string message, Exception innerException) : base(message, innerException) { }

			public CommitBeforeAcquireException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
