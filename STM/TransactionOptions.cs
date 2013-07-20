using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STM
{
	public class TransactionOptions
	{
		public bool AbortOnReadConflicts { get; set; }
		public bool AbortOnFirstConflict { get; set; }
		public bool RetryOnReadConflicts { get; set; }
		public int MaxAutoRetryAttempts { get; set; }

		public TransactionOptions()
		{
			AbortOnReadConflicts = true;
			AbortOnFirstConflict = true;
			RetryOnReadConflicts = true;
			MaxAutoRetryAttempts = 1;
		}
	}
}
