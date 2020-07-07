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
		{
			SerializingUtils.SerializeTypeDefinition(InjectedType, writer);

			writer.Write(InjectedType.Methods.Count);

			foreach (MethodDefinition method in InjectedType.Methods)
				SerializingUtils.SerializeMethodDefinition(method, writer);
		}

		public override Patch DeserializePatch(BinaryReader reader)
		{
			InjectedType = SerializingUtils.DeserializeTypeDefinition(reader);

			int methodsCount = reader.ReadInt32();

			for (int i = 0; i < methodsCount; i++)
			{
				InjectedType.Methods.Add(SerializingUtils.DeserializeMethodDefinition(reader));
			}

			return this;
		}

		public override void Apply(ModuleDefinition terraria) => terraria.Types.Add(InjectedType);
	}
}