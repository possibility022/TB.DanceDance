using System.Linq.Expressions;

namespace TB.DanceDance.Videos.Comments;

/// <summary>
/// Copied from the old <c>Application.Extensions.ExpressionExtensions</c> (which is going away).
/// Combines two predicates with a logical OR while rebinding parameters so the result is a single
/// translatable expression tree.
/// </summary>
public static class ExpressionExtensions
{
    extension<T>(Expression<Func<T, bool>> expr1)
    {
        public Expression<Func<T, bool>> Or(Expression<Func<T, bool>> expr2)
        {
            var parameter = Expression.Parameter(typeof(T));

            var leftVisitor = new ReplaceParameterVisitor(expr1.Parameters[0], parameter);
            var left = leftVisitor.Visit(expr1.Body);

            var rightVisitor = new ReplaceParameterVisitor(expr2.Parameters[0], parameter);
            var right = rightVisitor.Visit(expr2.Body);

            return Expression.Lambda<Func<T, bool>>(
                Expression.OrElse(left!, right!), parameter);
        }
    }
}

class ReplaceParameterVisitor : ExpressionVisitor
{
    private readonly ParameterExpression oldParam;
    private readonly ParameterExpression newParam;

    public ReplaceParameterVisitor(ParameterExpression oldParam, ParameterExpression newParam)
    {
        this.oldParam = oldParam;
        this.newParam = newParam;
    }

    protected override Expression VisitParameter(ParameterExpression node)
        => node == oldParam ? newParam : node;
}
