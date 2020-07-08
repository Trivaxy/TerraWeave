using Mono.Cecil;
using System;
using System.IO;
using System.Linq;

namespace Terraweave.Common
{
	public static class ModuleUtils
	{
		public static ModuleDefinition TerrariaModule;
		public static ModuleDefinition SystemModule;

		public static void Initialize(ModuleDefinition terraria)
		{
			TerrariaModule = terraria;
			SystemModule = TerrariaModule.TypeSystem.CoreLibrary as ModuleDefinition;
		}

		public static ReaderParameters DefaultParameters = new ReaderParameters()
		{
			AssemblyResolver = GetAssemblyResolver()
		};
		public static IAssemblyResolver GetAssemblyResolver()
		{
			DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();

			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				string[] xnaDirectories = new string[]
				{
				"Microsoft.Xna.Framework",
				"Microsoft.Xna.Framework.Game",
				"Microsoft.Xna.Framework.Graphics",
				"Microsoft.Xna.Framework.Xact"
				};

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

					resolver.AddSearchDirectory(finalDirectory);
				}
			}

			return resolver;
		}
	}
}
