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

    [Fact]
    public void Write_FirstShapeLock_ShouldLockOnFirstNonEmptyRow()
    {
        var rows = new List<IDictionary<string, object?>>()
        {
            new Dictionary<string, object?>(),
            new Dictionary<string, object?> { ["id"] = 1, ["name"] = "a" },
            new Dictionary<string, object?> { ["id"] = 2, ["name"] = "b", ["ignored"] = 9 }
        };

        using var writer = new StringWriter();
        CsvWriter.Write(rows, writer, new CsvOptions
        {
            NewLineBehavior = CsvNewLineBehavior.Lf,
            HeterogeneousHeaderBehavior = CsvHeterogeneousHeaderBehavior.FirstShapeLock
        });

        var lines = writer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("id,name", lines[0]);
        Assert.Equal(",", lines[1]);
        Assert.Equal("1,a", lines[2]);
        Assert.Equal("2,b", lines[3]);
    }

    [Fact]
    public async Task WriteAsync_Union_WithNonReplayableSource_ShouldThrowByDefault()
    {
        var options = new CsvOptions
        {
            HeterogeneousHeaderBehavior = CsvHeterogeneousHeaderBehavior.Union,
            NewLineBehavior = CsvNewLineBehavior.Lf
        };

        using var writer = new StringWriter();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await CsvWriter.WriteAsync(CreateRows(), writer, options));
    }

    [Fact]
    public async Task WriteAsync_Union_WithReplayableSource_ShouldUseTwoPassSchemaScan()
    {
        var replayable = new ReplayableAsyncRows<IDictionary<string, object?>>(new[]
        {
            new Dictionary<string, object?> { ["a"] = 1 },
            new Dictionary<string, object?> { ["b"] = 2 }
        });

        var options = new CsvOptions
        {
            HeterogeneousHeaderBehavior = CsvHeterogeneousHeaderBehavior.Union,
            NewLineBehavior = CsvNewLineBehavior.Lf
        };

        using var writer = new StringWriter();
        await CsvWriter.WriteAsync(replayable, writer, options);

        var lines = writer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("a,b", lines[0]);
        Assert.Equal("1,", lines[1]);
        Assert.Equal(",2", lines[2]);
    }

    [Fact]
    public void Write_Union_100kRows_ShouldKeepAllocationsBounded()
    {
        var rows = CreateHeterogeneousRows(100_000);
        var options = new CsvOptions
        {
            NewLineBehavior = CsvNewLineBehavior.Lf,
            HeterogeneousHeaderBehavior = CsvHeterogeneousHeaderBehavior.Union
        };

        long allocated;
        using (var writer = new StringWriter())
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var before = GC.GetAllocatedBytesForCurrentThread();
            CsvWriter.Write(rows, writer, options);
            allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        }

        Assert.True(allocated < 130_000_000, $"Expected < 130MB allocations, got {allocated:N0} bytes.");
    }

    private static IEnumerable<IDictionary<string, object?>> CreateHeterogeneousRows(int rowCount)
    {
        for (var i = 0; i < rowCount; i++)
        {
            var row = new Dictionary<string, object?>
            {
                ["id"] = i,
                ["name"] = $"name-{i}"
            };

            if (i % 3 == 0)
            {
                row["region"] = $"r-{i % 11}";
            }

            if (i % 5 == 0)
            {
                row["score"] = i * 1.1;
            }

            if (i % 7 == 0)
            {
                row["active"] = true;
            }

            yield return row;
        }
    }

    private static async IAsyncEnumerable<IDictionary<string, object?>> CreateRows()
    {
        yield return new Dictionary<string, object?> { ["id"] = 1 };
        await Task.Yield();
        yield return new Dictionary<string, object?> { ["late"] = "x" };
    }

    private sealed class ReplayableAsyncRows<T>(IReadOnlyList<T> rows) : IAsyncEnumerable<T>, DynamicCsvSerializer.IReplayableAsyncEnumerable<T>
    {
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => Replay().GetAsyncEnumerator(cancellationToken);

        public IAsyncEnumerable<T> Replay() => Iterate();

        private async IAsyncEnumerable<T> Iterate()
        {
            foreach (var row in rows)
            {
                yield return row;
                await Task.Yield();
            }
        }
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
