namespace StaffSharp.MusicXml.Validation;

/// <summary>
/// Exception thrown when MusicXML document validation fails.
/// </summary>
public sealed class MusicXmlValidationException : Exception
{
    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    public MusicXmlValidationException()
        : base("MusicXML document validation failed.")
    {
        Errors = [];
    }

    public MusicXmlValidationException(string message)
        : base(message)
    {
        Errors = [];
    }

    public MusicXmlValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors = [];
    }

    public MusicXmlValidationException(string message, IReadOnlyList<string> errors)
        : base(message)
    {
        Errors = errors ?? [];
    }

    public override string ToString()
    {
        var errorList = string.Join(Environment.NewLine, Errors.Select((e, i) => $"  {i + 1}. {e}"));
        return $"{Message}{Environment.NewLine}{errorList}{Environment.NewLine}{base.ToString()}";
    }
}
