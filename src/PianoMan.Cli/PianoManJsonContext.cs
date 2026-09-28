using System.Text.Json.Serialization;
using PianoMan.Core.Analysis;
using PianoMan.Core.Harmony;

namespace PianoMan.Cli;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(GameAnalysis))]
[JsonSerializable(typeof(HarmonySnapshot))]
internal sealed partial class PianoManJsonContext : JsonSerializerContext;
