using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Terraweave.Common
{
	public static partial class SerializingUtils
	{
		public static ReadOnlyDictionary<Code, OpCode> CodeToOpCode = new ReadOnlyDictionary<Code, OpCode>(
			typeof(OpCodes)
			.GetFields()
			.AsEnumerable()
			.ToDictionary(field =>
			{
				if (!Enum.TryParse(field.Name, out Code code))
					throw new Exception("Failed to construct Code-OpCode dictionary");

				return code;
			},
			field => (OpCode)field.GetValue(null))
			);

		public static void SerializeTypeReference(TypeReference type, BinaryWriter writer)
		{
			writer.Write(type.Namespace);
			writer.Write(type.Name);
		}

		public static TypeReference DeserializeTypeReference(BinaryReader reader)
		{
			string @namespace = reader.ReadString();
			string typeName = reader.ReadString();

			ModuleDefinition module = null;

			if (@namespace.StartsWith("System"))
				module = ModuleUtils.SystemModule;
			else if (@namespace.StartsWith("Terraria"))
				module = ModuleUtils.TerrariaModule;
			else if (@namespace.StartsWith("ReLogic"))
				module = ModuleUtils.DependencyModules[ModuleUtils.TerrariaDependency.ReLogic];
			else
				switch (@namespace)
				{
					case "Microsoft.Xna.Framework":
						module = ModuleUtils.DependencyModules[ModuleUtils.TerrariaDependency.XnaFramework];
						break;
					case "Microsoft.Xna.Framework.Game":
						module = ModuleUtils.DependencyModules[ModuleUtils.TerrariaDependency.XnaFrameworkDotGame];
						break;
					case "Microsoft.Xna.Framework.Graphics":
						module = ModuleUtils.DependencyModules[ModuleUtils.TerrariaDependency.XnaFrameworkDotGraphics];
						break;
					case "Microsoft.Xna.Framework.Xact":
						module = ModuleUtils.DependencyModules[ModuleUtils.TerrariaDependency.XnaFrameworkDotXact];
						break;
				}

			if (module == null)
			{
				throw new NullReferenceException("Deserialized TypeReference's ModuleDefinition was null!");
			}

			TypeReference type = new TypeReference(
				@namespace,
				typeName,
				module,
				module);

			if (module == ModuleUtils.TerrariaModule)
			{
				type.Scope = null;
				return type;
			}

			return ModuleUtils.TerrariaModule.ImportReference(type);
		}

		public static void SerializeMethodReference(MethodReference method, BinaryWriter writer)
		{
			writer.Write(method.Name);
			SerializeTypeReference(method.ReturnType, writer);
			SerializeTypeReference(method.DeclaringType, writer);

			writer.Write(method.Parameters.Count);

			foreach (ParameterDefinition parameter in method.Parameters)
				SerializeParameterDefinition(parameter, writer);
		}

		public static MethodReference DeserializeMethodReference(BinaryReader reader)
		{
			MethodReference method = new MethodReference(
				reader.ReadString(),
				DeserializeTypeReference(reader),
				DeserializeTypeReference(reader)
				);

			int count = reader.ReadInt32();

			for (int i = 0; i < count; i++)
				method.Parameters.Add(DeserializeParameterDefinition(reader));

			return method;
		}

		public static void SerializeFieldReference(FieldReference field, BinaryWriter writer)
		{
			writer.Write(field.FullName);
			SerializeTypeReference(field.FieldType, writer);
			SerializeTypeReference(field.DeclaringType, writer);
		}

		public static FieldReference DeserializeFieldReference(BinaryReader reader)
			=> new FieldReference(
				reader.ReadString(),
				DeserializeTypeReference(reader),
				DeserializeTypeReference(reader)
				);
	}
}