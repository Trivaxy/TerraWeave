using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.IO;

namespace Terraweave.Common.Patching
{
	public class MethodModifyPatch : Patch
	{
		public enum InstructionAction : byte
		{
			Add,
			Remove,
			Modify
		}

		public struct MethodChange
		{
			public InstructionAction Action;
			public Instruction Instruction;
			public int Index;

			public MethodChange(InstructionAction action, Instruction instruction, int index)
			{
				Action = action;
				Instruction = instruction;
				Index = index;
			}
		}

		public static MethodReference Method;
		public static MethodChange[] Changes;

		public MethodModifyPatch(BinaryReader reader) => DeserializePatch(reader);

		public MethodModifyPatch(MethodReference method, MethodChange[] changes)
		{
			Method = method;
			Changes = changes;
		}

		public override void SerializePatch(BinaryWriter writer)
		{
			SerializingUtils.SerializeMethodReference(Method, writer);

			writer.Write(Changes.Length);

			foreach (MethodChange change in Changes)
			{
				writer.Write((byte)change.Action);
				SerializingUtils.SerializeInstruction(change.Instruction, writer);
				writer.Write(change.Index);
			}
		}

		public override Patch DeserializePatch(BinaryReader reader)
		{
			Method = SerializingUtils.DeserializeMethodReference(reader);

			Changes = new MethodChange[reader.ReadInt32()];

			for (int i = 0; i < Changes.Length; i++)
				Changes[i] = new MethodChange(
					(InstructionAction)reader.ReadByte(),
					SerializingUtils.DeserializeInstruction(reader),
					reader.ReadInt32());

			return this;
		}

		public override void Apply(ModuleDefinition terraria)
		{
			MethodDefinition method = Method.Resolve();

			foreach (MethodChange change in Changes)
				switch (change.Action)
				{
					case InstructionAction.Add:
						method.Body.Instructions.Insert(change.Index, change.Instruction);
						break;

					case InstructionAction.Remove:
						method.Body.Instructions.RemoveAt(change.Index);
						break;

					case InstructionAction.Modify:
						method.Body.Instructions[change.Index] = change.Instruction;
						break;

					default:
						throw new Exception("Failed to modify method instruction. This should never happen");
				}
		}
	}
}