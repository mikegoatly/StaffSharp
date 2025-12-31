namespace StaffSharp.MusicXml.Tests;

using StaffSharp.MusicXml.Validation;
using System.Xml.Linq;
using Xunit;

public class MusicXmlSchemaValidatorTests
{
    [Fact]
    public void Validate_ValidDocument_PassesWithoutException()
    {
        // Arrange
        var testFilePath = Path.Combine("TestData", "single-note.xml");
        using var stream = File.OpenRead(testFilePath);
        var document = XDocument.Load(stream);

        // Act & Assert (should not throw)
        MusicXmlSchemaValidator.Validate(document);
    }

    [Fact]
    public async Task ValidateAsync_ValidDocument_PassesWithoutException()
    {
        // Arrange
        var testFilePath = Path.Combine("TestData", "c-major-scale.xml");
        using var stream = File.OpenRead(testFilePath);

        // Act & Assert (should not throw)
        await MusicXmlSchemaValidator.ValidateAsync(stream);
    }

    [Fact]
    public void Validate_InvalidDocument_ThrowsValidationException()
    {
        // Arrange
        var invalidXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE score-partwise PUBLIC ""-//Recordare//DTD MusicXML 4.0 Partwise//EN"" ""http://www.musicxml.org/dtds/partwise.dtd"">
<score-partwise version=""4.0"">
  <work>
    <work-title>Invalid Test</work-title>
  </work>
  <part-list>
    <score-part id=""P1"">
      <part-name>Test</part-name>
    </score-part>
  </part-list>
  <part id=""P1"">
    <measure number=""1"">
      <note>
        <pitch>
          <step>C</step>
          <octave>4</octave>
          <!-- Missing duration element - required! -->
        </pitch>
        <type>quarter</type>
      </note>
    </measure>
  </part>
</score-partwise>";

        var document = XDocument.Parse(invalidXml);

        // Act & Assert
        var exception = Assert.Throws<MusicXmlValidationException>(() => MusicXmlSchemaValidator.Validate(document));
        Assert.NotEmpty(exception.Errors);
        Assert.Contains("duration", exception.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_MissingRequiredElement_ReportsError()
    {
        // Arrange
        var invalidXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE score-partwise PUBLIC ""-//Recordare//DTD MusicXML 4.0 Partwise//EN"" ""http://www.musicxml.org/dtds/partwise.dtd"">
<score-partwise version=""4.0"">
  <work>
    <work-title>Missing Part List</work-title>
  </work>
  <!-- Missing part-list element - required! -->
  <part id=""P1"">
    <measure number=""1"">
    </measure>
  </part>
</score-partwise>";

        var document = XDocument.Parse(invalidXml);

        // Act & Assert
        var exception = Assert.Throws<MusicXmlValidationException>(() => MusicXmlSchemaValidator.Validate(document));
        Assert.NotEmpty(exception.Errors);
    }

    [Fact]
    public void Validate_AllTestFiles_AreValid()
    {
        // Arrange
        var testDataDir = Path.Combine("TestData");
        var xmlFiles = Directory.GetFiles(testDataDir, "*.xml");

        Assert.NotEmpty(xmlFiles); // Ensure we have test files

        // Act & Assert - all our test files should be valid MusicXML
        foreach (var xmlFile in xmlFiles)
        {
            using var stream = File.OpenRead(xmlFile);
            var document = XDocument.Load(stream);

            try
            {
                // Should not throw
                MusicXmlSchemaValidator.Validate(document);
            }
            catch (MusicXmlValidationException ex)
            {
                // Provide better error message showing which file failed
                var errorDetails = string.Join(Environment.NewLine, ex.Errors);
                throw new MusicXmlValidationException(
                    $"Validation failed for file: {Path.GetFileName(xmlFile)}{Environment.NewLine}{errorDetails}",
                    ex.Errors);
            }
        }
    }

    [Fact]
    public void MusicXmlValidationException_ToString_IncludesAllErrors()
    {
        // Arrange
        var errors = new List<string>
        {
            "Error 1: Missing required element",
            "Error 2: Invalid attribute value"
        };
        var exception = new MusicXmlValidationException("Validation failed", errors);

        // Act
        var result = exception.ToString();

        // Assert
        Assert.Contains("Validation failed", result);
        Assert.Contains("Error 1: Missing required element", result);
        Assert.Contains("Error 2: Invalid attribute value", result);
    }
}
