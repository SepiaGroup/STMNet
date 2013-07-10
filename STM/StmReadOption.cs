namespace STM
{
	public enum StmReadOption
	{
		// need to be indexed in increasing order to be able to escalate an object´s read option
		PassingReadOnly = 0,
		ReadOnly = 1,
		ReadWrite = 2 // default; the other options are optimizations; with ReadWrite a user of NSTM is on the safe side - however it is the slowest option since it requires a clone of the object´s data
	}
}
