using MapLib.Geometry;
using MapLib.Tests.Util;
using MapLib.Util;
using System.Diagnostics;
using System.Drawing.Interop;
using System.IO;
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

    /// <summary>
    /// If true, the expression is evaluated and the Overpass query is printed
    /// but no call is made the API. An empty result is returned.
    /// This is useful for debugging.
    /// </summary>
    public bool EvaluateOnly { get; set; } = false;

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
        TextWriter logger = Console.Out;

        //
        //new InOrderExpressionConsoleWriter().Visit(expression);
        //new HierarchicalExpressionConsoleWriter().Parse(expression);
        //return [];

        var visitor = new OsmExpressionVisitor();
        visitor.Visit(expression);
        string overpassQuery = visitor.BuildOverpassQuery(typeof(T));
        if (EvaluateOnly)
        {
            logger.WriteLine("---");
            logger.WriteLine("Expression:" + Environment.NewLine + expression.ToString());
            logger.WriteLine("Overpass QL:" + Environment.NewLine + overpassQuery);
            return [];
        }
        throw new NotImplementedException();


        //// Send HTTP request
        //using var client = new HttpClient();
        //var content = new StringContent(overpassQuery, Encoding.UTF8, "application/x-www-form-urlencoded");
        //var response = await client.PostAsync(_overpassApiUrl, content);
        //response.EnsureSuccessStatusCode();
        //var json = await response.Content.ReadAsStringAsync();
        //// Parse OSM JSON (TODO: improve)
        //JsonDocument doc = JsonDocument.Parse(json);
        //JsonElement elements = doc.RootElement.GetProperty("elements");
        //logger.WriteLine("Elements: " + Environment.NewLine + elements);


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
    // Bounding box, if specified. Expression should specify all or none.
    public double? XMin, XMax, YMin, YMax;

    // List of tag key/values to filter on. The value "*" is wildcard.
    public List<(string key, string value)> TagFilters = new();

    // Object type in OQL, such as "node", "way", "relation" or "nwr" (all).
    public string? OverpassObjectType = "nwr";


    /// <summary>Evaluates the specified expression as a value.</summary>
    /// <typeparam name="T">Expected type of the ealuated expression.</typeparam>
    /// <param name="ex">Expression to be evaluated.</param>
    /// <param name="value">Evaluated value, null if function returns fall.</param>
    /// <returns>True if successful, in which case value contains the result.</returns>
    private bool TryEvaluateValue<T>(Expression ex, out T? value)
    {
        if (ex is ConstantExpression ce)
        {
            if (ce.Value is T tValue)
            {
                value = tValue;
                return true;
            }
        }
        else
        {
            try
            {
                var lambda = Expression.Lambda(ex);
                var compiled = lambda.Compile();
                var rawValue = compiled.DynamicInvoke();
                if (rawValue is T tValue)
                {
                    value = tValue;
                    return true;
                }
            }
            catch { } // fall through and return false
        }
        value = default;
        return false;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        Debug.WriteLine("VisitMethodCall: " + node);

        bool unsupportedExpression = false;

        // Handle: Where(lambda)
        if (node.Method.Name == "Where")
        {
            if (node.Arguments[1] is UnaryExpression ue
                && ue.Operand is LambdaExpression lambda)
            {
                Visit(lambda.Body);
            }
            else unsupportedExpression = true;
            Visit(node.Arguments[0]);
        }
        // Handle: .Contains()
        else if (node.Method.Name == "Contains")
        {
            // Handle: Tags.Contains()
            if (node.Arguments[0] is MemberExpression sourceExpression &&
                sourceExpression.Member.Name == "Tags")
            {
                // Right hand side must be something we can evaluate to a constant value
                if (TryEvaluateValue<KeyValuePair<string,string>>(node.Arguments[1], out var kvp))
                {
                    var item = (kvp.Key, kvp.Value);
                    if (!TagFilters.Contains(item))
                        TagFilters.Add(item);
                }
                else unsupportedExpression = true;
            }
            else unsupportedExpression = true;
        }
        // Handle: .HasTag(value)
        else if (node.Method.Name == "HasTag")
        {
            // Right hand side must be something we can evaluate to a constant value
            if (TryEvaluateValue<string>(node.Arguments[0], out string? key))
            {
                if (key != null)
                {
                    (string, string) item = new(key, "*");
                    if (!TagFilters.Contains(item))
                        TagFilters.Add(item);
                }
                else unsupportedExpression = true;
            }
            else unsupportedExpression = true;
        }
        // Handle: .IsWithin(bounds)
        else if (node.Method.Name == "IsWithin")
        {
            // Handle: .IsWithin(existingBounds)
            if (node.Arguments.Count == 1 &&
                node.Arguments[0] is MemberExpression me &&
                me.Type == typeof(Bounds))
            {
                if (me.Expression is ConstantExpression ce && ce.Value != null)
                {
                    string fieldName = ReflectionHelper.GetFieldNames(ce.Value).First();
                    var bounds = ReflectionHelper.GetFieldValue(ce.Value, fieldName) as Bounds?;
                    if (bounds != null) {
                        // Can't use deconstructor since LHS is nullable :(
                        XMin = bounds.Value.XMin;
                        XMax = bounds.Value.XMax;
                        YMin = bounds.Value.YMin;
                        YMax = bounds.Value.YMax;
                    }
                    else unsupportedExpression = true;
                }
                else unsupportedExpression = true;
            }
            else if (node.Arguments.Count == 1 &&
                node.Arguments[0] is NewExpression ne &&
                ne.Type == typeof(Bounds) &&
                ne.Arguments.Count == 4)
            {
                XMin = (double)((ConstantExpression)ne.Arguments[0]).Value!;
                XMax = (double)((ConstantExpression)ne.Arguments[1]).Value!;
                YMin = (double)((ConstantExpression)ne.Arguments[2]).Value!;
                XMax = (double)((ConstantExpression)ne.Arguments[3]).Value!;
            }
            else unsupportedExpression = true;
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
        else unsupportedExpression = true;

        if (unsupportedExpression)
            throw new NotSupportedException(
                $"Method is not supported by LINQ-to-OSM: {node.Method.Name} ({node})");
        return base.VisitMethodCall(node);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        Debug.WriteLine("VisitBinary: " + node);

        bool unsupportedExpression = false;

        // Support: conditionA && conditionB
        if (node.NodeType == ExpressionType.AndAlso)
        {
            Visit(node.Left);
            Visit(node.Right);
        }
        else if (node.NodeType == ExpressionType.GreaterThanOrEqual ||
            node.NodeType == ExpressionType.LessThanOrEqual)
        {
            // Support: p.Coord.X <= ...
            if (node.Left is MemberExpression me && // actual: PropertyExpression
                (me.Member.Name == "X" || me.Member.Name == "Y"))
            {
                if (TryEvaluateValue<double>(node.Right, out var coord)) {
                    ref double? target = ref me.Member.Name == "X" ?
                        ref (node.NodeType == ExpressionType.GreaterThanOrEqual ? ref XMin : ref XMax) :
                        ref (node.NodeType == ExpressionType.GreaterThanOrEqual ? ref YMin : ref YMax);
                    target = coord;
                }
                else unsupportedExpression = true;
            }
            else unsupportedExpression = true;
        }
        // Support: equality, e.g. for tags
        else if (node.NodeType == ExpressionType.Equal ||
            node.NodeType == ExpressionType.NotEqual)
        {
            // Support: object["tag"] ==
            if (node.Left is MethodCallExpression indexerCall &&
                indexerCall.Method.Name == "get_Item" &&
                indexerCall.Arguments.Count == 1 &&
                indexerCall.Arguments[0] is ConstantExpression constKey &&
                constKey.Value is string stringKey)
            {
                // Support: object["tag"] == "value"
                if (node.Right is ConstantExpression constValue &&
                    constValue.Value is string stringValue)
                {
                    var item = (stringKey, stringValue);
                    if (!TagFilters.Contains(item))
                        TagFilters.Add(item);
                }
                else unsupportedExpression = true;
            }
            else unsupportedExpression = true;
        }
        else unsupportedExpression = true;

        if (unsupportedExpression)
            throw new NotSupportedException(
                $"Binary expression is not supported by LINQ-to-OSM: {node.NodeType} (left: {node.Left}, right: {node.Right})");
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
                if (value != "*") sb.Append($"=\"{value}\"");
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

// From https://blog.jeremylikness.com/blog/look-behind-the-iqueryable-curtain/
public class InOrderExpressionConsoleWriter : ExpressionVisitor
{
    protected override Expression VisitBinary(BinaryExpression node)
    {
        Console.Write($" binary:{node.NodeType} ");
        return base.VisitBinary(node);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.Method != null)
        {
            Console.Write($" unary:{node.Method.Name} ");
        }
        Console.Write($" unary:{node.Operand.NodeType} ");
        return base.VisitUnary(node);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        Console.Write($" constant:{node.Value} ");
        return base.VisitConstant(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        Console.Write($" member:{node.Member.Name} ");
        return base.VisitMember(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        Console.Write($" call:{node.Method.Name} ");
        return base.VisitMethodCall(node);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        Console.Write($" p:{node.Name} ");
        return base.VisitParameter(node);
    }
}
public class HierarchicalExpressionConsoleWriter
    : ExpressionVisitor
{
    int indent;

    private string Indent =>
        $"\r\n{new string('\t', indent)}";

    public void Parse(Expression expression)
    {
        indent = 0;
        Visit(expression);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Value is Expression value)
        {
            Visit(value);
        }
        else
        {
            Console.Write($"{node.Value}");
        }
        return node;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        Console.Write(node.Name);
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression != null)
        {
            Visit(node.Expression);
        }
        Console.Write($".{node.Member?.Name}.");
        return node;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Object != null)
        {
            Visit(node.Object);
        }
        Console.Write($"{Indent}{node.Method.Name}( ");
        var first = true;
        indent++;
        foreach (var arg in node.Arguments)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                indent--;
                Console.Write($"{Indent},");
                indent++;
            }
            Visit(arg);
        }
        indent--;
        Console.Write(") ");
        return node;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        Console.Write($"{Indent}<");
        indent++;
        Visit(node.Left);
        indent--;
        Console.Write($"{Indent}{node.NodeType}");
        indent++;
        Visit(node.Right);
        indent--;
        Console.Write(">");
        return node;
    }
}