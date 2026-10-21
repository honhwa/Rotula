using Rotula.App.Editing;
using Rotula.App.Localization;
using Rotula.App.ViewModels;
using Rotula.Core.Elements;
using Rotula.Core.Model;
using Rotula.Rendering.View;

namespace Rotula.Tests;

/// <summary>
/// Dragging a tool from the toolbox onto the label: the new element has to land on the point the user released it on,
/// snapped to the grid when that is on, and never off the paper.
/// </summary>
[Collection("App")]
public class AppToolDropTests : IDisposable
{
    private readonly MainViewModel _vm;

    public AppToolDropTests()
    {
        Loc.Instance.SetLanguage("en");
        _vm = new MainViewModel(new FakeServices().Build());
        _vm.SnapToGrid = false; // exact coordinates; grid snapping has its own test below
    }

    public void Dispose() => Loc.Instance.SetLanguage("en");

    private LabelDocument Doc => _vm.Session.Document;

    private RectD BoundsOf(LabelElement e) => _vm.Session.Bounds.GetBounds(e);

    [Fact]
    public void DroppedElementStandsWithItsTopLeftCornerOnTheDropPoint()
    {
        _vm.AddElementAt(ToolboxKind.Box, 300, 400);

        var box = Assert.IsType<BoxElement>(Doc.Elements.Single());
        var bounds = BoundsOf(box);
        Assert.Equal(300, bounds.X);
        Assert.Equal(400, bounds.Y);
    }

    [Fact]
    public void DropFarOutsideThePaperClampsTheElementBackOntoTheLabel()
    {
        _vm.AddElementAt(ToolboxKind.Box, Doc.WidthDots + 400, Doc.HeightDots + 400);

        var bounds = BoundsOf(Doc.Elements.Single());
        Assert.True(bounds.X >= 0, "left of the paper");
        Assert.True(bounds.Y >= 0, "above the paper");
        Assert.True(bounds.Right <= Doc.WidthDots, "right of the paper");
        Assert.True(bounds.Bottom <= Doc.HeightDots, "below the paper");
    }

    [Fact]
    public void DropSnapsToTheGridWhenSnappingIsOn()
    {
        _vm.SnapToGrid = true;
        double step = _vm.Grid.SpacingDots((int)Doc.Dpi);
        Assert.True(step > 1);

        // 13 is deliberately off-grid, so a snapped result proves the step was applied. The step is not a whole number of dots
        // (1 mm is 7.99 dots at 203 dpi), so "on the grid" means a whole number of steps away from the origin.
        _vm.AddElementAt(ToolboxKind.Line, 13, 13);

        var bounds = BoundsOf(Doc.Elements.Single());
        AssertOnGrid(bounds.X, step);
        AssertOnGrid(bounds.Y, step);
    }

    private static void AssertOnGrid(double value, double step)
    {
        double offBy = Math.Abs(value / step - Math.Round(value / step));
        Assert.True(offBy < 0.01, $"{value} is not on a grid line (step {step})");
    }

    [Fact]
    public void DroppedElementIsSelectedAndOneUndoRemovesIt()
    {
        _vm.AddElementAt(ToolboxKind.Circle, 200, 200);

        var element = Doc.Elements.Single();
        Assert.Equal(new[] { element.Id }, _vm.Session.SelectedIds);

        _vm.Session.PerformUndo();
        Assert.Empty(Doc.Elements);
    }

    [Fact]
    public void EveryToolboxKindLandsOnTheDropPoint()
    {
        foreach (var kind in Enum.GetValues<ToolboxKind>())
        {
            if (kind is ToolboxKind.Image or ToolboxKind.QrCode) continue; // a file dialog / the QR builder open first
            _vm.AddElementAt(kind, 120, 150);

            var bounds = BoundsOf(Doc.Elements.Last());
            Assert.True(Math.Abs(bounds.X - 120) <= 1, $"{kind} landed at {bounds.X}");
            Assert.True(Math.Abs(bounds.Y - 150) <= 1, $"{kind} landed at {bounds.Y}");

            _vm.Session.PerformUndo();
        }
    }

    [Fact]
    public void ClickingAToolStillAddsAtTheStaggeredDefaultPosition()
    {
        _vm.AddElementCommand.Execute(ToolboxKind.Box);

        var box = Assert.IsType<BoxElement>(Doc.Elements.Single());
        Assert.Equal(24, box.X);
        Assert.Equal(24, box.Y);
    }

    [Fact]
    public void PlacementKeepsAThinLineOnTheDropPoint()
    {
        var doc = new LabelDocument();
        var line = new LineElement { Direction = LineDirection.Horizontal, LengthDots = 200, ThicknessDots = 3 };

        DropPlacement.PlaceAt(line, new RectD(0, 0, 200, 3), 40, 60, doc, new GridSettings(SnapEnabled: false));

        Assert.Equal(40, line.X);
        Assert.Equal(60, line.Y);
    }

    [Fact]
    public void PlacementCompensatesAFrameThatStartsLeftAboveTheOrigin()
    {
        var doc = new LabelDocument();
        // A ^FT text is anchored at the bottom-left of its box, so its ink starts left of and above the origin.
        var text = new TextElement { Text = "Text", X = 100, Y = 100, OriginKind = FieldOriginKind.Typeset };

        DropPlacement.PlaceAt(text, new RectD(90, 88, 50, 20), 200, 300, doc, new GridSettings(SnapEnabled: false));

        // The frame (90, 88) has to move to (200, 300): the origin follows by the same delta.
        Assert.Equal(210, text.X);
        Assert.Equal(312, text.Y);
    }

    [Fact]
    public void PlacementNeverPutsAnElementOutsideTheLabelEvenWhenSnapped()
    {
        var doc = new LabelDocument();
        var box = new BoxElement { WidthDots = 50, HeightDots = 50 };
        // 1 mm at 203 dpi is 8 dots, and the label is not a whole number of steps wide, so snapping can overshoot the far edge.
        var grid = new GridSettings(LengthUnit.Millimeters, 5, Visible: true, SnapEnabled: true);

        DropPlacement.PlaceAt(box, new RectD(0, 0, 50, 50), doc.WidthDots, doc.HeightDots, doc, grid);

        Assert.True(box.X >= 0 && box.X + 50 <= doc.WidthDots);
        Assert.True(box.Y >= 0 && box.Y + 50 <= doc.HeightDots);
    }
}
