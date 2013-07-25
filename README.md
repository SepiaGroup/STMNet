# STMNet 
##### A Lightweight Software Transactional Memory API

STMNet is an API for multithreaded computation in which shared data is synchronized without using locks. Threads synchronize by means of memory transactions, short-lived computations that either commit (take effect) or abort (have no effect). Transactions avoid the well-known problems of locking, including deadlock, priority inversion, and fault-intolerance. The STMNet is a software transactional memory package written in C#.

Quick start:

````c#
// Create STM Objects to be used by several threads
public StmObject<int> myStmValue = StmCreateObject(5);
public StmObject<SomeObject> myStmObject = Stm.CreateObject(new SomeObject(initValue));

// This method is called by a thread to do something...
public void DoSomething()
{
	using(var trans = Stm.BeginTransaction())
    {
    	// Read the value of myStmValue
    	var v = myStmValue.Read();
        
        // Create and init a new SomeObject
        var newObj = new SomeObject(v);
        
        myStmObject.Write(newObj);
    }
        
    if(Stm.Transaction.State == TransactionsState.Committed)
    {
        // Transactions committed 
    }
    
    if(Stm.Transaction.State == TransactionsState.Aborted)
    {
    	// Transactions aborted 
    }
}
````

Transactions Delegate and Retry Delegate

````c#
// Create STM Objects to be used by several threads
public StmObject<int> myStmValue = StmCreateObject(5);
public StmObject<SomeObject> myStmObject = Stm.CreateObject(new SomeObject(initValue));

// Create a Transaction Delegate
public void TransDelegate(Transaction transaction)
{
	using (transaction)
	{
		myStmValue.Write(2);
        
        // Create and init a new SomeObject
        var newObj = new SomeObject(myStemValue.Read());
        
        myStmObject.Write(newObj);
	}
}

// Create a Retry Delegate
public bool RetryDelegate(Transaction transaction)
{
	return transaction.RetryCount < 3;
}

// Method to execute the transaction
public void ExecuteTransTest()
{
    var trans = Stm.ExecuteTransaction(TransDelegate, RetryDelegate);
    
    if(trans.State == TransactionsState.Committed)
    {
		// Transactions committed 
    }
    
    if(trans.State == TransactionsState.Aborted)
    {
    	// Transactions aborted 
    }
}
````

##### Note:
* Objects that take part in an STM Transaction must implement IStmObject interface