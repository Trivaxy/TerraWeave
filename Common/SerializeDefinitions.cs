using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.IO;
using System.Linq;

namespace Terraweave.Common
{
	public static partial class SerializingUtils
	{
		public static void SerializeTypeDefinition(TypeDefinition type, BinaryWriter writer)
		{
			writer.Write(type.Namespace);
			writer.Write(type.Name);
			writer.Write((uint)type.Attributes);

			writer.Write(type.HasNestedTypes);

			if (type.HasNestedTypes)
			{
				writer.Write(type.NestedTypes.Count);

				foreach (TypeDefinition nestedType in type.NestedTypes)
					SerializeTypeDefinition(nestedType, writer);
			}

			bool hasParent = !type.IsInterface && type.BaseType.FullName != "System.Object";
			writer.Write(hasParent);

			if (hasParent)
				SerializeTypeReference(type.BaseType, writer);

			writer.Write(type.HasGenericParameters);

			if (type.HasGenericParameters)
			{
				writer.Write(type.GenericParameters.Count);

				foreach (GenericParameter generic in type.GenericParameters)
					SerializeGenericParameter(generic, writer);
			}

			FieldDefinition[] fields = type.Fields.Where(field => !field.HasConstant).ToArray();

			writer.Write(fields.Length);

			foreach (FieldDefinition field in fields)
				SerializeFieldDefinition(field, writer);

			writer.Write(type.Methods.Count);

			foreach (MethodDefinition method in type.Methods)
				SerializeMethodDefinition(method, writer);

			writer.Write(type.Properties.Count);

			foreach (PropertyDefinition property in type.Properties)
				SerializePropertyDefinition(property, writer);
		}

