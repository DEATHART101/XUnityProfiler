using XUnityProfiler;
using XArgParser;

class Program
{
    public struct ProgArgs
    {
        [HelpInfo("The path to the file(.cs) or a folder")]
        [ShortName("p")]
        public string Path;
    }

    static void Main(string[] args)
    {
        string outHelpMsg;
        ProgArgs progArgs;
        if (!XArgParser.ArgParser.TryParse<ProgArgs>(out progArgs, out outHelpMsg, args))
        {
            Console.WriteLine(outHelpMsg);
            return;
        }

        XUnityProfiler.UnityProfiler.ProccessFiles(progArgs.Path);
    }
}