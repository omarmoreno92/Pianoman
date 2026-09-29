using PianoMan.Core.Chess;
using PianoMan.Core.Harmony;

namespace PianoMan.Core.Analysis;

public static class PerceptionAnalyzer
{
    public static PositionPerception Analyze(HarmonySnapshot harmony)
    {
        ArgumentNullException.ThrowIfNull(harmony);

        var whiteTension=PerspectiveTension(harmony,Color.White);
        var blackTension=PerspectiveTension(harmony,Color.Black);
        var globalScore=-Math.Abs(harmony.RelativeScore);

        return new PositionPerception(
            new HarmonicPerception(ListeningPerspective.Global,globalScore,harmony.Tension,ChordMapper.MapGlobal(harmony)),
            new HarmonicPerception(ListeningPerspective.White,harmony.RelativeScore,whiteTension,ChordMapper.MapPerspective(harmony,Color.White,whiteTension)),
            new HarmonicPerception(ListeningPerspective.Black,-harmony.RelativeScore,blackTension,ChordMapper.MapPerspective(harmony,Color.Black,blackTension)));
    }

    public static HarmonicPerception ForSide(PositionPerception perception,Color side)=>
        side==Color.White?perception.White:perception.Black;

    public static int HarmonyScore(HarmonySnapshot harmony,Color side)
    {
        var perception=ForSide(Analyze(harmony),side);
        return perception.Score-(perception.Tension/4);
    }

    private static int PerspectiveTension(HarmonySnapshot harmony,Color side)
    {
        var own=side==Color.White?harmony.White:harmony.Black;
        var opponent=side==Color.White?harmony.Black:harmony.White;
        var score=side==Color.White?harmony.RelativeScore:-harmony.RelativeScore;

        var pressureGap=Math.Max(0,opponent.Pressure-own.Pressure);
        var kingFragility=Math.Max(0,-own.KingSafety);
        var disadvantage=Math.Max(0,-score);

        return Math.Clamp(
            (harmony.Tension/2)
            +(pressureGap/2)
            +kingFragility
            +(disadvantage/4),
            0,
            255);
    }
}
