using PianoMan.Core.Chess;
using PianoMan.Core.Harmony;
using PianoMan.Core.Theory;

namespace PianoMan.Core.Analysis;

public static class GameAnalyzer
{
    private const int RadioKillerActivationLoss=81;
    private const int CorrectMoveWindow=15;

    public static GameAnalysis Analyze(PgnGame game,TheoryBook? book=null)
    {
        ArgumentNullException.ThrowIfNull(game);
        book??=TryLoadBook();
        var position=game.CreateInitialPosition();
        var timeline=new List<MoveAnalysis>(game.Moves.Count+1);
        var personaBySide=new Dictionary<Color,PianoManPersona>
        {
            [Color.White]=PianoManPersona.PianoMan,
            [Color.Black]=PianoManPersona.PianoMan
        };

        AddPosition(timeline,position,0,0,"-","(start)","-",null,book,null);

        for(var i=0;i<game.Moves.Count;i++)
        {
            var san=game.Moves[i];
            var side=position.SideToMove;
            Move move;
            try
            {
                move=SanParser.Parse(position,san);
            }
            catch(Exception ex) when(ex is FormatException or InvalidOperationException)
            {
                throw new InvalidOperationException($"Could not parse ply {i+1} ('{san}') from FEN '{position.ToFen()}'.",ex);
            }

            var requestedPersona=personaBySide[side];
            var decision=AnalyzeMove(position,move,book,requestedPersona);
            var from=Square.Name(move.From);
            var to=Square.Name(move.To);
            position.Apply(move);

            if(decision.TriggersRadioKillerForOpponent)
                personaBySide[side.Opposite()]=PianoManPersona.RadioKiller;

            if(personaBySide[side.Opposite()]==PianoManPersona.RadioKiller)
            {
                var nextHarmony=HarmonyAnalyzer.Analyze(position);
                var nextScore=side.Opposite()==Color.White?nextHarmony.RelativeScore:-nextHarmony.RelativeScore;
                if(nextScore<=15)
                    personaBySide[side.Opposite()]=PianoManPersona.PianoMan;
            }

            AddPosition(timeline,position,i+1,(i/2)+1,side.ToString(),san,move.ToUci(),decision,book,(from,to));
        }

        var white=game.Header("White");
        var black=game.Header("Black");
        var title=white is{Length:>0}&&black is{Length:>0}?$"{white} vs {black}":game.Header("Event")??"Untitled game";
        return new GameAnalysis(title,game.Header("Event"),white,black,game.Header("Result"),timeline,position.ToFen());
    }

    public static (HarmonySnapshot Harmony,MusicalChord Chord) Analyze(Position p)
    {
        var h=HarmonyAnalyzer.Analyze(p);
        return(h,ChordMapper.MapGlobal(h));
    }

    public static MoveDecision AnalyzeMove(Position position,Move played,TheoryBook? book=null,PianoManPersona? requestedPersona=null)
    {
        book??=TryLoadBook();
        var lookup=book?.Lookup(position);
        var packed=PackedMove.Pack(played);
        var theoryMove=lookup?.Continuations.FirstOrDefault(c=>c.PackedMove==packed);
        var side=position.SideToMove;

        if(theoryMove is not null)
        {
            var projected=lookup!.Continuations.Select(c=>ProjectTheory(position,side,c)).ToArray();
            var ordered=projected
                .OrderByDescending(x=>x.HarmonyScore)
                .ThenByDescending(x=>x.Weight)
                .ThenBy(x=>x.Move.ToUci(),StringComparer.Ordinal)
                .ToArray();

            var recommended=ordered[0].Move.ToUci();
            var candidates=new List<CandidateAnalysis>(ordered.Length);
            for(var i=0;i<ordered.Length;i++)
            {
                var item=ordered[i];
                candidates.Add(new CandidateAnalysis(
                    i+1,
                    SanFormatter.Format(position,item.Move),
                    item.Move.ToUci(),
                    null,
                    item.Weight,
                    null,
                    item.HarmonyScore,
                    item.Perception,
                    item.Chord,
                    item.Tension,
                    item.Move==played,
                    item.Move.ToUci()==recommended));
            }

            var rank=Array.FindIndex(ordered,x=>x.Move==played)+1;
            return new MoveDecision(
                AnalysisMode.Theory,
                "TEORÍA · SIN BÚSQUEDA",
                PianoManPersona.PianoMan,
                ordered.Length,
                rank,
                null,
                null,
                false,
                recommended,
                lookup.Eco,
                lookup.Name,
                candidates);
        }

        var persona=ResolvePersona(position,requestedPersona);
        var all=new List<CandidateState>();

        foreach(var move in MoveGenerator.GenerateLegalMoves(position))
        {
            var next=position.Clone();
            next.Apply(move);
            var harmony=HarmonyAnalyzer.Analyze(next);
            var score=side==Color.White?harmony.RelativeScore:-harmony.RelativeScore;
            var perception=PerceptionAnalyzer.Analyze(harmony);
            var own=PerceptionAnalyzer.ForSide(perception,side);
            var harmonyScore=score-(own.Tension/4);
            all.Add(new CandidateState(move,score,harmonyScore,harmony,perception,own.Chord,own.Tension,null));
        }

        var sorted=all.OrderByDescending(x=>x.Score).ThenBy(x=>x.Move.ToUci(),StringComparer.Ordinal).ToArray();
        var best=sorted.Length==0?0:sorted[0].Score;
        var playedIndex=Array.FindIndex(sorted,x=>x.Move==played);
        if(playedIndex<0)throw new InvalidOperationException("Played move was not legal.");

        var loss=Math.Max(0,best-sorted[playedIndex].Score);
        var correct=sorted.Where(x=>best-x.Score<=CorrectMoveWindow).ToArray();
        var recommendedState=persona==PianoManPersona.RadioKiller
            ? correct.OrderByDescending(x=>PerceptionAnalyzer.ForSide(x.Perception,side.Opposite()).Tension)
                .ThenByDescending(x=>x.HarmonyScore)
                .ThenByDescending(x=>x.Score)
                .ThenBy(x=>x.Move.ToUci(),StringComparer.Ordinal)
                .First()
            : correct.OrderByDescending(x=>x.HarmonyScore)
                .ThenByDescending(x=>x.Score)
                .ThenBy(x=>x.Move.ToUci(),StringComparer.Ordinal)
                .First();

        var recommendedUci=recommendedState.Move.ToUci();
        var selected=sorted.Take(10).ToList();
        if(playedIndex>=10)selected.Add(sorted[playedIndex]);

        var candidates=new List<CandidateAnalysis>(selected.Count);
        foreach(var item in selected)
        {
            var originalRank=Array.FindIndex(sorted,x=>x.Move==item.Move)+1;
            candidates.Add(new CandidateAnalysis(
                originalRank,
                SanFormatter.Format(position,item.Move),
                item.Move.ToUci(),
                item.Score,
                null,
                Math.Max(0,best-item.Score),
                item.HarmonyScore,
                item.Perception,
                item.Chord,
                item.Tension,
                item.Move==played,
                item.Move.ToUci()==recommendedUci));
        }

        return new MoveDecision(
            AnalysisMode.Tuning,
            "Afinando…",
            persona,
            sorted.Length,
            playedIndex+1,
            loss,
            Classify(loss),
            loss>=RadioKillerActivationLoss,
            recommendedUci,
            lookup?.Eco,
            lookup?.Name,
            candidates);
    }

