using System.Text;

namespace DQD.CommandLineParser
{
	public static class IArgumentSourceExtensions
	{
    public static string PullRemainder(this IArgumentSource source)
    {
      StringBuilder remainder = new StringBuilder();

      while (source.HasNext())
      {
        if (remainder.Length > 0)
          remainder.Append(' ');

        // If the argument contains spaces or tabs, surround it in double-quotes.
        string arg = source.Pull();

        if (arg.Contains(" ") || arg.Contains("\t"))
          remainder.Append('"').Append(arg.Replace("\"", "\"\"")).Append('"');
        else
          remainder.Append(arg.Replace("\"", "\"\""));
      }

      return remainder.ToString();
    }
	}
}
