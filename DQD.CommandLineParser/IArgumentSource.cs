namespace DQD.CommandLineParser
{
	public interface IArgumentSource
	{
		string? Current { get; }
		bool HasNext();
		bool HasNext(int count);
		string? Peek(int index);
		string Pull();
	}
}
