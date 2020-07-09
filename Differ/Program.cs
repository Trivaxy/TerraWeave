using System;
using Terraweave.Differ.Patching;

namespace Terraweave.Differ
{
    class Program
    {
        static Program()
        {
            Console.WriteLine("Are there any IL differences?");
        }

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: tweavediff [path to working directory]");
                Console.WriteLine("The working directory must contain:");
                Console.WriteLine("- Terraria.exe (vanilla, unmodified exe)");
                Console.WriteLine("- TerrariaModified.exe (your modified exe)");
                Console.WriteLine("- All the required dependencies (such as Relogic.dll, CSteamWorks.dll, etc)");
                Environment.Exit(0);
            }

            PatchCreator.Start(args[0]);
        }

        public static void Panic(string message)
        {
            Console.WriteLine("Error: " + message);
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}
