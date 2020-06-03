using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Common
{
    public static class ConsoleHelper
    {
        private static object _lockObj = new object();

        public static void ColorWriteLine(ConsoleColor color, string format, params object[] arg)
        {
            lock (_lockObj)
            {
                ConsoleColor cc = Console.ForegroundColor;
                Console.ForegroundColor = color;
                if (arg == null || arg.Length <= 0)
                    Console.WriteLine(format);
                else
                    Console.WriteLine(format, arg);
                Console.ForegroundColor = cc;
                DebugHelper.WriteColorOutputToLog(color, string.Format(format, arg));
            }
        }
    }
}
