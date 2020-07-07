using Mono.Cecil;
using System.IO;

namespace Common
{
	public static class SerializingUtils
	{
		// TODO: make this accommodate for other stuff
		public static byte[] SerializeFieldDefinition(FieldDefinition field)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(stream))
				{
					writer.Write(field.Name);
					writer.Write(field.IsStatic);
					writer.Write(field.IsPublic);
					writer.Write(SerializeTypeReference(field.FieldType));

					return stream.ToArray();
				}
			}
		}

		// TODO: make this accommodate for other stuff
		public static byte[] SerializeTypeReference(TypeReference type)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(stream))
				{
					writer.Write(type.FullName);
					writer.Write(type.IsArray);
				}

				return stream.ToArray();
			}
		}
	}
}
