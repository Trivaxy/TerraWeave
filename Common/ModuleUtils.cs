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
		public static ModuleDefinition SystemModule;

		public static Dictionary<TerrariaDependency, ModuleDefinition> DependencyModules = new Dictionary<TerrariaDependency, ModuleDefinition>();

		private static readonly Dictionary<string, TerrariaDependency> xnaDependencies = new Dictionary<string, TerrariaDependency>()
		{
			{ "Microsoft.Xna.Framework", TerrariaDependency.XnaFramework },
			{ "Microsoft.Xna.Framework.Game", TerrariaDependency.XnaFrameworkDotGame },
			{ "Microsoft.Xna.Framework.Graphics", TerrariaDependency.XnaFrameworkDotGraphics },
			{ "Microsoft.Xna.Framework.Xact", TerrariaDependency.XnaFrameworkDotXact }
		};

		public static void Initialize(string workingDirectory)
		{
			TerrariaModule = GetWorkingModule("Terraria.exe");
			ModdedModule = GetWorkingModule("TerrariaModified.exe");

			SystemModule = ModuleDefinition.ReadModule($"{GetDirectoryFromGAC("mscorlib")}{Path.DirectorySeparatorChar}mscorlib.dll");

			foreach (var xnaDependency in xnaDependencies)
				DependencyModules.Add(xnaDependency.Value, ModuleDefinition.ReadModule($"{GetDirectoryFromGAC(xnaDependency.Key)}{Path.DirectorySeparatorChar}{xnaDependency.Key}.dll"));

			DependencyModules[TerrariaDependency.ReLogic] = GetWorkingModule("ReLogic.dll");

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
				foreach (string xnaDirectory in xnaDependencies.Keys)
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

		public enum TerrariaDependency
		{ 
			XnaFramework,
			XnaFrameworkDotGame,
			XnaFrameworkDotGraphics,
			XnaFrameworkDotXact,
			ReLogic,
			ReLogicNative,
			SteamAPI,
			LogitechWeirdThing,
			CUESDK
		}
	}
}
