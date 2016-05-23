using System;
using System.IO;

namespace Baranova.Nsudotnet.LinesCounter
{
    class Program
    {
        private static int _localCount = 0;
        private static int _globalCount = 0;

        public static bool IsNotCodeLine(string str)
        {
            if (str.Equals("")) return true;
            else
                if (str.Length > 1)
            {
                if (str.Substring(0, 2).Equals("//"))
                    return true;
                else
                    return false;
            }
            else
                    if (str.Length == 1)
                return false;
            else
                return true;
        }

        public static bool ContainsCommentBegin(string str)
        {
            if (str.Contains("/*"))
                return true;
            else return false;
        }

        public static bool ContainsCommentEnd(string str)
        {
            if (str.Contains("*/"))
                return true;
            else return false;
        }

        static void Main(string[] args)
        {
            String ext = String.Format("*.{0}", args[0]);

            String path = args[1];
            String[] files = Directory.GetFiles(path, ext, SearchOption.AllDirectories);
            foreach (string f in files)
            {
                using (var streamReader = new StreamReader(new FileStream(f, FileMode.Open, FileAccess.Read)))
                {
                    String line = null;
                    bool isInComment = false;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        if (IsNotCodeLine(line))
                            continue;
                        if (ContainsCommentBegin(line))
                            isInComment = true;
                        if (isInComment)
                            _localCount--;
                        if (ContainsCommentEnd(line))
                            isInComment = false;
                        _localCount++;
                    }
                    _globalCount = _globalCount + _localCount;
                    _localCount = 0;
                }
            }
            Console.WriteLine("Lines: {0}", _globalCount);
            Console.ReadLine();
        }
    }
}

