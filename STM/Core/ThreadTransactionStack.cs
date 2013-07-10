using System;
using System.Collections.Generic;

namespace STM.Core
{
    /*
     * Tx are fixed to the thread they are created in.
     * All tx still open in a thread are pointed to by a TLS slot ("NstmTxStack").
     * In this slot they are mainly organized like a stack, because the default is to really nest tx if they
     * are created while another tx is still open.
     * However tx need not to nest; there can be just multiple tx open side by side in a thread.
     * Finishing side by side tx needs to be in reverse order, though, and must not overlap.
     */
    internal class ThreadTransactionStack
    {
        [ThreadStatic]
        private static List<ITransaction> _txStack;


        internal ThreadTransactionStack()
        {
            if (_txStack == null)
            {
                _txStack = new List<ITransaction>();
            }
        }


        public void Push(ITransaction tx)
        {
            _txStack.Add(tx);
        }
        

        public ITransaction Pop()
        {
            if (_txStack.Count > 0)
            {
                var tx = _txStack[_txStack.Count - 1];
                _txStack.RemoveAt(_txStack.Count - 1);
                return tx;
            }

            return null;
        }


        public ITransaction Peek()
        {
	        return _txStack.Count > 0 ? _txStack[_txStack.Count - 1] : null;
        }


	    public void Remove(ITransaction tx)
        {
		    if (Peek() == tx)
		    {
			    Pop();
		    }
		    else
		    {
			    throw new InvalidOperationException("NSTM transaction to be removed is not the current transaction! Check for overlapping transaction Commit()/Abort(). Recommendation: Create transactions within the scope of a using() statement.");
		    }
        }


        public int Count
        {
            get
            {
                return _txStack.Count;
            }
        }
    }
}
