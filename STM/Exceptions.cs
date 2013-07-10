using System;

namespace STM
{
	[Serializable]
	public class NstmValidationFailedException : ApplicationException
	{
		public NstmValidationFailedException() { }

		public NstmValidationFailedException(string message) : base(message) { }

		public NstmValidationFailedException(string message, Exception innerException) : base(message, innerException) { }

		public NstmValidationFailedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}


	[Serializable]
	public class NstmRetryFailedException : ApplicationException
	{
		public NstmRetryFailedException() { }

		public NstmRetryFailedException(string message) : base(message) { }

		public NstmRetryFailedException(string message, Exception innerException) : base(message, innerException) { }

		public NstmRetryFailedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}


	[Serializable]
	public class NstmRetryException : ApplicationException
	{
		private readonly int _timeout = System.Threading.Timeout.Infinite;

		public NstmRetryException() { }

		public NstmRetryException(int timeout)
		{
			_timeout = timeout;
		}

		public int Timeout
		{
			get { return _timeout; }
		}
	}

}
