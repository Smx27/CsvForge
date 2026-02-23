using System.Runtime.CompilerServices;

namespace CsvForge.Samples.Shared;

public sealed class SampleDataGenerator
{
    private readonly int _seed;

    public SampleDataGenerator(int seed = 12345)
    {
        _seed = seed;
    }

    public IEnumerable<GeneratedSampleRow> GenerateGeneratedRows(int count)
    {
        var random = new Random(_seed);
        for (var index = 0; index < count; index++)
        {
            yield return CreateGeneratedRow(index, random);
        }
    }

    public IEnumerable<FallbackSampleRow> GenerateFallbackRows(int count)
    {
        var random = new Random(_seed);
        for (var index = 0; index < count; index++)
        {
            yield return CreateFallbackRow(index, random);
        }
    }

    public async IAsyncEnumerable<GeneratedSampleRow> GenerateGeneratedRowsAsync(
        int count,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var random = new Random(_seed);
        for (var index = 0; index < count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (index > 0 && index % 10_000 == 0)
            {
                await Task.Yield();
            }

            yield return CreateGeneratedRow(index, random);
        }
    }

    public async IAsyncEnumerable<FallbackSampleRow> GenerateFallbackRowsAsync(
        int count,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var random = new Random(_seed);
        for (var index = 0; index < count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (index > 0 && index % 10_000 == 0)
            {
                await Task.Yield();
            }

            yield return CreateFallbackRow(index, random);
        }
    }

    public IEnumerable<GeneratedSampleRow> GenerateLargeDataset(int count = 100_000)
    {
        if (count is < 100_000 or > 1_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "count must be between 100,000 and 1,000,000.");
        }

        var random = new Random(_seed);
        for (var index = 0; index < count; index++)
        {
            yield return CreateGeneratedRow(index, random);
        }
    }

    private static GeneratedSampleRow CreateGeneratedRow(int index, Random random)
    {
        return new GeneratedSampleRow
        {
            Id = index + 1,
            IsActive = random.Next(0, 2) == 1,
            Name = $"Generated-{index + 1:D7}",
            Score = index % 5 == 0 ? null : random.Next(0, 101),
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(index),
            LastSeenAt = index % 3 == 0 ? null : new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(-random.Next(0, 120)),
            Balance = Math.Round((decimal)random.NextDouble() * 10000m, 2),
            CreditLimit = index % 4 == 0 ? null : Math.Round((decimal)random.NextDouble() * 20000m, 2),
            Status = (SampleStatus)random.Next(0, 4),
            InternalNote = $"Internal-{index + 1}",
            IgnoredField = random.Next()
        };
    }

    private static FallbackSampleRow CreateFallbackRow(int index, Random random)
    {
        return new FallbackSampleRow
        {
            Id = index + 1,
            IsActive = random.Next(0, 2) == 1,
            Name = $"Fallback-{index + 1:D7}",
            Score = index % 6 == 0 ? null : random.Next(0, 101),
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(index),
            LastSeenAt = index % 2 == 0 ? null : new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(-random.Next(0, 120)),
            Balance = Math.Round((decimal)random.NextDouble() * 12000m, 2),
            CreditLimit = index % 5 == 0 ? null : Math.Round((decimal)random.NextDouble() * 22000m, 2),
            Status = (SampleStatus)random.Next(0, 4),
            InternalNote = $"Internal-{index + 1}",
            IgnoredField = random.Next()
        };
    }
}
