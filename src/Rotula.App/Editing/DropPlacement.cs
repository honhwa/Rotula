using Rotula.Core.Elements;
using Rotula.Core.Model;
using Rotula.Rendering.View;

namespace Rotula.App.Editing;

/// <summary>
/// Where a new element lands when a tool is dragged from the toolbox onto the label: the top-left corner of the element's frame goes
/// to the point under the pointer, snapped to the grid when snapping is on and kept inside the label so nothing is created off-paper.
/// Kept free of WPF so the rules are unit-tested like the rest of the editing geometry.
/// </summary>
public static class DropPlacement
{
    /// <summary>
    /// Moves <paramref name="element"/> so that its frame (its measured <paramref name="bounds"/>) starts at the drop point.
    /// </summary>
    /// <param name="element">The element to place; only its <see cref="LabelElement.X"/> and <see cref="LabelElement.Y"/> change.</param>
    /// <param name="bounds">The element's bounds as measured for its current position.</param>
    /// <param name="xDots">Drop point in label dots.</param>
    /// <param name="yDots">Drop point in label dots.</param>
    /// <param name="doc">The label being edited (size, resolution).</param>
    /// <param name="grid">Grid settings: the step and whether snapping is on.</param>
    public static void PlaceAt(LabelElement element, RectD bounds, double xDots, double yDots, LabelDocument doc, GridSettings grid)
    {
        double width = Math.Max(1, bounds.Width), height = Math.Max(1, bounds.Height);
        // An element larger than the label keeps its top-left corner at the label origin instead of hanging outside the paper.
        double maxX = Math.Max(0, doc.WidthDots - width), maxY = Math.Max(0, doc.HeightDots - height);
        double x = Math.Clamp(xDots, 0, maxX), y = Math.Clamp(yDots, 0, maxY);
        if (grid.SnapEnabled)
        {
            double step = grid.SpacingDots((int)doc.Dpi);
            // Snap, then clamp again: the snapped value may sit past the far edge of a label that is not a whole number of steps wide.
            x = Math.Clamp(Snapping.SnapToGrid(x, step), 0, maxX);
            y = Math.Clamp(Snapping.SnapToGrid(y, step), 0, maxY);
        }
        // The frame is not always anchored at the origin (^FT text and rotated fields extend left/up), so the delta is applied to X/Y.
        element.X = (int)Math.Round(element.X + (x - bounds.X));
        element.Y = (int)Math.Round(element.Y + (y - bounds.Y));
    }
}
