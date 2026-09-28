using PianoMan.Core.Chess;
using PianoMan.Core.Harmony;

namespace PianoMan.Core.Analysis;

public static class GameAnalyzer
{
    public static GameAnalysis Analyze(PgnGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        var position = game.CreateInitialPosition();
        var timeline = new List<MoveAnalysis>(game.Moves.Count + 1);
        AddTimelineEntry(timeline, position, 0, 0, "-", "(start)", "-");

        for (var index = 0; index < game.Moves.Count; index++)
        {
            var san = game.Moves[index];
            var side = position.SideToMove;
            Move move;
            try
            {
                move = SanParser.Parse(position, san);
            }
            catch (Exception exception) when (exception is FormatException or InvalidOperationException)
            {
                throw new InvalidOperationException(
                    $"Could not parse ply {index + 1} ('{san}') from FEN '{position.ToFen()}'.",
                    exception);
            }

            position.Apply(move);
            AddTimelineEntry(
                timeline,
                position,
                index + 1,
                (index / 2) + 1,
                side.ToString(),
                san,
                move.ToUci());
        }

        var white = game.Header("White");
        var black = game.Header("Black");
        var title = white is { Length: > 0 } && black is { Length: > 0 }
            ? $"{white} vs {black}"
            : game.Header("Event") ?? "Untitled game";

        return new GameAnalysis(
            title,
            game.Header("Event"),
            white,
            black,
            game.Header("Result"),
            timeline,
            position.ToFen());
    }

    public static (HarmonySnapshot Harmony, MusicalChord Chord) Analyze(Position position)
    {
        var harmony = HarmonyAnalyzer.Analyze(position);
        return (harmony, ChordMapper.Map(harmony));
    }

    private static void AddTimelineEntry(
        List<MoveAnalysis> timeline,
        Position position,
        int ply,
        int moveNumber,
        string side,
        string san,
        string uci)
    {
        var harmony = HarmonyAnalyzer.Analyze(position);
        timeline.Add(new MoveAnalysis(
            ply,
            moveNumber,
            side,
            san,
            uci,
            position.ToFen(),
            harmony,
            ChordMapper.Map(harmony)));
    }
}
