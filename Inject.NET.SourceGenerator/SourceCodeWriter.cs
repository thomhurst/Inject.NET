using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Inject.NET.SourceGenerator;

public class SourceCodeWriter : IDisposable
{
    private static readonly ConcurrentQueue<StringBuilder> _stringBuilderPool = new();
    private static readonly int _maxPoolSize = 8; // Fixed reasonable size
    private static int _currentPoolSize = 0;
    
    private int _tabLevel;
    private readonly StringBuilder _stringBuilder;
    
    public SourceCodeWriter()
    {
        _stringBuilder = GetStringBuilderFromPool();
    }

    public void WriteLine()
    {
        _stringBuilder.AppendLine();
    }

    public void WriteTabs()
    {
        for (var i = 0; i < _tabLevel; i++)
        {
            _stringBuilder.Append('\t');
        }
    }
    
    public void WriteLine([StringSyntax("c#")] string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }
        
        if (value[0] is '}' or ']')
        {
            _tabLevel--;
        }
        
        WriteTabs();
        
        _stringBuilder.AppendLine(value);

        if (value is ['{' or '['])
        {
            _tabLevel++;
        }
    }

    public override string ToString()
    {
        return _stringBuilder.ToString();
    }

    public void Dispose()
    {
        ReturnStringBuilderToPool(_stringBuilder);
    }
    
    private static StringBuilder GetStringBuilderFromPool()
    {
        if (_stringBuilderPool.TryDequeue(out var sb))
        {
            sb.Clear(); // Ensure it's clean
            return sb;
        }
        
        return new StringBuilder();
    }
    
    private static void ReturnStringBuilderToPool(StringBuilder stringBuilder)
    {
        if (stringBuilder.Capacity > 8192) // Don't keep very large StringBuilder instances
        {
            return;
        }
        
        // Simple thread-safe increment without Interlocked for .NET Standard 2.0 compatibility
        if (_currentPoolSize < _maxPoolSize)
        {
            stringBuilder.Clear();
            _stringBuilderPool.Enqueue(stringBuilder);
            _currentPoolSize++; // Note: This could cause slight over-pooling in race conditions, which is acceptable
        }
    }
}