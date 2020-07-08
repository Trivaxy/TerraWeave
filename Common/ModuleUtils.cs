using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Terraweave.Common
{
	public static class ModuleUtils
	{
		public static ModuleDefinition TerrariaModule;
		public static ModuleDefinition SystemModule;

		public static Dictionary<string, ModuleDefinition> XnaModules;

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
			SystemModule = TerrariaModule.TypeSystem.CoreLibrary as ModuleDefinition;

			XnaModules = new Dictionary<string, ModuleDefinition>();

			foreach (string xnaDirectory in xnaDirectories)
			{
				string finalDirectory = Directory.EnumerateDirectories(
					Environment.ExpandEnvironmentVariables(
						Path.Combine(
						"%WINDIR%",
						"Microsoft.NET",
						"assembly",
						"GAC_32",
						xnaDirectory)
						))
						.First();

				XnaModules.Add(xnaDirectory, ModuleDefinition.ReadModule(finalDirectory));
			}
		}
	}
}
