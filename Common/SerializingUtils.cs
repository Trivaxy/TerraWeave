using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Terraweave.Common
{
	// TODO: make all forms of type serialization accommodate generics and more access modifiers
	public static class SerializingUtils
	{
		public static ModuleDefinition TerrariaModule;

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

		public static void SerializeTypeDefinition(TypeDefinition type, BinaryWriter writer)
		{
			writer.Write(type.FullName);
			writer.Write((uint)type.Attributes);

			writer.Write(type.Fields.Count);

			foreach (FieldDefinition field in type.Fields)
				SerializeFieldDefinition(field, writer);

			writer.Write(type.Methods.Count);

			foreach (MethodDefinition method in type.Methods)
				SerializeMethodDefinition(method, writer);
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
				type.Fields.Add(DeserializeFieldDefinition(reader));

			int methodCount = reader.ReadInt32();

			for (int i = 0; i < methodCount; i++)
				type.Methods.Add(DeserializeMethodDefinition(reader));

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

			return TerrariaModule.ImportReference(type);
		}

		public static void SerializeMethodReference(MethodReference method, BinaryWriter writer)
		{
			writer.Write(method.FullName);
			SerializeTypeReference(method.ReturnType, writer);
		}

		public static MethodReference DeserializeMethodReference(BinaryReader reader)
			=> TerrariaModule.ImportReference(new MethodReference(reader.ReadString(), DeserializeTypeReference(reader)));

		public static void SerializeFieldReference(FieldReference field, BinaryWriter writer)
		{
			writer.Write(field.FullName);
			SerializeTypeReference(field.FieldType, writer);
		}

		public static FieldReference DeserializeFieldReference(BinaryReader reader)
			=> TerrariaModule.ImportReference(new FieldReference(reader.ReadString(), DeserializeTypeReference(reader)));

		public static void SerializeMethodDefinition(MethodDefinition method, BinaryWriter writer)
		{
			writer.Write(method.Name);
			writer.Write((ushort)method.Attributes);
			SerializeTypeReference(method.ReturnType, writer);
			writer.Write(method.Body.Instructions.Count);

			foreach (Instruction instruction in method.Body.Instructions)
				SerializeInstruction(instruction, writer);
		}

		public static MethodDefinition DeserializeMethodDefinition(BinaryReader reader)
		{
			MethodDefinition method = new MethodDefinition(
				reader.ReadString(),
				 (MethodAttributes)reader.ReadUInt16(),
				 DeserializeTypeReference(reader)
				);

			int instructionCount = reader.ReadInt32();

			for (int i = 0; i < instructionCount; i++)
				method.Body.Instructions.Add(DeserializeInstruction(reader));

			return method;
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
			else if (operandType == OperandType.InlineSwitch)
			{
				Instruction[] instructions = new Instruction[reader.ReadInt32()];

				for (int i = 0; i < instructions.Length; i++)
					instructions[i] = DeserializeInstruction(reader);
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
			else if (operandObjType == typeof(Instruction))
				return Instruction.Create(finalOpCode, operand as Instruction);
			else if (operandObjType == typeof(Instruction[]))
				return Instruction.Create(finalOpCode, operand as Instruction[]);

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
			=> SerializeTypeReference(parameter.ParameterType, writer);

		public static ParameterDefinition DeserializeParameterDefinition(BinaryReader reader)
			=> new ParameterDefinition(DeserializeTypeReference(reader));
	}
}