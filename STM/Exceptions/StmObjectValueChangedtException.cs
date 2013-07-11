using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STM.Exceptions
{
	public class StmObjectValueChangedException : ApplicationException
	{
			public StmObjectValueChangedException() { }

			public StmObjectValueChangedException(string message) : base(message) { }

			public StmObjectValueChangedException(string message, Exception innerException) : base(message, innerException) { }

			public StmObjectValueChangedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
