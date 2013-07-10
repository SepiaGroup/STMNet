using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STM.Exceptions
{
	public class InvalidStmObjectStateException : ApplicationException
	{
			public InvalidStmObjectStateException() { }

			public InvalidStmObjectStateException(string message) : base(message) { }

			public InvalidStmObjectStateException(string message, Exception innerException) : base(message, innerException) { }

			public InvalidStmObjectStateException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
