using System.Threading;
using System.Collections;

namespace STM.Core
{
#if DEBUG
    public // this is necessary for the test of this internal type, since unit test requires test to be public
#else
    internal 
#endif
	partial class RetryTriggerList
    {
        #region set of waiting retries for multiple triggers
        public class WaitingRetriesSet
        {
            private readonly Hashtable _waitingRetries = new Hashtable();

            public void Add(Hashtable waitingRetriesForInstance)
            {
	            foreach (AutoResetEvent areRetry in waitingRetriesForInstance.Values)
	            {
		            if (!_waitingRetries.ContainsKey(areRetry))
		            {
			            _waitingRetries.Add(areRetry, areRetry);
		            }
	            }
            }

            public void NotifyAll()
            {
	            foreach (AutoResetEvent areRetry in _waitingRetries.Values)
	            {
		            areRetry.Set();
	            }
            }
        }
        #endregion


        #region managing the singleton
        private static readonly RetryTriggerList SingletonInstance = new RetryTriggerList();

        public static RetryTriggerList Instance
        {
            get
            {
                return SingletonInstance;
            }
        }
        #endregion


        #region manage the waiting retries for a list of tx value triggers
        // manages the list of tx values (instances) and their retries (waithandles) waiting for changes applied to them
        private Hashtable _triggers;

        public RetryTriggerList()
        {
            Clear();
        }

        private void Clear()
        {
            lock (this)
            {
                _triggers = new Hashtable();
            }
        }


        public void RegisterRetry(object instance, AutoResetEvent areRetry)
        {
            lock (this)
            {
                Hashtable waitingRetries;

                if (_triggers.ContainsKey(instance))
                {
                    // there are already retries waiting for this instance
                    // add this retry´s waithandle to its list
                    waitingRetries = (Hashtable)_triggers[instance];
                    if (!waitingRetries.ContainsKey(areRetry))
                        waitingRetries.Add(areRetry, areRetry);
                }
                else
                {
                    // no retries waiting for instance so far
                    // create and entry for it in the trigger list
                    waitingRetries = new Hashtable {{areRetry, areRetry}};
	                _triggers.Add(instance, waitingRetries);
                }
            }
        }


        public Hashtable TakeAllRetries(object instance)
        {
            lock (this)
            {
                if (_triggers.ContainsKey(instance))
                {
                    var waitingRetries = (Hashtable)_triggers[instance];
                    _triggers.Remove(instance);
                    return waitingRetries;
                }
				
                return new Hashtable();
            }
        }


        public void RemoveRetry(AutoResetEvent areRetry)
        {
            lock (this)
            {
	            foreach (Hashtable waitingRetries in _triggers.Values)
	            {
		            waitingRetries.Remove(areRetry);
	            }
            }
        }


        public void NotifyRetriesForTrigger(object instance)
        {
            var wrs = new WaitingRetriesSet();
            wrs.Add(TakeAllRetries(instance));
            wrs.NotifyAll();
        }
        #endregion
    }
}
