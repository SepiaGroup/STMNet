using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STM.Exceptions
{
	public class StmObjectBusyException : ApplicationException
	{
			public StmObjectBusyException() { }

			public StmObjectBusyException(string message) : base(message) { }

			public StmObjectBusyException(string message, Exception innerException) : base(message, innerException) { }

			public StmObjectBusyException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
