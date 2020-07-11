using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
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

		public enum PrimitiveType : byte
		{
			Byte,
			SByte,
			Short,
			UShort,
			Int,
			UInt,
			Long,
			ULong,
			Boolean,
			String // yes, i know string is not a primitive type, but it'd be a pain to separate it from here
		}

		public static Dictionary<Type, PrimitiveType> TypeToPrimitive = new Dictionary<Type, PrimitiveType>()
		{
			{ typeof(byte), PrimitiveType.Byte },
			{ typeof(sbyte), PrimitiveType.SByte },
			{ typeof(short), PrimitiveType.Short },
			{ typeof(ushort), PrimitiveType.UShort },
			{ typeof(int), PrimitiveType.Int },
			{ typeof(uint), PrimitiveType.UInt },
			{ typeof(long), PrimitiveType.Long },
			{ typeof(ulong), PrimitiveType.ULong },
			{ typeof(bool), PrimitiveType.Boolean },
			{ typeof(string), PrimitiveType.String }
		};

		public static void SerializePrimitive(object primitive, BinaryWriter writer)
		{
			Type type = primitive.GetType();

			writer.Write((byte)TypeToPrimitive[type]);

			if (type == typeof(byte))
				writer.Write((byte)primitive);
			else if (type == typeof(sbyte))
				writer.Write((sbyte)primitive);
			else if (type == typeof(short))
				writer.Write((short)primitive);
			else if (type == typeof(ushort))
				writer.Write((ushort)primitive);
			else if (type == typeof(int))
				writer.Write((int)primitive);
			else if (type == typeof(uint))
				writer.Write((uint)primitive);
			else if (type == typeof(long))
				writer.Write((long)primitive);
			else if (type == typeof(ulong))
				writer.Write((ulong)primitive);
			else if (type == typeof(bool))
				writer.Write((bool)primitive);
			else if (type == typeof(string))
				writer.Write((string)primitive);
			else
				throw new Exception("Failed to serialize constant - this should never happen");
		}

		public static object DeserializePrimitive(BinaryReader reader)
		{
			PrimitiveType primitiveType = (PrimitiveType)reader.ReadByte();

			switch (primitiveType)
			{
				case PrimitiveType.Byte:
					return reader.ReadByte();

				case PrimitiveType.SByte:
					return reader.ReadSByte();

				case PrimitiveType.Short:
					return reader.ReadInt16();

				case PrimitiveType.UShort:
					return reader.ReadUInt16();

				case PrimitiveType.Int:
					return reader.ReadInt32();

				case PrimitiveType.UInt:
					return reader.ReadUInt32();

				case PrimitiveType.Long:
					return reader.ReadInt64();

				case PrimitiveType.ULong:
					return reader.ReadUInt64();

				case PrimitiveType.Boolean:
					return reader.ReadBoolean();

				case PrimitiveType.String:
					return reader.ReadString();
			}

			throw new Exception("failed to deserialize primitive - this should never happen");
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
	}
}
