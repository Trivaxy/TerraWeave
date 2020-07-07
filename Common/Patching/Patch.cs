using Mono.Cecil;
using System.IO;

namespace Terraweave.Common.Patching
{
	public abstract class Patch
	{
		public abstract void SerializePatch(BinaryWriter writer);

		public abstract Patch DeserializePatch(BinaryReader reader);

		public abstract void Apply(ModuleDefinition terraria);
	}
}
