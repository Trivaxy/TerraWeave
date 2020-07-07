using Mono.Cecil;
using System;
using System.IO;

namespace Terraweave.Common
{
	// TODO: make all forms of type serialization accommodate generics and more access modifiers
	public static class SerializingUtils
	{
		public static ModuleDefinition TerrariaModule;

		public static void SerializeTypeDefinition(TypeDefinition type, BinaryWriter writer)
		{
			writer.Write(type.FullName);
			writer.Write((uint)type.Attributes);
			writer.Write(type.Fields.Count);

			foreach (FieldDefinition field in type.Fields)
				SerializeFieldDefinition(field, writer);
		}

		public static TypeDefinition DeserializeTypeDefinition(BinaryReader reader)
		{
			string typeName = reader.ReadString();
			string @namespace = typeName.Split('.')[0];

			TypeAttributes attributes = (TypeAttributes)reader.ReadUInt32();

			TypeDefinition type = new TypeDefinition(
				@namespace,
				typeName.Substring(@namespace.Length + 1),
				attributes
				);

			int fieldCount = reader.ReadInt32();

			for (int i = 0; i < fieldCount; i++)
			{
				FieldDefinition field = DeserializeFieldDefinition(reader);
				type.Fields.Add(field);
			}

			return type;
		}

		public static void SerializeFieldDefinition(FieldDefinition field, BinaryWriter writer)
		{
			writer.Write(field.Name);
			writer.Write((ushort)field.Attributes);
			SerializeTypeReference(field.FieldType, writer);
		}

		public static FieldDefinition DeserializeFieldDefinition(BinaryReader reader)
		{
			string fieldName = reader.ReadString();
			FieldAttributes attributes = (FieldAttributes)reader.ReadUInt16();

			FieldDefinition field = new FieldDefinition(
				fieldName,
				attributes,
				DeserializeTypeReference(reader)
				);

			return field;
		}

		public static void SerializeTypeReference(TypeReference type, BinaryWriter writer) => writer.Write(type.FullName);

		public static TypeReference DeserializeTypeReference(BinaryReader reader)
		{
			string typeName = reader.ReadString();
			string @namespace = typeName.Split('.')[0];

			TypeReference type = new TypeReference(
				@namespace,
				typeName.Substring(@namespace.Length + 1),
				TerrariaModule,
				TerrariaModule
				);

			return type;
		}
	}
}
