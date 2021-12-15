using System.Text.Json.Serialization;

namespace ReplaysToCSV
{
	[JsonSerializable(typeof(ReplayInfo))]
	internal partial class SourceGenerationContext : JsonSerializerContext
	{
	}
}
