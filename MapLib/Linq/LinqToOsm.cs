using MapLib.Geometry;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
namespace MapLib.Linq;

public class Osm<T> : IQueryable<T>, IQueryable
{
    public Type ElementType => typeof(T);
    public Expression Expression { get; }
    public IQueryProvider Provider { get; }

    public Osm(OsmQueryProvider provider, Expression? expression = null)
    {
        if (!typeof(Shape).IsAssignableFrom(typeof(T)))
            throw new NotSupportedException(
                $"Osm<T> only supports types implementing Shape. '{typeof(T)}' is not supported.");
        Provider = provider;
        Expression = expression ?? Expression.Constant(this);
    }

    public IEnumerator<T> GetEnumerator()
    {
        IEnumerable<Shape>? result =
            Provider.Execute(Expression) as IEnumerable<Shape>;
        if (result == null)
            yield break; // throw?
        foreach (Shape item in result)
        {
            if (item is T tItem)
                yield return tItem;
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class OsmQueryProvider : IQueryProvider
{
    private readonly string _overpassApiUrl;

    public OsmQueryProvider(string overpassApiUrl = "https://overpass-api.de/api/interpreter")
    {
        _overpassApiUrl = overpassApiUrl;
    }

    public IQueryable CreateQuery(Expression expression)
        => (IQueryable)Activator.CreateInstance(typeof(Osm<>)
            .MakeGenericType(expression.Type.GetGenericArguments()[0]), this, expression)!;

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        => new Osm<TElement>(this, expression);

    public object? Execute(Expression expression)
        => ExecuteAsync<Shape>(expression).GetAwaiter().GetResult();

    public TResult Execute<TResult>(Expression expression)
        => (TResult)Execute(expression);
        
    private async Task<IEnumerable<T>> ExecuteAsync<T>(Expression expression)
    {
        Debug.WriteLine("Expression: " + Environment.NewLine + expression.ToString());

        // Parse the expression tree to extract OSM query parameters
        var visitor = new OsmExpressionVisitor();
        visitor.Visit(expression);

        // Build Overpass QL query
        string overpassQuery = visitor.BuildOverpassQuery(typeof(T));

        Debug.WriteLine("Overpass QL:\n" + overpassQuery);

        return []; // Temporary stub to allow compilation


        //// Send HTTP request
        //using var client = new HttpClient();
        //var content = new StringContent(overpassQuery, Encoding.UTF8, "application/x-www-form-urlencoded");
        //var response = await client.PostAsync(_overpassApiUrl, content);
        //response.EnsureSuccessStatusCode();
        //var json = await response.Content.ReadAsStringAsync();

        //// Parse OSM JSON (simplified, for demo)
        //JsonDocument doc = JsonDocument.Parse(json);
        //JsonElement elements = doc.RootElement.GetProperty("elements");

        //Debug.WriteLine("Elements: " + Environment.NewLine + elements);

        //throw new NotImplementedException();

        //var results = new List<T>();
        //foreach (var el in elements.EnumerateArray())
        //{
        //    if (typeof(T) == typeof(OsmNode) && el.GetProperty("type").GetString() == "node")
        //        results.Add((T)(object)new OsmNode
        //        {
        //            Id = el.GetProperty("id").GetInt64(),
        //            Lat = el.GetProperty("lat").GetDouble(),
        //            Lon = el.GetProperty("lon").GetDouble(),
        //            Tags = el.TryGetProperty("tags", out var tags) ? tags.EnumerateObject().ToDictionary(t => t.Name, t => t.Value.GetString() ?? "") : new()
        //        });
        //    else if (typeof(T) == typeof(OsmWay) && el.GetProperty("type").GetString() == "way")
        //        results.Add((T)(object)new OsmWay
        //        {
        //            Id = el.GetProperty("id").GetInt64(),
        //            Nodes = el.GetProperty("nodes").EnumerateArray().Select(n => n.GetInt64()).ToList(),
        //            Tags = el.TryGetProperty("tags", out var tags) ? tags.EnumerateObject().ToDictionary(t => t.Name, t => t.Value.GetString() ?? "") : new()
        //        });
        //    else if (typeof(T) == typeof(OsmRelation) && el.GetProperty("type").GetString() == "relation")
        //        results.Add((T)(object)new OsmRelation
        //        {
        //            Id = el.GetProperty("id").GetInt64(),
        //            Members = el.GetProperty("members").EnumerateArray().Select(m => (object)m).ToList(),
        //            Tags = el.TryGetProperty("tags", out var tags) ? tags.EnumerateObject().ToDictionary(t => t.Name, t => t.Value.GetString() ?? "") : new()
        //        });
        //}
        //return results;
    }
}

internal class OsmExpressionVisitor : ExpressionVisitor
{
    public double? XMin, XMax, YMin, YMax;
    public List<(string key, string value)> TagFilters = new();
    public string? OverpassObjectType; // "node", "way", "relation", or null for all

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // Handle: Where(lambda)
        if (node.Method.Name == "Where")
        {
            if (node.Arguments[1] is UnaryExpression ue
                && ue.Operand is LambdaExpression lambda)
            {
                Visit(lambda.Body);
            }
            Visit(node.Arguments[0]);
        }
        
        // Handle: Tags.Contains(new KeyValuePair<string, string>("key", "value"))
        else if (node.Method.Name == "Contains")
        {
            MemberExpression? memberExpression = node.Arguments[0] as MemberExpression; // actually PropertyExpression

            // TODO: evaluate instead, may be existing variable instead of new
            NewExpression? newExpression = node.Arguments[1] as NewExpression;

            if (memberExpression != null &&
                memberExpression.Member.Name == "Tags" &&
                newExpression != null &&
                newExpression.Type == typeof(KeyValuePair<string,string>))
            {
                string key = ((newExpression.Arguments[0] as ConstantExpression)?.Value as string) ?? "";
                string value = ((newExpression.Arguments[1] as ConstantExpression)?.Value as string) ?? "";
                var item = (key, value);
                if (!TagFilters.Contains(item))
                    TagFilters.Add(item);
            }
        }

        // Handle: OfType<T>
        else if (node.Method.Name == "OfType" && node.Method.IsGenericMethod)
        {
            var typeArg = node.Method.GetGenericArguments()[0];
            if (typeArg == typeof(Point))
                OverpassObjectType = "node";
            else if (typeArg == typeof(Line))
                OverpassObjectType = "way";
            else if (typeArg == typeof(Polygon))
                OverpassObjectType = "way";
            Visit(node.Arguments[0]);
        }

        return base.VisitMethodCall(node);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        //// Support: x => x.Tags["highway"] == "residential"
        //if (node.NodeType == ExpressionType.Equal)
        //{
        //    if (node.Left is MethodCallExpression cx &&
        //        cx.Method.Name == "get_Item" &&
        //        cx.Object is MemberExpression e &&
        //        e.Member.Name == "Tags")
        //    {
        //        Debug.WriteLine($"{cx.Method} {cx.Type} {cx.Object} {cx.Arguments}");

        //        if (cx.Arguments[0] is ConstantExpression keyExpr &&
        //            node.Right is ConstantExpression valExpr)
        //        {
        //            string key = keyExpr.Value?.ToString() ?? "";
        //            string value = valExpr.Value?.ToString() ?? "";
        //            var item = (key, value);
        //            if (!TagFilters.Contains(item))
        //                TagFilters.Add(item);
        //        }
        //    }
        //    // Support: x => x.Type == "node"
        //    if (node.Left is MemberExpression member && member.Member.Name == "Type" && node.Right is ConstantExpression typeExpr)
        //    {
        //        OverpassObjectType = typeExpr.Value?.ToString();
        //    }
        //}

        // Support: bounding box, e.g. x => x.Lat >= min && x.Lat <= max
        if (node.NodeType == ExpressionType.AndAlso)
        {
            Visit(node.Left);
            Visit(node.Right);
        }
        // Support: x.Lat >= min, x.Lat > min, x.Lat <= max, x.Lat < max
        // NOTE: For simplicity, we treat less/greater than the same as -or-equal.
        // (for geodata it usually doesn't matter)
        else if (node.NodeType == ExpressionType.GreaterThanOrEqual ||
            node.NodeType == ExpressionType.LessThanOrEqual ||
            node.NodeType == ExpressionType.GreaterThan ||
            node.NodeType == ExpressionType.LessThan)
        {
            MemberExpression? memberExpression = node.Left as MemberExpression; // actually PropertyExpression
            ConstantExpression? constantExpression = node.Right as ConstantExpression; // TODO: evaluate instead, may be existing variable instead of new

            if (memberExpression != null && memberExpression.Expression != null &&
                memberExpression.Expression.Type == typeof(Coord) &&
                (memberExpression.Member.Name == "Y" || memberExpression.Member.Name == "X") &&
                constantExpression != null)
            {
                if (memberExpression.Member.Name == "X")
                {
                    if (node.NodeType == ExpressionType.GreaterThan ||
                        node.NodeType == ExpressionType.GreaterThanOrEqual)
                        XMin = Convert.ToDouble(constantExpression.Value);
                    else
                        XMax = Convert.ToDouble(constantExpression.Value);  
                }
                if (memberExpression.Member.Name == "Y")
                {
                    if (node.NodeType == ExpressionType.GreaterThan ||
                        node.NodeType == ExpressionType.GreaterThanOrEqual)
                        YMin = Convert.ToDouble(constantExpression.Value);
                    else
                        YMax = Convert.ToDouble(constantExpression.Value);
                }
            }
        }

        return base.VisitBinary(node);
    }

    public string BuildOverpassQuery(Type osmType)
    {
        var sb = new StringBuilder();
        sb.Append("[out:json];\n");

        // Object type filter
        sb.Append(OverpassObjectType ?? "nwr"); // nwr is OQL for node, way, relation

        // Tag type filter(s)
        if (TagFilters.Count > 0)
        {
            foreach (var (key, value) in TagFilters)
            {
                sb.Append($"[\"{key}\"");
                if (value != null) sb.Append($"=\"{value}\"");
                sb.Append("]");
            }
        }

        // Bounding box filter
        if (YMin.HasValue && XMin.HasValue && YMax.HasValue && XMax.HasValue)
        {
            sb.Append($"({YMin.Value},{XMin.Value},{YMax.Value},{XMax.Value})");
        }

        sb.Append(";\nout body;");
        return sb.ToString();
    }
}