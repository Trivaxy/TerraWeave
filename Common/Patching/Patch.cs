using Mono.Cecil;

namespace Terraweave.Common.Patching
{
	public abstract class Patch<T>
	{
		public Patch() { }

		public abstract byte[] SerializePatch();

		public abstract T DeserializePatch(byte[] data);

		public abstract void Apply(ModuleDefinition terraria);
	}
}
