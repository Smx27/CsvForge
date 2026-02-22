using System;
using System.Buffers;
using System.Text;

namespace CsvForge;

internal sealed class CsvSerializationContext : IDisposable
{
    private const int InitialFormatBufferSize = 128;
    private static readonly ObjectPool<StringBuilder> StringBuilderPool = new(static () => new StringBuilder(256), static builder =>
    {
        builder.Clear();
        if (builder.Capacity > 4096)
        {
            builder.Capacity = 4096;
        }
    });

    private char[] _formatBuffer;

    public CsvSerializationContext(CsvOptions options)
    {
        Options = options;
        _formatBuffer = ArrayPool<char>.Shared.Rent(InitialFormatBufferSize);
    }

    public CsvOptions Options { get; }

    public char[] FormatBuffer
    {
        get => _formatBuffer;
        set => _formatBuffer = value;
    }

    public PooledObject<StringBuilder> RentStringBuilder() => StringBuilderPool.Rent();

    public void Dispose()
    {
        ArrayPool<char>.Shared.Return(_formatBuffer);
    }
}

internal sealed class ObjectPool<T> where T : class
{
    private readonly Func<T> _factory;
    private readonly Action<T> _reset;
    private readonly System.Collections.Concurrent.ConcurrentBag<T> _pool = new();

    public ObjectPool(Func<T> factory, Action<T> reset)
    {
        _factory = factory;
        _reset = reset;
    }

    public PooledObject<T> Rent()
    {
        if (!_pool.TryTake(out var value))
        {
            value = _factory();
        }

        return new PooledObject<T>(value, Return);
    }

    private void Return(T value)
    {
        _reset(value);
        _pool.Add(value);
    }
}

internal readonly struct PooledObject<T> : IDisposable where T : class
{
    private readonly Action<T> _return;

    public PooledObject(T value, Action<T> @return)
    {
        Value = value;
        _return = @return;
    }

    public T Value { get; }

    public void Dispose()
    {
        _return(Value);
    }
}
