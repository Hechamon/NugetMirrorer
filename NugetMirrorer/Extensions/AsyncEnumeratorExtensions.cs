using NuGet.Common;

namespace NugetMirrorer.Extensions;

public static class AsyncEnumeratorExtensions
{
    public static IEnumeratorAsync<T> GetAsyncEnumerator<T>(this IEnumerableAsync<T> enumerable) =>
        enumerable.GetEnumeratorAsync();

    public static IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IEnumerableAsync<T> enumerable) =>
        AsyncEnumerable.Create<T>(_ => new NugetAsyncEnumerator<T>(enumerable.GetEnumeratorAsync()));
}

internal class NugetAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumeratorAsync<T> _enumerator;

    public NugetAsyncEnumerator(IEnumeratorAsync<T> enumerator)
    {
        _enumerator = enumerator;
    }

    public T Current => _enumerator.Current;

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask<bool> MoveNextAsync()
    {
        return await _enumerator.MoveNextAsync();
    }
}