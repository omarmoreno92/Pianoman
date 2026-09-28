using PianoMan.Core.Chess;using PianoMan.Core.Harmony;
namespace PianoMan.Core.Analysis;
public enum AnalysisMode:byte { Theory=0,Tuning=1 }
public enum PianoMoveClass:byte { Best=0,Excellent=1,Good=2,Inaccuracy=3,Mistake=4,Blunder=5 }
public sealed record CandidateAnalysis(int Rank,string San,string Uci,int? Score,uint? Weight,int? Loss,MusicalChord Chord,int Tension,bool Played);
public sealed record MoveDecision(AnalysisMode Mode,string Label,int EvaluatedMoveCount,int? Rank,int? Loss,PianoMoveClass? Classification,string? Eco,string? Opening,IReadOnlyList<CandidateAnalysis> Candidates);
public sealed record MoveAnalysis(int Ply,int MoveNumber,string Side,string San,string Uci,string Fen,string? From,string? To,HarmonySnapshot Harmony,MusicalChord Chord,string? Eco,string? Opening,bool InTheory,MoveDecision? Decision);
public sealed record GameAnalysis(string Title,string? Event,string? White,string? Black,string? Result,List<MoveAnalysis> Timeline,string FinalFen);
