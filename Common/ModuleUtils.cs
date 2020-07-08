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
		public static ModuleDefinition SystemModule;

		public static Dictionary<XnaDirectory, ModuleDefinition> XnaModules;

		private static readonly string[] xnaDirectories = new string[]
		{
			"Microsoft.Xna.Framework",
			"Microsoft.Xna.Framework.Game",
			"Microsoft.Xna.Framework.Graphics",
			"Microsoft.Xna.Framework.Xact"
		};

		public static void Initialize(ModuleDefinition terraria)
		{
			TerrariaModule = terraria;
			SystemModule = ModuleDefinition.ReadModule($"{GetDirectoryFromGAC("mscorlib")}{Path.DirectorySeparatorChar}mscorlib.dll");

			XnaModules = new Dictionary<XnaDirectory, ModuleDefinition>();

			for (int i = 0; i < 4; i++)
			{
				XnaModules.Add((XnaDirectory)i, ModuleDefinition.ReadModule($"{GetDirectoryFromGAC(xnaDirectories[i])}{Path.DirectorySeparatorChar}{xnaDirectories[i]}.dll"));
			}
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

		public enum XnaDirectory 
		{ 
			XnaFramework,
			XnaFrameworkDotGame,
			XnaFrameworkDotGraphics,
			XnaFrameworkDotXact
		}
	}
}
