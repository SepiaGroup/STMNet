namespace STM
{
	//internal interface IStmObject //: INstmVersioned
	//{
	//	object Value { get; set; }
	//	object CloneValue();
	//}

	public interface IStmObject<T> 
	{
		T Value { get; }
		StmObject<T> CloneValue();

		int Id { get; } // A Unique Id - like a Hashcode.
		object Version { get; }

		T Read();
		T Read(StmReadOption option);
		void Write(T value);
	}
}
