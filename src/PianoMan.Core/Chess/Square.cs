namespace PianoMan.Core.Chess;
public static class Square
{
    public static int File(int square)=>square&7; public static int Rank(int square)=>square>>3;
    public static bool IsValid(int square)=>square is >=0 and <64; public static bool IsValid(int file,int rank)=>file is >=0 and <8 && rank is >=0 and <8;
    public static int FromCoordinates(int file,int rank){if(!IsValid(file,rank))throw new ArgumentOutOfRangeException(nameof(file));return(rank<<3)|file;}
    public static string Name(int square){if(!IsValid(square))throw new ArgumentOutOfRangeException(nameof(square));return string.Create(2,square,static(s,v)=>{s[0]=(char)('a'+File(v));s[1]=(char)('1'+Rank(v));});}
    public static int Parse(ReadOnlySpan<char> value){if(!TryParse(value,out var s))throw new FormatException($"Invalid chess square: '{value.ToString()}'.");return s;}
    public static bool TryParse(ReadOnlySpan<char> value,out int square){if(value.Length==2&&value[0] is >= 'a' and <= 'h'&&value[1] is >= '1' and <= '8'){square=FromCoordinates(value[0]-'a',value[1]-'1');return true;}square=-1;return false;}
}
