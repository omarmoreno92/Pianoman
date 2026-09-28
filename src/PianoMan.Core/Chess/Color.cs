namespace PianoMan.Core.Chess;

public enum Color : byte
{
    White = 0,
    Black = 1
}

public static class ColorExtensions
{
    public static Color Opposite(this Color color) =>
        color == Color.White ? Color.Black : Color.White;
}
