using System.Text;
namespace PianoMan.Core.Chess;
public static class PgnReader
{
    private static readonly HashSet<string> Results=new(StringComparer.Ordinal){"1-0","0-1","1/2-1/2","*"};
    public static PgnGame Read(string content){var games=ReadMany(content);if(games.Count!=1)throw new FormatException($"Expected exactly one PGN game, found {games.Count}.");return games[0];}
    public static IReadOnlyList<PgnGame> ReadMany(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);var chunks=SplitGames(content);var games=new List<PgnGame>(chunks.Count);foreach(var chunk in chunks)games.Add(ParseGame(chunk));return games;
    }
    public static PgnGame ReadFile(string path)=>Read(File.ReadAllText(path));
    private static List<string> SplitGames(string content)
    {
        var lines=content.Replace("\r\n","\n",StringComparison.Ordinal).Split('\n');var chunks=new List<string>();var current=new StringBuilder();var sawMoves=false;
        foreach(var line in lines){var trimmed=line.Trim();if(trimmed.StartsWith('[')&&sawMoves&&current.Length>0){chunks.Add(current.ToString());current.Clear();sawMoves=false;}if(trimmed.Length>0&&!trimmed.StartsWith('['))sawMoves=true;current.AppendLine(line);}if(current.ToString().Trim().Length>0)chunks.Add(current.ToString());return chunks;
    }
    private static PgnGame ParseGame(string content)
    {
        var headers=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);var text=new StringBuilder();using var reader=new StringReader(content);while(reader.ReadLine() is{} line){var t=line.Trim();if(t.StartsWith('[')&&t.EndsWith(']'))ParseHeader(t,headers);else text.AppendLine(line);}var cleaned=RemoveCommentsAndVariations(text.ToString());var moves=new List<string>();foreach(var raw in cleaned.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries)){var token=StripMoveNumber(raw);if(token.Length==0||token=="..."||token.StartsWith('$')||Results.Contains(token)||token.Equals("e.p.",StringComparison.OrdinalIgnoreCase))continue;moves.Add(token);}if(moves.Count==0)throw new FormatException("The PGN does not contain any main-line moves.");return new PgnGame(headers,moves);
    }
    private static void ParseHeader(string line,Dictionary<string,string> headers){var sep=line.IndexOf(' ');var first=line.IndexOf('"');var last=line.LastIndexOf('"');if(sep<=1||first<sep||last<=first)throw new FormatException($"Invalid PGN header: {line}");headers[line[1..sep]]=line[(first+1)..last].Replace("\\\"","\"",StringComparison.Ordinal).Replace("\\\\","\\",StringComparison.Ordinal);}
    private static string RemoveCommentsAndVariations(string value){var r=new StringBuilder(value.Length);var braces=0;var vars=0;var line=false;foreach(var c in value){if(line){if(c=='\n'){line=false;r.Append(' ');}continue;}if(c==';'&&braces==0&&vars==0){line=true;continue;}if(c=='{'){braces++;continue;}if(c=='}'&&braces>0){braces--;if(braces==0)r.Append(' ');continue;}if(braces>0)continue;if(c=='('){vars++;continue;}if(c==')'&&vars>0){vars--;if(vars==0)r.Append(' ');continue;}if(vars==0)r.Append(c);}if(braces!=0||vars!=0)throw new FormatException("The PGN contains an unterminated comment or variation.");return r.ToString();}
    private static string StripMoveNumber(string token){var dot=token.LastIndexOf('.');if(dot<0)return token;foreach(var c in token.AsSpan(0,dot+1))if(!char.IsAsciiDigit(c)&&c!='.')return token;return token[(dot+1)..];}
}
