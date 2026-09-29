using PianoMan.Core.Chess;
using PianoMan.Core.Harmony;

namespace PianoMan.Core.Analysis;

public static class PerceptionAnalyzer
{
    public static PositionPerception Analyze(HarmonySnapshot harmony,SonificationContext? context=null)
    {
        ArgumentNullException.ThrowIfNull(harmony);

        context??=new SonificationContext(false,null);
        var globalTension=AudibleTension(harmony,context);
        var whiteTension=PerspectiveTension(harmony,Color.White,globalTension);
        var blackTension=PerspectiveTension(harmony,Color.Black,globalTension);
        var globalScore=-Math.Abs(harmony.RelativeScore);

        return new PositionPerception(
            new HarmonicPerception(ListeningPerspective.Global,globalScore,globalTension,harmony.Tension,ChordMapper.MapGlobal(harmony,globalTension)),
            new HarmonicPerception(ListeningPerspective.White,harmony.RelativeScore,whiteTension,harmony.Tension,ChordMapper.MapPerspective(harmony,Color.White,whiteTension)),
            new HarmonicPerception(ListeningPerspective.Black,-harmony.RelativeScore,blackTension,harmony.Tension,ChordMapper.MapPerspective(harmony,Color.Black,blackTension)));
    }

    public static HarmonicPerception ForSide(PositionPerception perception,Color side)=>
        side==Color.White?perception.White:perception.Black;

    public static int HarmonyScore(HarmonySnapshot harmony,Color side)
    {
        var perception=ForSide(Analyze(harmony),side);
        return perception.Score-(perception.Tension/4);
    }

    private static int AudibleTension(HarmonySnapshot harmony,SonificationContext context)
    {
        if(context.InTheory)return 0;
        if(context.MoveLoss is not int loss)return Math.Min(40,harmony.Tension/6);
        return loss switch
        {
            <=0=>0,
            <=15=>4,
            <=40=>12,
            <=80=>30,
            <=160=>70,
            _=>Math.Clamp(110+((loss-160)/4),110,200)
        };
    }

    private static int PerspectiveTension(HarmonySnapshot harmony,Color side,int globalTension)
    {
        var own=side==Color.White?harmony.White:harmony.Black;
        var opponent=side==Color.White?harmony.Black:harmony.White;
        var score=side==Color.White?harmony.RelativeScore:-harmony.RelativeScore;

        var pressureGap=Math.Max(0,opponent.Pressure-own.Pressure);
        var kingFragility=Math.Max(0,-own.KingSafety);
        var disadvantage=Math.Max(0,-score);

        return Math.Clamp(
            globalTension
            +(pressureGap/8)
            +(kingFragility/4)
            +((disadvantage+7)/8),
            0,
            255);
    }
}
