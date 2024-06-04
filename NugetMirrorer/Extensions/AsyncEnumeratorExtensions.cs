using NuGet.Common;

namespace NugetMirrorer.Extensions;

public static class AsyncEnumeratorExtensions
{
    public static IEnumeratorAsync<T> GetAsyncEnumerator<T>(this IEnumerableAsync<T> enumerable) =>
        enumerable.GetEnumeratorAsync();
}