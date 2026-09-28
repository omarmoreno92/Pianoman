using System.Text.Json.Serialization;
using PianoMan.Core.Analysis;
using PianoMan.Core.Theory;
namespace PianoMan.Cli;
[JsonSourceGenerationOptions(WriteIndented=true,PropertyNamingPolicy=JsonKnownNamingPolicy.CamelCase,UseStringEnumConverter=true)]
[JsonSerializable(typeof(GameAnalysis))]
[JsonSerializable(typeof(GameAnalysis[]))]
[JsonSerializable(typeof(TheoryBookMetadata))]
internal partial class PianoManJsonContext : JsonSerializerContext;
