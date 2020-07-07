using Mono.Cecil;
using System;
using System.IO;
using System.Linq;
using Terraweave.Common.Patching;

namespace Terraweave.Differ.Patching
{
	public static class PatchCreator
	{
		public static void Start(string terrariaAssemblyPath, string moddedAssemblyPath)
		{
			if (!File.Exists(terrariaAssemblyPath))
				Program.Panic("Could not find the vanilla Terraria.exe");

			if (!File.Exists(moddedAssemblyPath))
				Program.Panic("Could not find the modded Terraria.exe");

			ModuleDefinition terraria = ModuleDefinition.ReadModule(terrariaAssemblyPath);
			ModuleDefinition moddedTerraria = ModuleDefinition.ReadModule(moddedAssemblyPath);

			TypeDefinition[] injectedTypes = moddedTerraria.GetTypes()
				.Where(type => terraria.GetType(type.FullName) == null)
				.ToArray();

			using (MemoryStream stream = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(stream))
				{
					writer.Write("TWEAVE");

					foreach (TypeDefinition type in injectedTypes)
					{
						Log($"Detected type to inject: {type.FullName}");
						writer.Write(new TypeInjectPatch(type).SerializePatch());
					}
				}

				File.WriteAllBytes("patch.tweave", stream.ToArray());
			}
		}

		public static void Log(string message) => Console.WriteLine(message);
	}
}