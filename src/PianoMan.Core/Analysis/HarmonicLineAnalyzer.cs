using PianoMan.Core.Chess;
using PianoMan.Core.Harmony;
using PianoMan.Core.Theory;

namespace PianoMan.Core.Analysis;

public static class HarmonicLineAnalyzer
{
    public static HarmonicPrincipalLine Analyze(Position initial,TheoryBook? book=null,int maxPlies=8)
    {
        ArgumentNullException.ThrowIfNull(initial);
        maxPlies=Math.Clamp(maxPlies,1,24);
        var position=initial.Clone();
        var line=new List<PrincipalLineMove>(maxPlies);
        var extensions=0;
        var stopReason="Profundidad armónica alcanzada.";

        for(var ply=1;ply<=maxPlies;ply++)
        {
            var legal=MoveGenerator.GenerateLegalMoves(position);
            if(legal.Count==0)
            {
                stopReason=MoveGenerator.IsKingInCheck(position,position.SideToMove)?"Jaque mate.":"Tablas por ahogado.";
                break;
            }

            var side=position.SideToMove;
            var theory=book?.Lookup(position);
            var seed=theory is{Continuations.Count:>0}
                ?PackedMove.Resolve(position,theory.Continuations[0].PackedMove)
                :legal[0];
            var decision=GameAnalyzer.AnalyzeMove(position,seed,book);
            var selected=UciParser.Parse(position,decision.RecommendedUci??legal[0].ToUci());
            var selective=IsAnomaly(position,selected);
            if(selective)
            {
                selected=SelectWithReplyExtension(position,decision,side);
                extensions++;
            }

            var entry=GameAnalyzer.AnalyzeContinuation(position,selected,ply,book);
            line.Add(new PrincipalLineMove(
                ply,
                side.ToString(),
                entry.San,
                entry.Uci,
                entry.Fen,
                entry.Decision?.Mode==AnalysisMode.Theory,
                selective,
                side==Color.White?entry.Harmony.RelativeScore:-entry.Harmony.RelativeScore,
                entry.Perception.Global.Tension,
                entry.Perception.Global.Chord));
            position=Fen.Parse(entry.Fen);
        }

        return new HarmonicPrincipalLine(line,extensions,stopReason);
    }

    private static Move SelectWithReplyExtension(Position position,MoveDecision decision,Color originalSide)
    {
        var candidates=decision.Candidates
            .Select(candidate=>UciParser.Parse(position,candidate.Uci))
            .Distinct()
            .ToArray();

        return candidates
            .Select(move=>new
            {
                Move=move,
                Score=ExtendedScore(position,move,originalSide)
            })
            .OrderByDescending(item=>item.Score)
            .ThenBy(item=>item.Move.ToUci(),StringComparer.Ordinal)
            .First().Move;
    }

    private static int ExtendedScore(Position position,Move move,Color originalSide)
    {
        var next=position.Clone();
        next.Apply(move);
        var replies=MoveGenerator.GenerateLegalMoves(next);
        if(replies.Count==0)return Evaluate(next,originalSide);
        var worst=int.MaxValue;
        foreach(var reply in replies)
        {
            var afterReply=next.Clone();
            afterReply.Apply(reply);
            worst=Math.Min(worst,Evaluate(afterReply,originalSide));
        }
        return worst;
    }

    private static int Evaluate(Position position,Color side)
    {
        var harmony=HarmonyAnalyzer.Analyze(position);
        var perception=PerceptionAnalyzer.ForSide(
            PerceptionAnalyzer.Analyze(harmony,new SonificationContext(false,0)),
            side);
        var score=side==Color.White?harmony.RelativeScore:-harmony.RelativeScore;
        return score-(perception.Tension/4);
    }

    private static bool IsAnomaly(Position position,Move move)
    {
        var moving=position[move.From];
        var beforeMaterial=position.Pieces().Sum(entry=>HarmonyAnalyzer.PieceValue(entry.Piece.Type));
        var next=position.Clone();
        next.Apply(move);
        var afterMaterial=next.Pieces().Sum(entry=>HarmonyAnalyzer.PieceValue(entry.Piece.Type));
        var suddenMaterial=beforeMaterial-afterMaterial>=HarmonyAnalyzer.PieceValue(PieceType.Rook);
        var givesCheck=MoveGenerator.IsKingInCheck(next,next.SideToMove);
        var queenExposure=moving.Type==PieceType.Queen
            &&MoveGenerator.IsSquareAttacked(next,move.To,moving.Color.Opposite())
            &&!MoveGenerator.IsSquareAttacked(next,move.To,moving.Color);
        var tacticalEnergy=HarmonyAnalyzer.Analyze(next).Tension>=100;
        return suddenMaterial||givesCheck||queenExposure||move.Promotion!=PieceType.None||tacticalEnergy;
    }
}
