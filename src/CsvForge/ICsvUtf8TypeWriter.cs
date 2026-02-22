using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace CsvForge;

public interface ICsvUtf8TypeWriter<T>
{
    void WriteHeader(IBufferWriter<byte> writer, CsvOptions options);

    void WriteRow(IBufferWriter<byte> writer, T value, CsvOptions options);

    ValueTask WriteHeaderAsync(IBufferWriter<byte> writer, CsvOptions options, CancellationToken cancellationToken);

    ValueTask WriteRowAsync(IBufferWriter<byte> writer, T value, CsvOptions options, CancellationToken cancellationToken);
}
