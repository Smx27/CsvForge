using CsvForge.Attributes;

namespace CsvForge.Samples.Shared;

public enum SampleStatus
{
    New,
    Active,
    Suspended,
    Deleted
}

[CsvSerializable]
public partial class GeneratedSampleRow
{
    [CsvColumn("row_id", Order = 0)]
    public int Id { get; init; }

    [CsvColumn("is_active", Order = 1)]
    public bool IsActive { get; init; }

    [CsvColumn("name", Order = 2)]
    public string Name { get; init; } = string.Empty;

    [CsvColumn("score", Order = 3)]
    public int? Score { get; init; }

    [CsvColumn("created_at", Order = 4)]
    public DateTime CreatedAt { get; init; }

    [CsvColumn("last_seen_at", Order = 5)]
    public DateTime? LastSeenAt { get; init; }

    [CsvColumn("balance", Order = 6)]
    public decimal Balance { get; init; }

    [CsvColumn("credit_limit", Order = 7)]
    public decimal? CreditLimit { get; init; }

    [CsvColumn("status", Order = 8)]
    public SampleStatus Status { get; init; }

    [CsvIgnore]
    public string InternalNote { get; init; } = string.Empty;

    [CsvIgnore]
    public int IgnoredField;
}

public sealed class FallbackSampleRow
{
    [CsvColumn("row_id", Order = 0)]
    public int Id { get; init; }

    [CsvColumn("is_active", Order = 1)]
    public bool IsActive { get; init; }

    [CsvColumn("name", Order = 2)]
    public string Name { get; init; } = string.Empty;

    [CsvColumn("score", Order = 3)]
    public int? Score { get; init; }

    [CsvColumn("created_at", Order = 4)]
    public DateTime CreatedAt { get; init; }

    [CsvColumn("last_seen_at", Order = 5)]
    public DateTime? LastSeenAt { get; init; }

    [CsvColumn("balance", Order = 6)]
    public decimal Balance { get; init; }

    [CsvColumn("credit_limit", Order = 7)]
    public decimal? CreditLimit { get; init; }

    [CsvColumn("status", Order = 8)]
    public SampleStatus Status { get; init; }

    [CsvIgnore]
    public string InternalNote { get; init; } = string.Empty;

    [CsvIgnore]
    public int IgnoredField;
}
