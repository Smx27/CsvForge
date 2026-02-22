using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CsvForge;

public static class CsvWriter
{
    public static void Write<T>(IEnumerable<T> data, string path, CsvOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        options ??= CsvOptions.Default;

        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, options.BufferSize, useAsync: false);
        using var writer = new StreamWriter(stream, options.Encoding, options.StreamWriterBufferSize, leaveOpen: false);
        CsvSerializer.Write(data, writer, options);
        writer.Flush();
    }

    public static void Write<T>(IEnumerable<T> data, Stream stream, CsvOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        options ??= CsvOptions.Default;

        using var writer = new StreamWriter(stream, options.Encoding, options.StreamWriterBufferSize, leaveOpen: true);
        CsvSerializer.Write(data, writer, options);
        writer.Flush();
    }

    public static void Write<T>(IEnumerable<T> data, TextWriter writer, CsvOptions? options = null)
    {
        options ??= CsvOptions.Default;
        CsvSerializer.Write(data, writer, options);
        writer.Flush();
    }

    public static Task WriteAsync<T>(IEnumerable<T> data, string path, CsvOptions? options = null, CancellationToken cancellationToken = default)
    {
        return WriteEnumerableToPathAsync(data, path, options, cancellationToken);
    }

    public static Task WriteAsync<T>(IEnumerable<T> data, Stream stream, CsvOptions? options = null, CancellationToken cancellationToken = default)
    {
        return WriteEnumerableToStreamAsync(data, stream, options, cancellationToken);
    }

    public static async Task WriteAsync<T>(IEnumerable<T> data, TextWriter writer, CsvOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= CsvOptions.Default;
        await CsvSerializer.WriteAsync(data, writer, options, cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    public static Task WriteAsync<T>(IAsyncEnumerable<T> data, string path, CsvOptions? options = null, CancellationToken cancellationToken = default)
    {
        return WriteAsyncEnumerableToPathAsync(data, path, options, cancellationToken);
    }

    public static Task WriteAsync<T>(IAsyncEnumerable<T> data, Stream stream, CsvOptions? options = null, CancellationToken cancellationToken = default)
    {
        return WriteAsyncEnumerableToStreamAsync(data, stream, options, cancellationToken);
    }

    public static async Task WriteAsync<T>(IAsyncEnumerable<T> data, TextWriter writer, CsvOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= CsvOptions.Default;
        await CsvSerializer.WriteAsync(data, writer, options, cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    private static async Task WriteEnumerableToPathAsync<T>(IEnumerable<T> data, string path, CsvOptions? options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(path);
        options ??= CsvOptions.Default;

        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, options.BufferSize, useAsync: true);
        await using var writer = new StreamWriter(stream, options.Encoding, options.StreamWriterBufferSize, leaveOpen: false);
        await CsvSerializer.WriteAsync(data, writer, options, cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    private static async Task WriteEnumerableToStreamAsync<T>(IEnumerable<T> data, Stream stream, CsvOptions? options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        options ??= CsvOptions.Default;

        await using var writer = new StreamWriter(stream, options.Encoding, options.StreamWriterBufferSize, leaveOpen: true);
        await CsvSerializer.WriteAsync(data, writer, options, cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    private static async Task WriteAsyncEnumerableToPathAsync<T>(IAsyncEnumerable<T> data, string path, CsvOptions? options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(path);
        options ??= CsvOptions.Default;

        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, options.BufferSize, useAsync: true);
        await using var writer = new StreamWriter(stream, options.Encoding, options.StreamWriterBufferSize, leaveOpen: false);
        await CsvSerializer.WriteAsync(data, writer, options, cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    private static async Task WriteAsyncEnumerableToStreamAsync<T>(IAsyncEnumerable<T> data, Stream stream, CsvOptions? options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        options ??= CsvOptions.Default;

        await using var writer = new StreamWriter(stream, options.Encoding, options.StreamWriterBufferSize, leaveOpen: true);
        await CsvSerializer.WriteAsync(data, writer, options, cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }
}
