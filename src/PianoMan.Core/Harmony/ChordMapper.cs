using PianoMan.Core.Chess;

namespace PianoMan.Core.Harmony;

public sealed class ChordMapper
{
    private static readonly string[] CircleOfFifths=["Ab","Eb","Bb","F","C","G","D","A","E"];

    public static MusicalChord Map(HarmonySnapshot harmony)=>MapGlobal(harmony);

    public static MusicalChord MapGlobal(HarmonySnapshot harmony)
    {
        ArgumentNullException.ThrowIfNull(harmony);
        var balanceTension=Math.Clamp(harmony.Tension+(Math.Abs(harmony.RelativeScore)/3),0,255);
        return MapVector(
            harmony.RelativeScore,
            balanceTension,
            harmony.White.Activity-harmony.Black.Activity,
            harmony.White.Coordination-harmony.Black.Coordination,
            harmony.White.KingSafety-harmony.Black.KingSafety,
            harmony.White.Space-harmony.Black.Space,
            harmony.White.Structure-harmony.Black.Structure,
            harmony.White.Pressure-harmony.Black.Pressure);
    }

    public static MusicalChord MapPerspective(HarmonySnapshot harmony,Color side,int tension)
    {
        ArgumentNullException.ThrowIfNull(harmony);
        var own=side==Color.White?harmony.White:harmony.Black;
        var opponent=side==Color.White?harmony.Black:harmony.White;
        var score=side==Color.White?harmony.RelativeScore:-harmony.RelativeScore;
        return MapVector(
            score,
            tension,
            own.Activity-opponent.Activity,
            own.Coordination-opponent.Coordination,
            own.KingSafety-opponent.KingSafety,
            own.Space-opponent.Space,
            own.Structure-opponent.Structure,
            own.Pressure-opponent.Pressure);
    }

    public static MusicalChord MapPerspective(int score,int tension)=>MapCore(score,tension);

    private static MusicalChord MapVector(
        int score,
        int tension,
        int activityGap,
        int coordinationGap,
        int kingSafetyGap,
        int spaceGap,
        int structureGap,
        int pressureGap)
    {
        var shift=Math.Clamp(score/80,-4,4);
        var root=CircleOfFifths[4+shift];
        var(q,baseIntervals,tc)=DescribeQuality(score,tension);
        var intervals=baseIntervals.ToList();
        var decorations=new List<string>();

        var tones=new List<FeatureTone>();
        AddTone(tones,spaceGap,4,spaceGap>=0?14:9,spaceGap>=0?"add9":"6");
        AddTone(tones,activityGap,4,activityGap>=0?21:17,activityGap>=0?"13":"11");
        AddTone(tones,coordinationGap,4,coordinationGap>=0?11:10,coordinationGap>=0?"maj7":"b7");
        AddTone(tones,structureGap,8,structureGap>=0?9:8,structureGap>=0?"6":"b6");
        AddTone(tones,pressureGap,8,pressureGap>=0?10:13,pressureGap>=0?"b7":"b9");
        if(Math.Abs(kingSafetyGap)>=12)
            tones.Add(new FeatureTone(Math.Abs(kingSafetyGap)/12d,18,"#11"));

        foreach(var tone in tones.OrderByDescending(x=>x.Strength).ThenBy(x=>x.Label,StringComparer.Ordinal))
        {
            if(decorations.Count>=2)break;
            var pitchClass=((tone.Interval%12)+12)%12;
            if(intervals.Any(i=>((i%12)+12)%12==pitchClass))continue;
            intervals.Add(tone.Interval);
            decorations.Add(tone.Label);
        }

        var midiRoot=60+PitchClass(root);
        var midi=intervals
            .Distinct()
            .OrderBy(i=>i)
            .Select(i=>midiRoot+i)
            .ToArray();
        var symbol=decorations.Count==0?$"{root}{q}":$"{root}{q}({string.Join(",",decorations)})";
        return new MusicalChord(symbol,root,q,tc,midi);
    }

    private static void AddTone(List<FeatureTone> tones,int gap,int threshold,int interval,string label)
    {
        var magnitude=Math.Abs(gap);
        if(magnitude<threshold)return;
        tones.Add(new FeatureTone(magnitude/(double)threshold,interval,label));
    }

    private static MusicalChord MapCore(int score,int tension)
    {
        var shift=Math.Clamp(score/120,-4,4);
        var root=CircleOfFifths[4+shift];
        var(q,intervals,tc)=DescribeQuality(score,tension);
        var midi=60+PitchClass(root);
        return new MusicalChord($"{root}{q}",root,q,tc,intervals.Select(i=>midi+i).ToArray());
    }

    private static (string,int[],string) DescribeQuality(int score,int tension)
    {
        if(tension>=140)return("dim7",[0,3,6,9],"critical");
        if(tension>=100)return("7(b9)",[0,4,7,10,13],"high");
        if(tension>=65)return("sus4(add9)",[0,5,7,14],"unresolved");
        if(score<-25)return("m",[0,3,7],tension<40?"stable":"moderate");
        if(score>25)return("maj7",[0,4,7,11],tension<40?"stable":"moderate");
        return("maj",[0,4,7],tension<40?"stable":"moderate");
    }

    private static int PitchClass(string n)=>n switch{"C"=>0,"D"=>2,"Eb"=>3,"E"=>4,"F"=>5,"G"=>7,"Ab"=>8,"A"=>9,"Bb"=>10,_=>throw new InvalidOperationException()};

    private readonly record struct FeatureTone(double Strength,int Interval,string Label);
}
