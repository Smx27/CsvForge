using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CsvForge;

public interface ICsvTypeWriter<T>
{
    void WriteHeader(TextWriter writer, CsvOptions options);

    void WriteRow(TextWriter writer, T value, CsvOptions options);

    ValueTask WriteHeaderAsync(TextWriter writer, CsvOptions options, CancellationToken cancellationToken);

    ValueTask WriteRowAsync(TextWriter writer, T value, CsvOptions options, CancellationToken cancellationToken);
}
