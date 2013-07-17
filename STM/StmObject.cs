using System;
using System.Linq;
using System.Threading;
using STM.Exceptions;

namespace STM
{

	// shared object: can be involved in many tx and can be used by many threads
	public class StmObject<T>  //: IStmObject<T> 
	{
		private static int _uniqueId = Int32.MinValue;
		internal ManualResetEventSlim ResetEvent = new ManualResetEventSlim(false);

		internal static int GetUniqueId
		{
			get
			{
				return Interlocked.Increment(ref _uniqueId);
			}
		}

		public readonly int UniqueId = GetUniqueId;
		public T Value
		{
			get { return Element.Value; }
		}

		internal Element<T> Element;

		static StmObject()
		{
			// check if T implements ICloneable...
			if (typeof(T).GetInterfaces().Any(interfaceType => interfaceType == typeof(ICloneable)))
			{
				return;
			}

			// ...if not, then we can still work with T if it´s a value type or string, because cloning them is easy
			if (!(typeof(T).IsValueType || typeof(T) == typeof(string)))
			{
				throw new InvalidStmObjectCastException(string.Format("Invalid type parameter! Cannot create StmObject<T> for type {0}. It is neither a value type, nor a string, nor does it implement ICloneable.", typeof(T).Name));
			}
		}
		
		//internal StmObject() { }

		public StmObject(T initialValue)
		{
			Element = new Element<T>(initialValue);
		}

		public StmObject<T> Clone()
		{
			if (typeof(T).IsValueType || typeof(T) == typeof(string))
			{
				// value types and strings are cloned by just returning them
				// for value types that means they are implicitly copied,
				// and strings are immutable anyhow
				return new StmObject<T>(Element.Value);
			}

			// if it´s not a value type or string, then it must be cloneable
			// (our class ctor has checked that!)
			return Element.Value == null ? new StmObject<T>(default(T)) : (new StmObject<T>((T)((ICloneable)Element.Value).Clone()));
		}

		public T Read()
		{
			return Stm.Transaction.LogRead(this);
		}

		public void Write(T newValue)
		{
			Stm.Transaction.LogWrite(this, newValue);
		}
	}
}
