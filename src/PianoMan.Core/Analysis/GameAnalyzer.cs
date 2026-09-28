using PianoMan.Core.Chess;using PianoMan.Core.Harmony;using PianoMan.Core.Theory;
namespace PianoMan.Core.Analysis;
public static class GameAnalyzer
{
    public static GameAnalysis Analyze(PgnGame game,TheoryBook? book=null)
    {
        ArgumentNullException.ThrowIfNull(game);book??=TryLoadBook();var position=game.CreateInitialPosition();var timeline=new List<MoveAnalysis>(game.Moves.Count+1);AddPosition(timeline,position,0,0,"-","(start)","-",null,book,null);
        for(var i=0;i<game.Moves.Count;i++)
        {
            var san=game.Moves[i];var side=position.SideToMove;Move move;try{move=SanParser.Parse(position,san);}catch(Exception ex) when(ex is FormatException or InvalidOperationException){throw new InvalidOperationException($"Could not parse ply {i+1} ('{san}') from FEN '{position.ToFen()}'.",ex);}var decision=AnalyzeMove(position,move,book);var from=Square.Name(move.From);var to=Square.Name(move.To);position.Apply(move);AddPosition(timeline,position,i+1,(i/2)+1,side.ToString(),san,move.ToUci(),decision,book,(from,to));
        }
        var white=game.Header("White");var black=game.Header("Black");var title=white is{Length:>0}&&black is{Length:>0}?$"{white} vs {black}":game.Header("Event")??"Untitled game";return new GameAnalysis(title,game.Header("Event"),white,black,game.Header("Result"),timeline,position.ToFen());
    }
    public static (HarmonySnapshot Harmony,MusicalChord Chord) Analyze(Position p){var h=HarmonyAnalyzer.Analyze(p);return(h,ChordMapper.Map(h));}
    public static MoveDecision AnalyzeMove(Position position,Move played,TheoryBook? book=null)
    {
        book??=TryLoadBook();var lookup=book?.Lookup(position);var packed=PackedMove.Pack(played);var theoryMove=lookup?.Continuations.FirstOrDefault(c=>c.PackedMove==packed);
        if(theoryMove is not null)
        {
            var ordered=lookup!.Continuations.OrderByDescending(c=>c.Weight).ThenBy(c=>c.PackedMove).ToArray();var candidates=new List<CandidateAnalysis>(ordered.Length);for(var i=0;i<ordered.Length;i++){var move=PackedMove.Resolve(position,ordered[i].PackedMove);var next=position.Clone();next.Apply(move);var h=HarmonyAnalyzer.Analyze(next);candidates.Add(new CandidateAnalysis(i+1,SanFormatter.Format(position,move),move.ToUci(),null,ordered[i].Weight,null,ChordMapper.Map(h),h.Tension,move==played));}var rank=Array.FindIndex(ordered,c=>c.PackedMove==packed)+1;return new MoveDecision(AnalysisMode.Theory,"TEORÍA · SIN BÚSQUEDA",ordered.Length,rank,null,null,lookup.Eco,lookup.Name,candidates);
        }
        var side=position.SideToMove;var all=new List<(Move Move,int Score,HarmonySnapshot Harmony,MusicalChord Chord)>();foreach(var move in MoveGenerator.GenerateLegalMoves(position)){var next=position.Clone();next.Apply(move);var h=HarmonyAnalyzer.Analyze(next);var score=side==Color.White?h.RelativeScore:-h.RelativeScore;all.Add((move,score,h,ChordMapper.Map(h)));}var sorted=all.OrderByDescending(x=>x.Score).ThenBy(x=>x.Move.ToUci(),StringComparer.Ordinal).ToArray();var best=sorted.Length==0?0:sorted[0].Score;var playedIndex=Array.FindIndex(sorted,x=>x.Move==played);if(playedIndex<0)throw new InvalidOperationException("Played move was not legal.");var loss=Math.Max(0,best-sorted[playedIndex].Score);var selected=sorted.Take(10).ToList();if(playedIndex>=10)selected.Add(sorted[playedIndex]);var candidates2=new List<CandidateAnalysis>(selected.Count);foreach(var item in selected){var originalRank=Array.FindIndex(sorted,x=>x.Move==item.Move)+1;candidates2.Add(new CandidateAnalysis(originalRank,SanFormatter.Format(position,item.Move),item.Move.ToUci(),item.Score,null,Math.Max(0,best-item.Score),item.Chord,item.Harmony.Tension,item.Move==played));}return new MoveDecision(AnalysisMode.Tuning,"Afinando…",sorted.Length,playedIndex+1,loss,Classify(loss),lookup?.Eco,lookup?.Name,candidates2);
    }
    public static PianoMoveClass Classify(int loss)=>loss switch{0=>PianoMoveClass.Best,<=15=>PianoMoveClass.Excellent,<=40=>PianoMoveClass.Good,<=80=>PianoMoveClass.Inaccuracy,<=160=>PianoMoveClass.Mistake,_=>PianoMoveClass.Blunder};
    private static void AddPosition(List<MoveAnalysis> timeline,Position p,int ply,int moveNumber,string side,string san,string uci,MoveDecision? decision,TheoryBook? book,(string From,string To)? last)
    {
        var h=HarmonyAnalyzer.Analyze(p);var lookup=book?.Lookup(p);timeline.Add(new MoveAnalysis(ply,moveNumber,side,san,uci,p.ToFen(),last?.From,last?.To,h,ChordMapper.Map(h),lookup?.Eco,lookup?.Name,lookup is not null,decision));
    }
    private static TheoryBook? TryLoadBook(){try{return TheoryBook.LoadEmbedded();}catch{return null;}}
}
