using Mono.Cecil.Cil;

namespace Terraweave.Common.Data
{
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
}
