using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STM.Exceptions
{
	public class TransactionNotActiveException : ApplicationException
	{
			public TransactionNotActiveException() { }

			public TransactionNotActiveException(string message) : base(message) { }

			public TransactionNotActiveException(string message, Exception innerException) : base(message, innerException) { }

			public TransactionNotActiveException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
