# Checkpointing

## Who this is for
This guide is for teams running long CSV jobs that must resume after interruption, redeploy, or transient infrastructure failures.

## Add resumable exports

```csharp
// samples/CsvForge.Samples.Checkpointing/Program.cs
using CsvForge;

var checkpointOptions = new CsvCheckpointOptions
{
    CheckpointFilePath = "checkpoints/orders-export.json",
    FlushIntervalRows = 10_000
};

await using var stream = File.Open("orders.csv", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
await CsvSerializer.SerializeWithCheckpointAsync(rows, stream, checkpointOptions);
```

## Coordinate retries safely

```csharp
// src/CsvForge/Checkpoint/CsvCheckpointCoordinator.cs patterns
using CsvForge.Checkpoint;

var coordinator = new CsvCheckpointCoordinator(checkpointOptions);
var state = await coordinator.LoadAsync();
// Resume from state.LastCommittedRow, then commit after each durable flush.
```

## Enterprise guidance
- Large exports: checkpoint by stable row index or deterministic key boundaries.
- Reliability: write checkpoint metadata atomically; avoid in-place partial JSON writes.
- Observability: record checkpoint lag (rows since last commit) and restart counts.
- Deployment constraints: persist checkpoint files on durable storage (volume/object storage), not ephemeral temp disks.

## Troubleshooting
### Restart loops
- Verify checkpoint path permissions and ensure process identity can read/write state.

### Partial output files
- Pair checkpointing with append-safe stream handling and explicit fsync/flush boundaries.

### Memory pressure
- Keep checkpoint payload minimal (offsets/counters), not full row payload snapshots.

## See also
- [Advanced Usage](./advanced-usage.md)
- [Compression](./compression.md)
- [Developer Guide](./developer-guide.md)
- [FAQ](./faq.md)
