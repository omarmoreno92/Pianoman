namespace PianoMan.Core.Harmony;
public sealed class ChordMapper
{
    private static readonly string[] CircleOfFifths=["Ab","Eb","Bb","F","C","G","D","A","E"];
    public static MusicalChord Map(HarmonySnapshot harmony){ArgumentNullException.ThrowIfNull(harmony);var shift=Math.Clamp(harmony.RelativeScore/120,-4,4);var root=CircleOfFifths[4+shift];var(q,intervals,tc)=DescribeQuality(harmony);var midi=60+PitchClass(root);return new MusicalChord($"{root}{q}",root,q,tc,intervals.Select(i=>midi+i).ToArray());}
    private static (string,int[],string) DescribeQuality(HarmonySnapshot h){if(h.Tension>=140)return("dim7",[0,3,6,9],"critical");if(h.Tension>=100)return("7(b9)",[0,4,7,10,13],"high");if(h.Tension>=65)return("sus4(add9)",[0,5,7,14],"unresolved");if(h.RelativeScore<-25)return("m",[0,3,7],h.Tension<40?"stable":"moderate");if(h.RelativeScore>25)return("maj7",[0,4,7,11],h.Tension<40?"stable":"moderate");return("maj",[0,4,7],h.Tension<40?"stable":"moderate");}
    private static int PitchClass(string n)=>n switch{"C"=>0,"D"=>2,"Eb"=>3,"E"=>4,"F"=>5,"G"=>7,"Ab"=>8,"A"=>9,"Bb"=>10,_=>throw new InvalidOperationException()};
}
