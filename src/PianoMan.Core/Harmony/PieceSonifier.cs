using PianoMan.Core.Analysis;
using PianoMan.Core.Chess;

namespace PianoMan.Core.Harmony;

public static class PieceSonifier
{
    public static PositionVoicing Map(Position position,PositionPerception perception,PieceMovementContext? movement=null)
    {
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(perception);

        return new PositionVoicing(
            Build(position,perception.Global,null,movement),
            Build(position,perception.White,Color.White,movement),
            Build(position,perception.Black,Color.Black,movement));
    }

    private static List<PieceVoice> Build(Position position,HarmonicPerception perception,Color? listener,PieceMovementContext? movement)
    {
        var pitchClasses=perception.Chord.MidiNotes
            .Select(note=>((note%12)+12)%12)
            .Distinct()
            .OrderBy(note=>note)
            .ToArray();
        if(pitchClasses.Length==0)pitchClasses=[0,4,7];

        var voices=new List<PieceVoice>(32);
        foreach(var(square,piece) in position.Pieces().OrderBy(entry=>entry.Square))
        {
            var squareName=Square.Name(square);
            var moved=movement?.To==squareName;
            var harmonicOffset=moved?DissonantOffset(movement!.Dissonance,Square.File(square)):0;
            var anchor=Register(piece)+Square.File(square)+(Square.Rank(square)*2)+TypeOffset(piece.Type);
            var midi=NearestChordTone(anchor,pitchClasses)+harmonicOffset;
            var discomfort=listener==piece.Color?perception.Tension:0;
            var detune=discomfort==0?0:Math.Min(18,discomfort/7d)*(Square.File(square)%2==0?-1:1);
            if(moved&&movement!.Dissonance>0)detune+=(Square.File(square)%2==0?-1:1)*Math.Min(35,movement.Dissonance/2d);
            var velocity=piece.Type switch
            {
                PieceType.Pawn=>0.52,
                PieceType.Knight or PieceType.Bishop=>0.62,
                PieceType.Rook=>0.69,
                PieceType.Queen=>0.76,
                PieceType.King=>0.72,
                _=>0.5
            };
            if(moved)velocity=Math.Min(0.92,velocity+0.12);

            voices.Add(new PieceVoice(
                $"{piece.Color}-{piece.Type}-{squareName}",
                squareName,
                piece.Color.ToString(),
                piece.Type.ToString(),
                midi,
                Math.Round(detune,2),
                velocity,
                moved,
                harmonicOffset));
        }
        return voices;
    }

    private static int Register(Piece piece)=>piece.Color==Color.White?52:40;

    private static int TypeOffset(PieceType type)=>type switch
    {
        PieceType.Pawn=>0,
        PieceType.Knight=>2,
        PieceType.Bishop=>4,
        PieceType.Rook=>-3,
        PieceType.Queen=>7,
        PieceType.King=>-7,
        _=>0
    };

    private static int DissonantOffset(int dissonance,int file)
    {
        var magnitude=dissonance switch
        {
            <4=>0,
            <12=>1,
            <30=>1,
            <70=>2,
            <110=>6,
            _=>11
        };
        return file%2==0?-magnitude:magnitude;
    }

    private static int NearestChordTone(int target,IReadOnlyList<int> pitchClasses)
    {
        var best=Math.Clamp(target,28,96);
        var distance=int.MaxValue;
        for(var note=28;note<=96;note++)
        {
            if(!pitchClasses.Contains(note%12))continue;
            var candidateDistance=Math.Abs(note-target);
            if(candidateDistance>=distance)continue;
            best=note;
            distance=candidateDistance;
        }
        return best;
    }
}
