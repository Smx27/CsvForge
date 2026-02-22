using System.Dynamic;
using CsvForge;

namespace CsvForge.Tests;

public class CsvDynamicRowsTests
{
    [Fact]
    public void Write_ObjectRows_ShouldUseRuntimeMetadataAndUnionHeaders()
    {
        object[] rows =
        [
            new AlphaRow { A = 1 },
            new BetaRow { B = 2 }
        ];

        using var writer = new StringWriter();
        CsvWriter.Write(rows, writer, new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf });

        var lines = writer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("A,B", lines[0]);
        Assert.Equal("1,", lines[1]);
        Assert.Equal(",2", lines[2]);
    }

    [Fact]
    public void Write_DictionaryRows_ShouldUseStableUnionHeaderOrder()
    {
        var rows = new List<IDictionary<string, object?>>()
        {
            new Dictionary<string, object?> { ["b"] = 2, ["a"] = 1 },
            new Dictionary<string, object?> { ["c"] = 3 }
        };

        using var writer = new StringWriter();
        CsvWriter.Write(rows, writer, new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf });

        var lines = writer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("b,a,c", lines[0]);
        Assert.Equal("2,1,", lines[1]);
        Assert.Equal(",,3", lines[2]);
    }

    [Fact]
    public void Write_ExpandoRows_WithFirstShapeLock_ShouldIgnoreLateColumns()
    {
        dynamic first = new ExpandoObject();
        first.id = 10;

        dynamic second = new ExpandoObject();
        second.id = 11;
        second.late = "x";

        var rows = new List<ExpandoObject> { first, second };

        using var writer = new StringWriter();
        CsvWriter.Write(rows, writer, new CsvOptions
        {
            NewLineBehavior = CsvNewLineBehavior.Lf,
            HeterogeneousHeaderBehavior = CsvHeterogeneousHeaderBehavior.FirstShapeLock
        });

        var lines = writer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("id", lines[0]);
        Assert.Equal("10", lines[1]);
        Assert.Equal("11", lines[2]);
    }

    private sealed class AlphaRow
    {
        public int A { get; set; }
    }

    private sealed class BetaRow
    {
        public int B { get; set; }
    }
}
