using Mono.Cecil;
using System.IO;

namespace Terraweave.Common.Patching
{
	public class TypeInjectPatch : Patch
	{
		public TypeDefinition InjectedType;

		public TypeInjectPatch(BinaryReader reader) => DeserializePatch(reader);

		public TypeInjectPatch(TypeDefinition type) => InjectedType = type;

		public override void SerializePatch(BinaryWriter writer)
			=> SerializingUtils.SerializeTypeDefinition(InjectedType, writer);

		public override Patch DeserializePatch(BinaryReader reader)
		{
			InjectedType = SerializingUtils.DeserializeTypeDefinition(reader);
			return this;
		}

		public override void Apply(ModuleDefinition terraria) => terraria.Types.Add(InjectedType);
	}
}