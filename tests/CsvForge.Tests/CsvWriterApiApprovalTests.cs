using System.Reflection;
using CsvForge;

namespace CsvForge.Tests;

public class CsvWriterApiApprovalTests
{
    [Fact]
    public void CsvWriter_PublicApi_ShouldMatchApprovedSnapshot()
    {
        var current = typeof(CsvWriter)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(static method => method.DeclaringType == typeof(CsvWriter))
            .Select(static method => method.ToString())
            .OrderBy(static signature => signature, StringComparer.Ordinal)
            .ToArray();

        var snapshotPath = Path.Combine(AppContext.BaseDirectory, "ApiApproval", "CsvWriterApi.approved.txt");
        var approved = File.ReadAllLines(snapshotPath)
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .OrderBy(static signature => signature, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(approved, current);
    }
}
