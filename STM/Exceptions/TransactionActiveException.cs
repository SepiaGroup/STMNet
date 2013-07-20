using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STM.Exceptions
{
	public class TransactionActiveException : ApplicationException
	{
			public TransactionActiveException() { }

			public TransactionActiveException(string message) : base(message) { }

			public TransactionActiveException(string message, Exception innerException) : base(message, innerException) { }

			public TransactionActiveException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
