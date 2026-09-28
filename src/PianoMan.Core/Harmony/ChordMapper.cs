namespace PianoMan.Core.Harmony;

public sealed class ChordMapper
{
    private static readonly string[] CircleOfFifths =
        ["Ab", "Eb", "Bb", "F", "C", "G", "D", "A", "E"];

    public static MusicalChord Map(HarmonySnapshot harmony)
    {
        ArgumentNullException.ThrowIfNull(harmony);

        var tonalShift = Math.Clamp(harmony.RelativeScore / 120, -4, 4);
        var root = CircleOfFifths[4 + tonalShift];
        var (quality, intervals, tensionClass) = DescribeQuality(harmony);
        var rootMidi = 60 + PitchClass(root);
        var notes = intervals.Select(interval => rootMidi + interval).ToArray();
        return new MusicalChord(
            $"{root}{quality}",
            root,
            quality,
            tensionClass,
            notes);
    }

    private static (string Quality, int[] Intervals, string TensionClass) DescribeQuality(
        HarmonySnapshot harmony)
    {
        if (harmony.Tension >= 140)
        {
            return ("dim7", [0, 3, 6, 9], "critical");
        }

        if (harmony.Tension >= 100)
        {
            return ("7(b9)", [0, 4, 7, 10, 13], "high");
        }

        if (harmony.Tension >= 65)
        {
            return ("sus4(add9)", [0, 5, 7, 14], "unresolved");
        }

        if (harmony.RelativeScore < -25)
        {
            return ("m", [0, 3, 7], harmony.Tension < 40 ? "stable" : "moderate");
        }

        if (harmony.RelativeScore > 25)
        {
            return ("maj7", [0, 4, 7, 11], harmony.Tension < 40 ? "stable" : "moderate");
        }

        return ("maj", [0, 4, 7], harmony.Tension < 40 ? "stable" : "moderate");
    }

    private static int PitchClass(string note) => note switch
    {
        "C" => 0,
        "D" => 2,
        "Eb" => 3,
        "E" => 4,
        "F" => 5,
        "G" => 7,
        "Ab" => 8,
        "A" => 9,
        "Bb" => 10,
        _ => throw new InvalidOperationException($"Unsupported tonal root: {note}.")
    };
}
