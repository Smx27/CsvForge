using CsvForge;

namespace CsvForge.Tests;

public class CsvWriterCheckpointTests
{
    [Fact]
    public async Task WriteWithCheckpointAsync_FreshExport_WritesAllRowsAndCheckpoint()
    {
        using var scope = new TempScope();
        var dataPath = scope.Path("fresh.csv");
        var checkpointPath = scope.Path("fresh.chk");

        await CsvWriter.WriteWithCheckpointAsync(CreateRows(5), dataPath, new CsvCheckpointOptions
        {
            BatchSize = 2,
            CheckpointFilePath = checkpointPath,
            FlushInterval = TimeSpan.Zero,
            CsvOptions = new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf, EnableRuntimeMetadataFallback = true }
        });

        var lines = File.ReadAllLines(dataPath);
        Assert.Equal("Id,Name", lines[0]);
        Assert.Equal(6, lines.Length);
        Assert.Equal("4", await File.ReadAllTextAsync(checkpointPath));
    }

    [Fact]
    public async Task WriteWithCheckpointAsync_ResumeExport_SkipsCommittedRows()
    {
        using var scope = new TempScope();
        var dataPath = scope.Path("resume.csv");
        var checkpointPath = scope.Path("resume.chk");

        await File.WriteAllTextAsync(dataPath, "Id,Name\n0,row-0\n1,row-1\n");
        await File.WriteAllTextAsync(checkpointPath, "1");

        await CsvWriter.WriteWithCheckpointAsync(CreateRows(5), dataPath, new CsvCheckpointOptions
        {
            BatchSize = 2,
            CheckpointFilePath = checkpointPath,
            ResumeIfExists = true,
            CsvOptions = new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf, EnableRuntimeMetadataFallback = true }
        });

        var lines = File.ReadAllLines(dataPath);
        Assert.Equal(6, lines.Length);
        Assert.Equal("4,row-4", lines[^1]);
        Assert.Equal("4", await File.ReadAllTextAsync(checkpointPath));
    }

    [Fact]
    public async Task WriteWithCheckpointAsync_CrashBetweenBatches_ResumesWithoutDuplicates()
    {
        using var scope = new TempScope();
        var dataPath = scope.Path("crash.csv");
        var checkpointPath = scope.Path("crash.chk");
        var options = new CsvCheckpointOptions
        {
            BatchSize = 2,
            CheckpointFilePath = checkpointPath,
            FlushInterval = TimeSpan.Zero,
            CsvOptions = new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf, EnableRuntimeMetadataFallback = true }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await CsvWriter.WriteWithCheckpointAsync(CreateRows(6, throwAfterYielded: 4), dataPath, options));

        Assert.Equal("3", await File.ReadAllTextAsync(checkpointPath));

        await CsvWriter.WriteWithCheckpointAsync(CreateRows(6), dataPath, options);

        var lines = File.ReadAllLines(dataPath);
        Assert.Equal(7, lines.Length);
        Assert.Equal(6, lines.Skip(1).Distinct().Count());
    }

    [Fact]
    public async Task WriteWithCheckpointAsync_AtomicCheckpointUpdate_LeavesNoTempFile()
    {
        using var scope = new TempScope();
        var dataPath = scope.Path("atomic.csv");
        var checkpointPath = scope.Path("atomic.chk");

        await CsvWriter.WriteWithCheckpointAsync(CreateRows(3), dataPath, new CsvCheckpointOptions
        {
            BatchSize = 1,
            CheckpointFilePath = checkpointPath,
            TempFileStrategy = CsvCheckpointTempFileStrategy.Replace,
            CsvOptions = new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf, EnableRuntimeMetadataFallback = true }
        });

        Assert.False(File.Exists(checkpointPath + ".tmp"));
        Assert.Equal("2", await File.ReadAllTextAsync(checkpointPath));
    }

    [Fact]
    public async Task WriteWithCheckpointAsync_IdempotentRerun_DoesNotAppendDuplicates()
    {
        using var scope = new TempScope();
        var dataPath = scope.Path("idempotent.csv");
        var checkpointPath = scope.Path("idempotent.chk");
        var options = new CsvCheckpointOptions
        {
            BatchSize = 2,
            CheckpointFilePath = checkpointPath,
            ResumeIfExists = true,
            CsvOptions = new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf, EnableRuntimeMetadataFallback = true }
        };

        await CsvWriter.WriteWithCheckpointAsync(CreateRows(4), dataPath, options);
        var firstRun = await File.ReadAllTextAsync(dataPath);

        await CsvWriter.WriteWithCheckpointAsync(CreateRows(4), dataPath, options);
        var secondRun = await File.ReadAllTextAsync(dataPath);

        Assert.Equal(firstRun, secondRun);
    }

    private static async IAsyncEnumerable<TestRow> CreateRows(int count, int? throwAfterYielded = null)
    {
        for (var i = 0; i < count; i++)
        {
            if (throwAfterYielded.HasValue && i == throwAfterYielded.Value)
            {
                throw new InvalidOperationException("Simulated crash");
            }

            yield return new TestRow { Id = i, Name = $"row-{i}" };
            await Task.Yield();
        }
    }

    private sealed class TestRow
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;
    }

    private sealed class TempScope : IDisposable
    {
        private readonly string _root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"csvforge-checkpoint-{Guid.NewGuid():N}");

        public TempScope() => Directory.CreateDirectory(_root);

        public string Path(string fileName) => System.IO.Path.Combine(_root, fileName);

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
    }
}
