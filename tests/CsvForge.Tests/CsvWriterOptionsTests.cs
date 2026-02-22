using CsvForge;

namespace CsvForge.Tests;

public class CsvWriterOptionsTests
{
    [Fact]
    public void Defaults_ShouldUseCommaDelimiterAndHeader()
    {
        var options = new CsvWriterOptions();

        Assert.Equal(',', options.Delimiter);
        Assert.True(options.IncludeHeader);
        Assert.Equal("\n", options.NewLine);
    }
}
