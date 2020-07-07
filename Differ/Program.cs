using System;
using Terraweave.Differ.Patching;

namespace Terraweave.Differ
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: tweavediff [path to unmodified exe] [patch to modded exe]");
                Environment.Exit(0);
            }

            if (args.Length < 2)
                Panic("Did not specify modded terraria path");

            PatchCreator.Start(args[0], args[1]);
        }

        public static void Panic(string message)
        {
            Console.WriteLine("Error: " + message);
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}
