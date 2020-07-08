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

			ReaderParameters parameters = new ReaderParameters() { AssemblyResolver = GetAssemblyResolver() };

			ModuleDefinition terraria = ModuleDefinition.ReadModule(terrariaAssemblyPath, parameters);
			ModuleDefinition moddedTerraria = ModuleDefinition.ReadModule(moddedAssemblyPath, parameters);

			SerializingUtils.TerrariaModule = terraria;

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
				patch.Apply(SerializingUtils.TerrariaModule);

			SerializingUtils.TerrariaModule.Write("PatchedTerraria.exe");
		}

		public static void Log(string message) => Console.WriteLine(message);
	}
}