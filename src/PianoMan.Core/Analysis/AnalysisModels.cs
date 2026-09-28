using PianoMan.Core.Harmony;
namespace PianoMan.Core.Analysis;

public enum AnalysisMode:byte { Theory=0,Tuning=1 }
public enum PianoMoveClass:byte { Best=0,Excellent=1,Good=2,Inaccuracy=3,Mistake=4,Blunder=5 }
public enum PianoManPersona:byte { PianoMan=0,RadioKiller=1 }
public enum ListeningPerspective:byte { Global=0,White=1,Black=2 }

public sealed record HarmonicPerception(ListeningPerspective Perspective,int Score,int Tension,MusicalChord Chord);
public sealed record PositionPerception(HarmonicPerception Global,HarmonicPerception White,HarmonicPerception Black);
public sealed record PerceptionDelta(int GlobalScore,int WhiteScore,int BlackScore,int GlobalTension,int WhiteTension,int BlackTension);

public sealed record CandidateAnalysis(
    int Rank,
    string San,
    string Uci,
    int? Score,
    uint? Weight,
    int? Loss,
    int HarmonyScore,
    PositionPerception Perception,
    MusicalChord Chord,
    int Tension,
    bool Played,
    bool Recommended);

public sealed record MoveDecision(
    AnalysisMode Mode,
    string Label,
    PianoManPersona Persona,
    int EvaluatedMoveCount,
    int? Rank,
    int? Loss,
    PianoMoveClass? Classification,
    bool TriggersRadioKillerForOpponent,
    string? RecommendedUci,
    string? Eco,
    string? Opening,
    IReadOnlyList<CandidateAnalysis> Candidates);

public sealed record MoveAnalysis(
    int Ply,
    int MoveNumber,
    string Side,
    string San,
    string Uci,
    string Fen,
    string? From,
    string? To,
    HarmonySnapshot Harmony,
    PositionPerception Perception,
    PerceptionDelta? PerceptionDelta,
    MusicalChord Chord,
    string? Eco,
    string? Opening,
    bool InTheory,
    MoveDecision? Decision);

public sealed record GameAnalysis(string Title,string? Event,string? White,string? Black,string? Result,List<MoveAnalysis> Timeline,string FinalFen);
