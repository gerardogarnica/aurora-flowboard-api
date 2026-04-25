using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Aurora.Flowboard.Application.UnitTests.Abstractions;

internal sealed class TestAsyncQueryProvider<TEntity>(IQueryProvider inner) : IAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression) =>
        new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
        new TestAsyncEnumerable<TElement>(expression);

    public object? Execute(Expression expression) => inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        Type elementType = typeof(TResult).GenericTypeArguments[0];
        object? queryResult = inner.Execute(expression);

        return (TResult)typeof(Task)
            .GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(elementType)
            .Invoke(null, [queryResult])!;
    }
}

internal sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(Expression expression) : base(expression) { }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
}

internal sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;

    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());

    public ValueTask DisposeAsync()
    {
        inner.Dispose();
        return ValueTask.CompletedTask;
    }
}

internal static class MockDbSetHelper
{
    internal static DbSet<T> CreateMockDbSet<T>(IEnumerable<T> items) where T : class
    {
        var data = new TestAsyncEnumerable<T>(items);

        IQueryable<T> queryable = data;
        IQueryProvider provider = queryable.Provider;
        Expression expression = queryable.Expression;
        Type elementType = queryable.ElementType;
        IAsyncEnumerator<T> asyncEnumerator = data.GetAsyncEnumerator();

        var mockSet = Substitute.For<DbSet<T>, IQueryable<T>, IAsyncEnumerable<T>>();
        ((IQueryable<T>)mockSet).Provider.Returns(provider);
        ((IQueryable<T>)mockSet).Expression.Returns(expression);
        ((IQueryable<T>)mockSet).ElementType.Returns(elementType);
        ((IAsyncEnumerable<T>)mockSet)
            .GetAsyncEnumerator(Arg.Any<CancellationToken>())
            .Returns(asyncEnumerator);

        return mockSet;
    }
}
