using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STM
{
	internal class Element<T>
	{
		internal object Version;

		public T Value { get; private set; }

		internal Element(T initialValue)
		{
			Value = initialValue;
			Version = int.MinValue;
		}
	}
}
