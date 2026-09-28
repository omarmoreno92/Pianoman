namespace PianoMan.Core.Harmony;

public sealed record MusicalChord(
    string Symbol,
    string Root,
    string Quality,
    string TensionClass,
    int[] MidiNotes);
