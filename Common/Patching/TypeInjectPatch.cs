using Common;
using Mono.Cecil;
using System.IO;

namespace Terraweave.Common.Patching
{
	public class TypeInjectPatch : Patch<TypeInjectPatch>
	{
		public TypeDefinition InjectedType;

		public TypeInjectPatch() => InjectedType = null;

		public TypeInjectPatch(TypeDefinition type) => InjectedType = type;

		public override byte[] SerializePatch()
		{
			using (MemoryStream stream = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(stream))
				{
					writer.Write(InjectedType.FullName);
					writer.Write(InjectedType.Fields.Count);

					foreach (FieldDefinition field in InjectedType.Fields)
						writer.Write(SerializingUtils.SerializeFieldDefinition(field));
				}

				return stream.ToArray();
			}
		}

		public override TypeInjectPatch DeserializePatch(byte[] data)
		{
			throw new System.NotImplementedException();
		}

		public override void Apply(ModuleDefinition terraria)
		{

		}
	}
}
