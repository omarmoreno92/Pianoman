namespace PianoMan.Core.Harmony;

public readonly record struct SideHarmony(
    int Material,
    int Activity,
    int Coordination,
    int KingSafety,
    int Space,
    int Structure,
    int Pressure,
    int Initiative)
{
    public int Total =>
        Material +
        Activity +
        Coordination +
        KingSafety +
        Space +
        Structure +
        Pressure +
        Initiative;
}
