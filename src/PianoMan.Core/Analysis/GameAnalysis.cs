namespace PianoMan.Core.Analysis;

public sealed record GameAnalysis(
    string Title,
    string? Event,
    string? White,
    string? Black,
    string? Result,
    List<MoveAnalysis> Timeline,
    string FinalFen);
