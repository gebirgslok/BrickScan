using System;

namespace BrickScan.Training
{
    public class ConsoleWriter : IConsoleWriter
    {
        public bool Verbose { get; set; }

        public void WriteLine(string line)
        {
            Console.WriteLine(line);
        }

        public void WriteLineIfVerbose(string line)
        {
            if (Verbose)
            {
                WriteLine(line);
            }
        }
    }

    public interface IConsoleWriter
    {
        bool Verbose { get; set; }

        void WriteLine(string line);

        void WriteLineIfVerbose(string line);
    }
}
