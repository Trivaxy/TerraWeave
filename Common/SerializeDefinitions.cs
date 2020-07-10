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

			writer.Write(type.Fields.Count);

			foreach (FieldDefinition field in type.Fields)
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

		public static void SerializeInstruction(Instruction instruction, BinaryWriter writer)
		{
			Code opCode = instruction.OpCode.Code;
			OperandType operandType = instruction.OpCode.OperandType;
			object operand = instruction.Operand;

			writer.Write((int)opCode);
			writer.Write((int)operandType);

			switch (operandType)
			{
				case OperandType.InlineType:
					SerializeTypeReference(operand as TypeReference, writer);
					break;

				case OperandType.InlineMethod:
					SerializeMethodReference(operand as MethodReference, writer);
					break;

				case OperandType.InlineField:
					SerializeFieldReference(operand as FieldReference, writer);
					break;
			}

			if (operandType == OperandType.InlineString)
				writer.Write(operand as string);
			else if (operandType == OperandType.ShortInlineI)
			{
				if (opCode == Code.Ldc_I4_S)
					writer.Write((sbyte)operand);
				else
					writer.Write((byte)operand);
			}
			else if (operandType == OperandType.InlineI)
				writer.Write((int)operand);
			else if (operandType == OperandType.InlineI8)
				writer.Write((long)operand);
			else if (operandType == OperandType.ShortInlineR)
				writer.Write((float)operand);
			else if (operandType == OperandType.InlineR)
				writer.Write((double)operand);
			else if (opCode == Code.Calli)
				SerializeCallSite(operand as CallSite, writer);
			else if (operandType == OperandType.InlineVar || operandType == OperandType.ShortInlineVar)
				SerializeVariableDefinition(operand as VariableDefinition, writer);
			else if (operandType == OperandType.InlineTok)
				SerializeMetadataTokenProvider(operand as IMetadataTokenProvider, writer);
			else if (operandType == OperandType.InlineArg || operandType == OperandType.ShortInlineArg)
				SerializeParameterDefinition(operand as ParameterDefinition, writer);
			else if (operandType == OperandType.InlineBrTarget || operandType == OperandType.ShortInlineBrTarget)
				SerializeInstruction(operand as Instruction, writer);
			else if (operandType == OperandType.InlineSwitch)
			{
				Instruction[] instructions = operand as Instruction[];
				writer.Write(instructions.Length);

				foreach (Instruction instr in instructions)
					SerializeInstruction(instr, writer);
			}
		}

		public static Instruction DeserializeInstruction(BinaryReader reader)
		{
			Code opCode = (Code)reader.ReadInt32();
			OperandType operandType = (OperandType)reader.ReadInt32();
			object operand = null;

			#region Instruction stuff
			switch (operandType)
			{
				case OperandType.InlineType:
					operand = DeserializeTypeReference(reader);
					break;

				case OperandType.InlineMethod:
					operand = DeserializeMethodReference(reader);
					break;

				case OperandType.InlineField:
					operand = DeserializeFieldReference(reader);
					break;
			}

			if (operandType == OperandType.InlineString)
				operand = reader.ReadString();
			else if (operandType == OperandType.ShortInlineI)
			{
				if (opCode == Code.Ldc_I4_S)
					operand = reader.ReadSByte();
				else
					operand = reader.ReadByte();
			}
			else if (operandType == OperandType.InlineI)
				operand = reader.ReadInt32();
			else if (operandType == OperandType.InlineI8)
				operand = reader.ReadInt64();
			else if (operandType == OperandType.ShortInlineR)
				operand = reader.ReadSingle();
			else if (operandType == OperandType.InlineR)
				operand = reader.ReadDouble();
			else if (opCode == Code.Calli)
				operand = DeserializeCallSite(reader);
			else if (operandType == OperandType.InlineVar || operandType == OperandType.ShortInlineVar)
				operand = DeserializeVariableDefinition(reader);
			else if (operandType == OperandType.InlineArg || operandType == OperandType.ShortInlineArg)
				operand = DeserializeParameterDefinition(reader);
			else if (operandType == OperandType.InlineBrTarget || operandType == OperandType.ShortInlineBrTarget)
				operand = DeserializeInstruction(reader);
			else if (operandType == OperandType.InlineTok)
				operand = DeserializeMetadataTokenProvider(reader);
			else if (operandType == OperandType.InlineSwitch)
			{
				Instruction[] instructions = new Instruction[reader.ReadInt32()];

				for (int i = 0; i < instructions.Length; i++)
					instructions[i] = DeserializeInstruction(reader);

				operand = instructions;
			}

			OpCode finalOpCode = CodeToOpCode[opCode];

			if (operand == null)
				return Instruction.Create(finalOpCode);

			Type operandObjType = operand.GetType();

			if (operandObjType == typeof(TypeReference))
				return Instruction.Create(finalOpCode, operand as TypeReference);
			else if (operandObjType == typeof(MethodReference))
				return Instruction.Create(finalOpCode, operand as MethodReference);
			else if (operandObjType == typeof(FieldReference))
				return Instruction.Create(finalOpCode, operand as FieldReference);
			else if (operandObjType == typeof(string))
				return Instruction.Create(finalOpCode, operand as string);
			else if (operandObjType == typeof(sbyte))
				return Instruction.Create(finalOpCode, (sbyte)operand);
			else if (operandObjType == typeof(byte))
				return Instruction.Create(finalOpCode, (byte)operand);
			else if (operandObjType == typeof(int))
				return Instruction.Create(finalOpCode, (int)operand);
			else if (operandObjType == typeof(long))
				return Instruction.Create(finalOpCode, (long)operand);
			else if (operandObjType == typeof(float))
				return Instruction.Create(finalOpCode, (float)operand);
			else if (operandObjType == typeof(double))
				return Instruction.Create(finalOpCode, (double)operand);
			else if (operandObjType == typeof(CallSite))
				return Instruction.Create(finalOpCode, operand as CallSite);
			else if (operandObjType == typeof(VariableDefinition))
				return Instruction.Create(finalOpCode, operand as VariableDefinition);
			else if (operandObjType == typeof(ParameterDefinition))
				return Instruction.Create(finalOpCode, operand as ParameterDefinition);
			else if (operand is IMetadataTokenProvider)
			{
				return Instruction.Create(finalOpCode, operand as FieldDefinition);
			}
			else if (operandObjType == typeof(Instruction))
				return Instruction.Create(finalOpCode, operand as Instruction);
			else if (operandObjType == typeof(Instruction[]))
				return Instruction.Create(finalOpCode, operand as Instruction[]);
			#endregion

			throw new Exception("Failed to deserialize instruction: this should never happen");
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
