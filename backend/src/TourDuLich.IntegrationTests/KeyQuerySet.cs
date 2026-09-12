using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
#pragma warning disable EF1001 // Unit double inspects the installed EF raw-query root; no production dependency.

namespace TourDuLich.IntegrationTests;

// Unit-test double for key-based FromSql reads. Does NOT execute SQL or simulate locks;
// SQL translation and real concurrency must be verified separately.
internal sealed class KeyQuerySet<T>(DbContext context, IEnumerable<T> rows) : DbSet<T>, IQueryable<T> where T : class
{
    public override Microsoft.EntityFrameworkCore.Metadata.IEntityType EntityType => context.Model.FindEntityType(typeof(T))!;
    Type IQueryable.ElementType => typeof(T);
    Expression IQueryable.Expression => new EntityQueryRootExpression(EntityType);
    IQueryProvider IQueryable.Provider => new Provider<T>(rows.AsQueryable(), EntityType.FindPrimaryKey()!.Properties.Single().Name);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => rows.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => rows.GetEnumerator();
    public override EntityEntry<T> Add(T entity) { ((ICollection<T>)rows).Add(entity); return context.Entry(entity); }
    public override EntityEntry<T> Remove(T entity) { ((ICollection<T>)rows).Remove(entity); return context.Entry(entity); }
    public override void RemoveRange(IEnumerable<T> entities) { foreach (var entity in entities.ToArray()) Remove(entity); }

    private sealed class Roots<TItem>(IQueryable<TItem> source, string key) : ExpressionVisitor
    {
        protected override Expression VisitExtension(Expression node)
        {
            if (node is FromSqlQueryRootExpression sql)
            {
                if (sql.Argument is not ConstantExpression { Value: object[] args } || args.Length != 1 ||
                    !sql.Sql.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
                    throw new NotSupportedException("Only single-key raw queries supported by this unit double.");
                var parameter = Expression.Parameter(typeof(TItem), "row");
                var predicate = Expression.Lambda<Func<TItem, bool>>(
                    Expression.Equal(Expression.Property(parameter, key), Expression.Constant(args[0])), parameter);
                return source.Where(predicate).Expression;
            }
            return node is EntityQueryRootExpression ? source.Expression : base.VisitExtension(node);
        }
    }
    private sealed class Provider<TItem>(IQueryable<TItem> source, string key) : IAsyncQueryProvider
    {
        private Expression Rewrite(Expression expression) => new Roots<TItem>(source, key).Visit(expression)!;
        public IQueryable CreateQuery(Expression expression) => throw new NotSupportedException();
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new AsyncQuery<TElement>(Rewrite(expression));
        public object? Execute(Expression expression) => source.Provider.Execute(Rewrite(expression));
        public TResult Execute<TResult>(Expression expression) => source.Provider.Execute<TResult>(Rewrite(expression));
        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
            => (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(typeof(TResult).GetGenericArguments()[0]).Invoke(null, [Execute(expression)])!;
    }
    private sealed class AsyncQuery<TItem> : EnumerableQuery<TItem>, IAsyncEnumerable<TItem>, IQueryable<TItem>
    {
        public AsyncQuery(Expression expression) : base(expression) { }
        IQueryProvider IQueryable.Provider => new AsyncProvider(this);
        public IAsyncEnumerator<TItem> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new Enumerator<TItem>(this.AsEnumerable().GetEnumerator());
    }
    private sealed class AsyncProvider(IQueryProvider inner) : IAsyncQueryProvider
    {
        public IQueryable CreateQuery(Expression expression) => throw new NotSupportedException();
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new AsyncQuery<TElement>(expression);
        public object? Execute(Expression expression) => inner.Execute(expression);
        public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);
        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
            => (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(typeof(TResult).GetGenericArguments()[0]).Invoke(null, [inner.Execute(expression)])!;
    }
    private sealed class Enumerator<TItem>(IEnumerator<TItem> inner) : IAsyncEnumerator<TItem>
    {
        public TItem Current => inner.Current;
        public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());
        public ValueTask DisposeAsync() { inner.Dispose(); return ValueTask.CompletedTask; }
    }
}
