using System.Windows;
using Rotula.App.Editing;

namespace Rotula.App.Controls;

/// <summary>
/// The clipboard payload of a tool dragged from the toolbox to the canvas. Only the kind travels: the element itself is created on
/// drop, at the point where the pointer was released, so nothing is added when the drag is cancelled or dropped outside the label.
/// </summary>
public static class ToolboxDrag
{
    /// <summary>Format of the payload. The data is the <see cref="ToolboxKind"/> name.</summary>
    public const string Format = "Rotula.ToolboxKind";

    /// <summary>A payload for one tool.</summary>
    public static DataObject Create(ToolboxKind kind) => new(Format, kind.ToString());

    /// <summary>The tool carried by a drop, when the payload is ours.</summary>
    public static bool TryGet(IDataObject? data, out ToolboxKind kind)
    {
        kind = default;
        if (data is null || !data.GetDataPresent(Format)) return false;
        return data.GetData(Format) is string text && Enum.TryParse(text, out kind);
    }
}