		public static TypeDefinition DeserializeTypeDefinition(BinaryReader reader)
		{
			string @namespace = reader.ReadString();
			string typeName = reader.ReadString();

			TypeAttributes attributes = (TypeAttributes)reader.ReadUInt32();

			TypeDefinition type = new TypeDefinition(
				@namespace,
				typeName,
				attributes
				);

			if (reader.ReadBoolean())
			{
				int nestedTypeCount = reader.ReadInt32();

				for (int i = 0; i < nestedTypeCount; i++)
					type.NestedTypes.Add(DeserializeTypeDefinition(reader));
			}

			if (reader.ReadBoolean())
				type.BaseType = DeserializeTypeReference(reader);
			else if (type.IsInterface)
				type.BaseType = null;
			else
				type.BaseType = ModuleUtils.TerrariaModule.ImportReference(typeof(object));

			if (reader.ReadBoolean())
			{
				int genericParameterCount = reader.ReadInt32();

				for (int i = 0; i < genericParameterCount; i++)
					type.GenericParameters.Add(DeserializeGenericParameter(reader));
			}

			int fieldCount = reader.ReadInt32();

			for (int i = 0; i < fieldCount; i++)
				type.Fields.Add(DeserializeFieldDefinition(reader));

			int methodCount = reader.ReadInt32();

			for (int i = 0; i < methodCount; i++)
				type.Methods.Add(DeserializeMethodDefinition(reader));

			int propertyCount = reader.ReadInt32();

			for (int i = 0; i < propertyCount; i++)
			{
				PropertyDefinition property = DeserializePropertyDefinition(reader);

				string genericTypeName = type.Name.Contains('<') && !type.Name.Contains("AnonymousType") ? type.FullName + "." : "";

				property.GetMethod = type.Methods.Where(m => m.Name.EndsWith("get_" + property.Name)).First();

				MethodDefinition setter = type.Methods.Where(m => m.Name == "set_" + property.Name).FirstOrDefault();

				if (setter != null)
					property.SetMethod = setter;

				type.Properties.Add(property);
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
			FieldDefinition field = new FieldDefinition(
				reader.ReadString(),
				(FieldAttributes)reader.ReadUInt16(),
				DeserializeTypeReference(reader)
				);

			return field;
		}

		public static void SerializeMethodDefinition(MethodDefinition method, BinaryWriter writer)
		{
			writer.Write(method.Name);
			writer.Write((ushort)method.Attributes);
			SerializeTypeReference(method.ReturnType, writer);

			writer.Write((ushort)method.SemanticsAttributes);

			writer.Write(method.HasGenericParameters);

			if (method.HasGenericParameters)
			{
				writer.Write(method.GenericParameters.Count);

				foreach (GenericParameter generic in method.GenericParameters)
					SerializeGenericParameter(generic, writer);
			}

			writer.Write(method.Parameters.Count);

			foreach (ParameterDefinition parameter in method.Parameters)
				SerializeParameterDefinition(parameter, writer);

			writer.Write(method.HasBody);

			if (method.HasBody)
			{
				writer.Write(method.Body.Instructions.Count);

				foreach (Instruction instruction in method.Body.Instructions)
					SerializeInstruction(instruction, writer);
			}
		}

		public static MethodDefinition DeserializeMethodDefinition(BinaryReader reader)
		{
			MethodDefinition method = new MethodDefinition(
				reader.ReadString(),
				 (MethodAttributes)reader.ReadUInt16(),
				 DeserializeTypeReference(reader)
				);

			method.SemanticsAttributes = (MethodSemanticsAttributes)reader.ReadUInt16();

			if (reader.ReadBoolean())
			{
				int genericParameterCount = reader.ReadInt32();

				for (int i = 0; i < genericParameterCount; i++)
					method.GenericParameters.Add(DeserializeGenericParameter(reader));
			}

			int parameterCount = reader.ReadInt32();

			for (int i = 0; i < parameterCount; i++)
				method.Parameters.Add(DeserializeParameterDefinition(reader));

			if (reader.ReadBoolean())
			{
				int instructionCount = reader.ReadInt32();

				for (int i = 0; i < instructionCount; i++)
					method.Body.Instructions.Add(DeserializeInstruction(reader));
			}

			return method;
		}

		public static void SerializePropertyDefinition(PropertyDefinition property, BinaryWriter writer)
		{
			writer.Write(property.Name.Contains('.') ? property.Name.Split('.').Last() : property.Name);
			writer.Write((ushort)property.Attributes);
			SerializeTypeReference(property.PropertyType, writer);
		}

		public static PropertyDefinition DeserializePropertyDefinition(BinaryReader reader)
		{
			PropertyDefinition property = new PropertyDefinition(
				reader.ReadString(),
				(PropertyAttributes)reader.ReadUInt16(),
				DeserializeTypeReference(reader)
				);

			return property;
		}

		public static void SerializeCallSite(CallSite callSite, BinaryWriter writer)
			=> SerializeTypeReference(callSite.ReturnType, writer);

		public static CallSite DeserializeCallSite(BinaryReader reader)
			=> new CallSite(DeserializeTypeReference(reader));

		public static void SerializeVariableDefinition(VariableDefinition variable, BinaryWriter writer)
			=> SerializeTypeReference(variable.VariableType, writer);

		public static VariableDefinition DeserializeVariableDefinition(BinaryReader reader)
			=> new VariableDefinition(DeserializeTypeReference(reader));

		public static void SerializeParameterDefinition(ParameterDefinition parameter, BinaryWriter writer)
		{
			writer.Write(parameter.Name);
			writer.Write((ushort)parameter.Attributes);
			SerializeTypeReference(parameter.ParameterType, writer);
		}

		public static ParameterDefinition DeserializeParameterDefinition(BinaryReader reader)
			=> new ParameterDefinition(
				reader.ReadString(),
				(ParameterAttributes)reader.ReadUInt16(),
				DeserializeTypeReference(reader)
				);

		public static void SerializeGenericParameter(GenericParameter generic, BinaryWriter writer)
			=> SerializeTypeReference((TypeReference)generic.Owner, writer);

		public static GenericParameter DeserializeGenericParameter(BinaryReader reader)
			=> new GenericParameter(DeserializeTypeReference(reader));

		// i hate you, ldtoken
		public static void SerializeMetadataTokenProvider(IMetadataTokenProvider tokenProvider, BinaryWriter writer)
		{
			Type type = tokenProvider.GetType();

			if (type == typeof(TypeReference))
			{
				writer.Write((byte)0);
				SerializeTypeReference(tokenProvider as TypeReference, writer);
			}
			else if (type == typeof(MethodReference))
			{
				writer.Write((byte)1);
				SerializeMethodReference(tokenProvider as MethodReference, writer);
			}
			else if (type == typeof(FieldReference))
			{
				writer.Write((byte)2);
				SerializeFieldReference(tokenProvider as FieldReference, writer);
			}
			else if (type == typeof(TypeDefinition))
			{
				writer.Write((byte)3);
				SerializeTypeDefinition(tokenProvider as TypeDefinition, writer);
			}
			else if (type == typeof(MethodDefinition))
			{
				writer.Write((byte)4);
				SerializeMethodDefinition(tokenProvider as MethodDefinition, writer);
			}
			else if (type == typeof(FieldDefinition))
			{
				writer.Write((byte)5);
				SerializeFieldDefinition(tokenProvider as FieldDefinition, writer);
			}
		}

		public static T DeserializeMetadataTokenProvider<T>(BinaryReader reader)
			where T : IMetadataTokenProvider
			=> (T)DeserializeMetadataTokenProvider(reader);

		public static IMetadataTokenProvider DeserializeMetadataTokenProvider(BinaryReader reader)
		{
			byte providerType = reader.ReadByte();

			switch (providerType)
			{
				case 0:
					return DeserializeTypeReference(reader);

				case 1:
					return DeserializeMethodReference(reader);

				case 2:
					return DeserializeFieldReference(reader);

				case 3:
					return DeserializeTypeDefinition(reader);

				case 4:
					return DeserializeMethodDefinition(reader);

				case 5:
					return DeserializeFieldDefinition(reader);
			}

			throw new Exception("Could not deserialize IMetaDataTokenProvider. This should never happen");
		}
	}
}
