using PianoMan.Core.Chess;

namespace PianoMan.Tests;

public sealed class SanFormatterTests
{
    [Fact]
    public void FormatsBasicPawnMove()
    {
        var position = Position.Initial;
        var move = SanParser.Parse(position, "e4");
        Assert.Equal("e4", SanFormatter.Format(position, move));
    }

    [Fact]
    public void FormatsBasicKnightMove()
    {
        var position = Position.Initial;
        position.Apply(SanParser.Parse(position, "e4"));
        position.Apply(SanParser.Parse(position, "e5"));
        var move = SanParser.Parse(position, "Nf3");
        Assert.Equal("Nf3", SanFormatter.Format(position, move));
    }

    [Fact]
    public void FormatsCheckmateSuffix()
    {
        var position = Position.Initial;
        foreach (var san in new[] { "e4", "e5", "Bc4", "Nc6", "Qh5", "Nf6" })
        {
            position.Apply(SanParser.Parse(position, san));
        }

        var move = SanParser.Parse(position, "Qxf7#");
        Assert.Equal("Qxf7#", SanFormatter.Format(position, move));
    }
}
