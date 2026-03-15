using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace CsvForge;

/// <summary>
/// Defines a contract for writing UTF-8 encoded CSV data for a specific type using an <see cref="IBufferWriter{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the data row to write.</typeparam>
public interface ICsvUtf8TypeWriter<T>
{
    /// <summary>
    /// Writes the CSV header row to the specified buffer writer.
    /// </summary>
    /// <param name="writer">The output buffer writer.</param>
    /// <param name="options">The formatting options.</param>
    void WriteHeader(IBufferWriter<byte> writer, CsvOptions options);

    /// <summary>
    /// Writes a single CSV data row to the specified buffer writer.
    /// </summary>
    /// <param name="writer">The output buffer writer.</param>
    /// <param name="value">The data item to write.</param>
    /// <param name="options">The formatting options.</param>
    void WriteRow(IBufferWriter<byte> writer, T value, CsvOptions options);

    /// <summary>
    /// Asynchronously writes the CSV header row to the specified buffer writer.
    /// </summary>
    /// <param name="writer">The output buffer writer.</param>
    /// <param name="options">The formatting options.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the write operation.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    ValueTask WriteHeaderAsync(IBufferWriter<byte> writer, CsvOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously writes a single CSV data row to the specified buffer writer.
    /// </summary>
    /// <param name="writer">The output buffer writer.</param>
    /// <param name="value">The data item to write.</param>
    /// <param name="options">The formatting options.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the write operation.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    ValueTask WriteRowAsync(IBufferWriter<byte> writer, T value, CsvOptions options, CancellationToken cancellationToken);
}
