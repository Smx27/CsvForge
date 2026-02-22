using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CsvForge.Checkpoint;

internal sealed class CsvCheckpointCoordinator
{
    private readonly string _checkpointPath;
    private readonly CsvCheckpointTempFileStrategy _tempFileStrategy;

    public CsvCheckpointCoordinator(string checkpointPath, CsvCheckpointTempFileStrategy tempFileStrategy)
    {
        _checkpointPath = checkpointPath;
        _tempFileStrategy = tempFileStrategy;
    }

    public async Task<long> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_checkpointPath))
        {
            return -1;
        }

        var content = await File.ReadAllTextAsync(_checkpointPath, cancellationToken).ConfigureAwait(false);
        return long.TryParse(content.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var checkpoint)
            ? checkpoint
            : -1;
    }

    public async Task PersistAsync(long rowIndex, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_checkpointPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = _checkpointPath + ".tmp";
        var contents = rowIndex.ToString(CultureInfo.InvariantCulture);
        await File.WriteAllTextAsync(tempPath, contents, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

        if (_tempFileStrategy == CsvCheckpointTempFileStrategy.Replace && File.Exists(_checkpointPath))
        {
            File.Replace(tempPath, _checkpointPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            return;
        }

        if (File.Exists(_checkpointPath))
        {
            File.Delete(_checkpointPath);
        }

        File.Move(tempPath, _checkpointPath);
    }
}
