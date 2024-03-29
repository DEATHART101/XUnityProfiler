using System.Text.RegularExpressions;

namespace XUnityProfiler
{
    public class CodeFile
    {
        public string FilePath;
        public LinkedList<string> Lines = new LinkedList<string>();
        public List<BlockGetter> BlockGetters = new List<BlockGetter>();

        public void AddLine(string line)
        {
            Lines.AddLast(line);
        }

        public static string GetLineStart(string line)
        {
            int end = 0;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == ' ' || c == '\t')
                {
                    end = i;
                    continue;
                }

                break;
            }

            return line.Substring(0, end + 1);
        }
    }

    public class MethodGetter : BlockGetter
    {
        public string MethodName;

        public bool GetIsRoutine()
        {
            for (var i = StartLine; i != null; i = i.Next)
            {
                if (i.Value.IndexOf("yield return") >= 0)
                {
                    return true;
                }

                if (i.Value.IndexOf("yield break") >= 0)
                {
                    return true;
                }

                if (i == EndLine)
                {
                    break;
                }
            }

            return false;
        }

        protected override void OnModify()
        {
            base.OnModify();

            if (GetIsRoutine())
            {
                return;
            }

            FileInfo fileInfo = new FileInfo(CodeFile.FilePath);
            LinkedList<string> lines = StartLine.List;

            string lineStart = $"{CodeFile.GetLineStart(StartLine.Value)}\t";
            string content = $"\"{fileInfo.Name} {MethodName}\"";
            lines.AddAfter(StartLine, $"{lineStart}UnityEngine.Profiling.Profiler.BeginSample({content});");
            lines.AddBefore(EndLine, $"{lineStart}UnityEngine.Profiling.Profiler.EndSample();");

            for (var i = StartLine; i != null; i = i.Next)
            {
                int returnIndex = i.Value.IndexOf("return");
                if (returnIndex >= 0)
                {
                    lines.AddBefore(i, $"{CodeFile.GetLineStart(i.Value)}UnityEngine.Profiling.Profiler.EndSample();");
                }

                if (i == EndLine)
                {
                    break;
                }
            }
        }

        public static bool IsDefineLine(string line)
        {
            if (line.EndsWith(";") || line.EndsWith(","))
            {
                return false;
            }

            if (line.IndexOf("new ") >= 0)
            {
                return false;
            }

            if (line.IndexOf("return ") >= 0)
            {
                return false;
            }

            if (line.IndexOf("if ") >= 0)
            {
                return false;
            }

            if (line.IndexOf("foreach ") >= 0)
            {
                return false;
            }

            if (line.IndexOf("//") >= 0)
            {
                return false;
            }

            if (line.IndexOf("async") >= 0)
            {
                return false;
            }

            return true;
        }
    }

    public class BlockGetter
    {
        public CodeFile CodeFile;

        public LinkedListNode<string> BlockDefineLine;
        public LinkedListNode<string> StartLine;
        public LinkedListNode<string> EndLine;

        public bool Finished;

        public int BracketIndex;

        public void Start(LinkedListNode<string> start)
        {
            BlockDefineLine = start;
            StartLine = null;
            EndLine = null;
            EnterALine(start);
        }

        public bool EnterALine(LinkedListNode<string> line)
        {
            bool checkEnd = false;
            foreach (char item in line.Value)
            {
                if (item == '{')
                {
                    if (BracketIndex == 0)
                    {
                        StartLine = line;
                    }

                    BracketIndex++;
                }
                else if (item == '}')
                {
                    checkEnd = true;
                    BracketIndex--;
                }
            }

            if (BracketIndex == 0 && checkEnd)
            {
                EndLine = line;
                Finished = true;
            }

            return Finished;
        }

        public void PrintBlock()
        {
            for (var i = BlockDefineLine; i != null; i = i.Next)
            {
                Console.WriteLine(i.Value);

                if (i == EndLine)
                {
                    break;
                }
            }
        }

        public void Modify()
        {
            OnModify();
        }

        protected virtual void OnModify()
        {

        }
    }

    public class UnityProfiler
    {
        public static void ProccessFiles(string fileOrFolderPath)
        {
            List<CodeFile> codeFiles = new List<CodeFile>();

            if (File.Exists(fileOrFolderPath))
            {
                codeFiles.Add(ProccessToCodeFile(fileOrFolderPath));
            }
            else if (Directory.Exists(fileOrFolderPath))
            {
                foreach (string filePath in Directory.EnumerateFiles(fileOrFolderPath, "*.cs", SearchOption.AllDirectories))
                {
                    codeFiles.Add(ProccessToCodeFile(filePath));
                }
            }
            else
            {
                throw new Exception($"Path {fileOrFolderPath} is not a file's nor a folder's path");
            }

            foreach (var item in codeFiles)
            {
                foreach (var getter in item.BlockGetters)
                {
                    getter.Modify();
                }

                File.Delete(item.FilePath);
                File.WriteAllLines(item.FilePath, item.Lines);
            }
        }

        private static CodeFile ProccessToCodeFile(string filePath)
        {
            Console.WriteLine($"Processing file: {filePath}");
            CodeFile codeFile = new CodeFile();
            codeFile.FilePath = filePath;
            codeFile.Lines = new LinkedList<string>();

            MethodGetter curMethod = null;

            using (StreamReader sr = new StreamReader(filePath))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    codeFile.AddLine(line);

                    string methodPattern = @"(public|private|internal|protected)?\s+(static)?\s*[\w<>\[\]]+\s+([\w<>]+)\s*\([^)]*\)\s*{*";
                    //string methodPattern = @"(public|private|protected|internal)?(\s+static)?\s+([\w<>,]+)\s+([a-zA-Z_][\w]*)\s*\(([^)]*)\)\s*{";
                    Match match = Regex.Match(line, methodPattern);
                    if (match.Success)
                    {
                        int br = 1;
                    }
                    if (curMethod == null && match.Success && MethodGetter.IsDefineLine(line))
                    {
                        string functionName = match.Groups[3].Value;
                        if (curMethod != null)
                        {
                            throw new Exception();
                        }
                        curMethod = new MethodGetter();
                        curMethod.MethodName = functionName;
                        curMethod.CodeFile = codeFile;

                        curMethod.Start(codeFile.Lines.Last);
                    }
                    else
                    {
                        if (curMethod != null)
                        {
                            curMethod.EnterALine(codeFile.Lines.Last);
                        }
                    }

                    if (curMethod != null && curMethod.Finished)
                    {
                        codeFile.BlockGetters.Add(curMethod);
                        curMethod.PrintBlock();
                        curMethod = null;
                    }
                }
            }

            return codeFile;
        }
    }
}