    public static PianoMoveClass Classify(int loss)=>loss switch
    {
        0=>PianoMoveClass.Best,
        <=15=>PianoMoveClass.Excellent,
        <=40=>PianoMoveClass.Good,
        <=80=>PianoMoveClass.Inaccuracy,
        <=160=>PianoMoveClass.Mistake,
        _=>PianoMoveClass.Blunder
    };

    private static PianoManPersona ResolvePersona(Position position,PianoManPersona? requested)
    {
        if(requested==PianoManPersona.RadioKiller)
        {
            var h=HarmonyAnalyzer.Analyze(position);
            var score=position.SideToMove==Color.White?h.RelativeScore:-h.RelativeScore;
            if(score>15)return PianoManPersona.RadioKiller;
        }
        return PianoManPersona.PianoMan;
    }

    private static CandidateState ProjectTheory(Position position,Color side,TheoryContinuation continuation)
    {
        var move=PackedMove.Resolve(position,continuation.PackedMove);
        var next=position.Clone();
        next.Apply(move);
        var harmony=HarmonyAnalyzer.Analyze(next);
        var perception=PerceptionAnalyzer.Analyze(harmony);
        var own=PerceptionAnalyzer.ForSide(perception,side);
        var score=side==Color.White?harmony.RelativeScore:-harmony.RelativeScore;
        return new CandidateState(move,score,score-(own.Tension/4),harmony,perception,own.Chord,own.Tension,continuation.Weight);
    }

    private static void AddPosition(List<MoveAnalysis> timeline,Position p,int ply,int moveNumber,string side,string san,string uci,MoveDecision? decision,TheoryBook? book,(string From,string To)? last)
    {
        var h=HarmonyAnalyzer.Analyze(p);
        var perception=PerceptionAnalyzer.Analyze(h);
        var lookup=book?.Lookup(p);
        PerceptionDelta? delta=null;

        if(timeline.Count>0)
        {
            var before=timeline[^1].Perception;
            delta=new PerceptionDelta(
                perception.Global.Score-before.Global.Score,
                perception.White.Score-before.White.Score,
                perception.Black.Score-before.Black.Score,
                perception.Global.Tension-before.Global.Tension,
                perception.White.Tension-before.White.Tension,
                perception.Black.Tension-before.Black.Tension);
        }

        timeline.Add(new MoveAnalysis(
            ply,
            moveNumber,
            side,
            san,
            uci,
            p.ToFen(),
            last?.From,
            last?.To,
            h,
            perception,
            delta,
            perception.Global.Chord,
            lookup?.Eco,
            lookup?.Name,
            lookup is not null,
            decision));
    }

    private static TheoryBook? TryLoadBook()
    {
        try{return TheoryBook.LoadEmbedded();}
        catch{return null;}
    }

    private sealed record CandidateState(
        Move Move,
        int Score,
        int HarmonyScore,
        HarmonySnapshot Harmony,
        PositionPerception Perception,
        MusicalChord Chord,
        int Tension,
        uint? Weight);
}
