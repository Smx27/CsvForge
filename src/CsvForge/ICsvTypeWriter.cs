using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CsvForge;

/// <summary>
/// Defines a contract for writing CSV data for a specific type using a <see cref="TextWriter"/>.
/// </summary>
/// <typeparam name="T">The type of the data row to write.</typeparam>
public interface ICsvTypeWriter<T>
{
    /// <summary>
    /// Writes the CSV header row to the specified writer.
    /// </summary>
    /// <param name="writer">The output text writer.</param>
    /// <param name="options">The formatting options.</param>
    void WriteHeader(TextWriter writer, CsvOptions options);

    /// <summary>
    /// Writes a single CSV data row to the specified writer.
    /// </summary>
    /// <param name="writer">The output text writer.</param>
    /// <param name="value">The data item to write.</param>
    /// <param name="options">The formatting options.</param>
    void WriteRow(TextWriter writer, T value, CsvOptions options);

    /// <summary>
    /// Asynchronously writes the CSV header row to the specified writer.
    /// </summary>
    /// <param name="writer">The output text writer.</param>
    /// <param name="options">The formatting options.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the write operation.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    ValueTask WriteHeaderAsync(TextWriter writer, CsvOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously writes a single CSV data row to the specified writer.
    /// </summary>
    /// <param name="writer">The output text writer.</param>
    /// <param name="value">The data item to write.</param>
    /// <param name="options">The formatting options.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the write operation.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    ValueTask WriteRowAsync(TextWriter writer, T value, CsvOptions options, CancellationToken cancellationToken);
}
