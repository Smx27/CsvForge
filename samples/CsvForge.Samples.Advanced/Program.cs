using CsvForge;
using CsvForge.Attributes;
using System.Runtime.CompilerServices;

var outputPath = Path.Combine(AppContext.BaseDirectory, "advanced-export.csv");
var options = new CsvOptions
{
    Delimiter = ';',
    IncludeHeader = true,
    EnableRuntimeMetadataFallback = false
};

await CsvWriter.WriteToFileAsync(StreamOrdersAsync(), outputPath, options);

Console.WriteLine($"Advanced sample written to {outputPath}");
Console.WriteLine(await File.ReadAllTextAsync(outputPath));

static async IAsyncEnumerable<AdvancedOrderRow> StreamOrdersAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    var orders = new[]
    {
        new AdvancedOrderRow { Id = 1001, CustomerName = "Ada", Total = 42.50m, PlacedUtc = new DateTime(2025, 01, 18, 09, 20, 00, DateTimeKind.Utc) },
        new AdvancedOrderRow { Id = 1002, CustomerName = "Grace", Total = 99.95m, PlacedUtc = new DateTime(2025, 01, 18, 09, 25, 00, DateTimeKind.Utc) },
        new AdvancedOrderRow { Id = 1003, CustomerName = "Linus", Total = 15.00m, PlacedUtc = new DateTime(2025, 01, 18, 09, 26, 00, DateTimeKind.Utc) }
    };

    foreach (var order in orders)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(30, cancellationToken);
        yield return order;
    }
}

[CsvSerializable]
public partial class AdvancedOrderRow
{
    [CsvColumn("order_id", Order = 0)]
    public int Id { get; init; }

    [CsvColumn("customer", Order = 1)]
    public string CustomerName { get; init; } = string.Empty;

    [CsvColumn("total_amount", Order = 2)]
    public decimal Total { get; init; }

    [CsvColumn("placed_utc", Order = 3)]
    public DateTime PlacedUtc { get; init; }
}
