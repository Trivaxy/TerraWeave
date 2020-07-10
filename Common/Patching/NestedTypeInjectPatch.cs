using Mono.Cecil;
using System.IO;

namespace Terraweave.Common.Patching
{
	public class NestedTypeInjectPatch : Patch
	{
		public TypeReference ParentType;
		public TypeDefinition NestedType;

		public NestedTypeInjectPatch(BinaryReader reader) => DeserializePatch(reader);

		public NestedTypeInjectPatch(TypeReference parentType, TypeDefinition nestedType)
		{
			ParentType = parentType;
			NestedType = nestedType;
		}

		public override void SerializePatch(BinaryWriter writer)
		{
			SerializingUtils.SerializeTypeReference(ParentType, writer);
			SerializingUtils.SerializeTypeDefinition(NestedType, writer);
		}

		public override Patch DeserializePatch(BinaryReader reader)
		{
			ParentType = SerializingUtils.DeserializeTypeReference(reader);
			NestedType = SerializingUtils.DeserializeTypeDefinition(reader);
			return this;
		}

		public override void Apply(ModuleDefinition terraria)
		{
			terraria.GetType(ParentType.FullName).NestedTypes.Add(NestedType);
		}
	}
}
