namespace StaffSharp.Notation;

/// <summary>
/// Represents text annotation on a note or event.
/// ABC notation: "^above"C, "_below"D, "<left"E, ">right"F
/// </summary>
/// <param name="Text">
/// The annotation text.
/// </param>
/// <paramref name="Placement">
/// Where the annotation should be placed relative to the note.
/// </paramref>
public readonly record struct Annotation(string Text, AnnotationPlacement Placement = AnnotationPlacement.Above);

/// <summary>
/// Placement of annotation relative to note.
/// </summary>
public enum AnnotationPlacement
{
    Above,   // ^ in ABC
    Below,   // _ in ABC
    Left,    // < in ABC
    Right    // > in ABC (note: different from broken rhythm >)
}
