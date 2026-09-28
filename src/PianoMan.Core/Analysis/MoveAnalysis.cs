using PianoMan.Core.Harmony;

namespace PianoMan.Core.Analysis;

public sealed record MoveAnalysis(
    int Ply,
    int MoveNumber,
    string Side,
    string San,
    string Uci,
    string Fen,
    HarmonySnapshot Harmony,
    MusicalChord Chord);
