using Mono.Cecil;
using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Terraweave.Common;
using Terraweave.Common.Patching;

namespace Terraweave.Differ.Patching
{
	public static class PatchCreator
	{
		public static void Start(string workingDirectory)
		{
			if (!File.Exists(Path.Combine(workingDirectory, "Terraria.exe")))
				Program.Panic("Could not find the vanilla Terraria.exe");

			if (!File.Exists(Path.Combine(workingDirectory, "TerrariaModified.exe")))
				Program.Panic("Could not find the modded Terraria.exe (must be named TerrariaModified.exe)");

			ModuleUtils.Initialize(workingDirectory);

			int patchCount = 0;

			TypeDefinition[] injectedTypes = ModuleUtils.ModdedModule.GetTypes()
				.Where(type => ModuleUtils.TerrariaModule.GetType(type.FullName) == null)
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