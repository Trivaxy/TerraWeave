using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Terraweave.Common
{
	public static class ModuleUtils
	{
		public static ModuleDefinition TerrariaModule;
		public static ModuleDefinition ModdedModule;
		public static ModuleDefinition MscorlibModule;

		public static Dictionary<string, ModuleDefinition> DependencyModules = new Dictionary<string, ModuleDefinition>();

		private static string[] dependencyBlacklist = new string[]
		{
			"Common.dll",
			"Differ.dll",
			"Mono.Cecil.dll",
			"Mono.Cecil.Mdb.dll",
			"Mono.Cecil.Pdb.dll",
			"Mono.Cecil.Rocks.dll",
			"steam_api.dll",
			"CSteamworks.dll"
		};

		private static string[] xnaDirectories = new string[]
		{
			"Microsoft.Xna.Framework",
			"Microsoft.Xna.Framework.Game",
			"Microsoft.Xna.Framework.Graphics",
			"Microsoft.Xna.Framework.Xact"
		};

		private static string[] importantSystemModules = new string[]
		{
			"System.ComponentModel",
			"System.Text.RegularExpressions"
		};

		public static void Initialize(string workingDirectory)
		{
			TerrariaModule = GetWorkingModule("Terraria.exe");
			ModdedModule = GetWorkingModule("TerrariaModified.exe");

			MscorlibModule = ModuleDefinition.ReadModule($"{GetDirectoryFromGAC("mscorlib")}{Path.DirectorySeparatorChar}mscorlib.dll");

			foreach (string directory in xnaDirectories)
				DependencyModules.Add(directory, ModuleDefinition.ReadModule($"{GetDirectoryFromGAC(directory)}{Path.DirectorySeparatorChar}{directory}.dll"));

			var dllPaths = Directory.EnumerateFiles(
				Directory.EnumerateDirectories(Environment.ExpandEnvironmentVariables(
					Path.Combine("%WINDIR%", "Microsoft.NET", "Framework")))
					.First(dir => Path.GetFileName(dir).StartsWith("v4.0")))
				.Where(file => Path.GetFileName(file).StartsWith("System.") && file.EndsWith(".dll"))
				.Where(file => importantSystemModules.Contains(Path.GetFileNameWithoutExtension(file)));

			foreach (string dll in dllPaths)
				DependencyModules.Add(Path.GetFileNameWithoutExtension(dll), ModuleDefinition.ReadModule(dll));

			foreach (string moduleName in Directory.EnumerateFiles(workingDirectory)
				.Where(file => file.EndsWith(".dll") && !dependencyBlacklist.Contains(Path.GetFileName(file))))
			{
				ModuleDefinition module = GetWorkingModule(moduleName);
				DependencyModules[Path.GetFileNameWithoutExtension(module.Name)] = module;
			}

			ModuleDefinition GetWorkingModule(string file)
				=> ModuleDefinition.ReadModule(Path.Combine(workingDirectory, file), DefaultParameters);
		}

		public static ReaderParameters DefaultParameters = new ReaderParameters()
		{
			AssemblyResolver = GetAssemblyResolver()
		};

		// Well played assembly resolver, you have escaped death this time
		public static IAssemblyResolver GetAssemblyResolver()
		{
			DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();

			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				foreach (string xnaDirectory in xnaDirectories)
				{
					resolver.AddSearchDirectory(GetDirectoryFromGAC(xnaDirectory));
				}
			}

			return resolver;
		}

		private static string GetDirectoryFromGAC(string dir)
		{
			return Directory.EnumerateDirectories(
					Environment.ExpandEnvironmentVariables(
						Path.Combine(
						"%WINDIR%",
						"Microsoft.NET",
						"assembly",
						"GAC_32",
						dir)
						))
						.First();
		}
	}
}