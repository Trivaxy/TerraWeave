using Mono.Cecil;
using System;
using System.IO;
using System.Linq;
using Terraweave.Common;
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

			ModuleDefinition terraria = ModuleDefinition.ReadModule(terrariaAssemblyPath, ModuleUtils.DefaultParameters);
			ModuleDefinition moddedTerraria = ModuleDefinition.ReadModule(moddedAssemblyPath, ModuleUtils.DefaultParameters);

			ModuleUtils.Initialize(terraria);

			int patchCount = 0;

			TypeDefinition[] injectedTypes = moddedTerraria.GetTypes()
				.Where(type => terraria.GetType(type.FullName) == null)
				.ToArray();

			patchCount += injectedTypes.Count();

			using (MemoryStream stream = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(stream))
				{
					writer.Write(System.Text.Encoding.ASCII.GetBytes("TWEAVE"));

					writer.Write(patchCount);

					foreach (TypeDefinition type in injectedTypes)
					{
						Log($"Detected type to inject: {type.FullName}");
						writer.Write(PatchTypes.TypeInject);
						new TypeInjectPatch(type).SerializePatch(writer);
					}
				}

				File.WriteAllBytes("patch.tweave", stream.ToArray());
			}

			TestDeserializePatches();
		}

		// this will be moved to installer eventually, just here for testing
		public static void TestDeserializePatches()
		{
			Patch[] patches;

			using (BinaryReader reader = new BinaryReader(File.Open("patch.tweave", FileMode.Open)))
			{
				if (System.Text.Encoding.ASCII.GetString(reader.ReadBytes(6)) != "TWEAVE")
					Program.Panic("Not a tweave file!");

				int patchCount = reader.ReadInt32();
				patches = new Patch[patchCount];

				for (int i = 0; i < patchCount; i++)
				{
					byte patchType = reader.ReadByte();

					switch (patchType)
					{
						case PatchTypes.TypeInject:
							patches[i] = new TypeInjectPatch(reader);
							break;
					}
				}
			}

			foreach (Patch patch in patches)
				patch.Apply(ModuleUtils.TerrariaModule);

			ModuleUtils.TerrariaModule.Write("PatchedTerraria.exe");
		}

		public static void Log(string message) => Console.WriteLine(message);
	}
}