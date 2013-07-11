using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STM.Exceptions
{
	public class InvalidStmObjectCastException : ApplicationException
	{
			public InvalidStmObjectCastException() { }

			public InvalidStmObjectCastException(string message) : base(message) { }

			public InvalidStmObjectCastException(string message, Exception innerException) : base(message, innerException) { }

			public InvalidStmObjectCastException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
