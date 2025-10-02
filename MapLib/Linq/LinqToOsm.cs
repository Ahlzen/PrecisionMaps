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

        // Send HTTP request
        using var client = new HttpClient();
        var content = new StringContent(overpassQuery, Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = await client.PostAsync(_overpassApiUrl, content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();

        // Parse OSM JSON (TODO: improve)
        JsonDocument doc = JsonDocument.Parse(json);
        JsonElement elements = doc.RootElement.GetProperty("elements");

        logger.WriteLine("Elements: " + Environment.NewLine + elements);

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
                // Handle: Tags.Contains(new KeyValuePair<string, string>("key", "value"))
                if (node.Arguments[1] is NewExpression valueNewExpression &&
                    valueNewExpression.Type == typeof(KeyValuePair<string, string>))
                {
                    string key = ((valueNewExpression.Arguments[0] as ConstantExpression)?.Value as string) ?? "";
                    string value = ((valueNewExpression.Arguments[1] as ConstantExpression)?.Value as string) ?? "";
                    var item = (key, value);
                    if (!TagFilters.Contains(item))
                        TagFilters.Add(item);
                }
                // Handle: Tags.Contains(existingKeyValuePair)
                else if (node.Arguments[1] is MemberExpression valueMemberExpression &&
                    valueMemberExpression.Type == typeof(KeyValuePair<string, string>) &&
                    valueMemberExpression.Expression is ConstantExpression ce)
                {
                    object? source = ce.Value; // value is anonymous object; need to extract the only member field
                    if (source != null)
                    {
                        string fieldName = ReflectionHelper.GetFieldNames(source).First();
                        var kvpObj = ReflectionHelper.GetFieldValue(source, fieldName)
                            as KeyValuePair<string, string>?;
                        if (kvpObj != null) {
                            var kvp = kvpObj.Value;
                            var item = (kvp.Key, kvp.Value);
                            if (!TagFilters.Contains(item))
                                TagFilters.Add(item);
                        }
                        else unsupportedExpression = true;
                    }
                    else unsupportedExpression = true;
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
                    if (bounds != null)
                    {
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
        bool unsupportedExpression = false;

        // Support: conditionA && conditionB
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
            // Support p.Coord.X <= ...
            if (node.Left is MemberExpression memberExpression && // actually PropertyExpression
                (memberExpression.Member.Name == "Y" || memberExpression.Member.Name == "X"))
            {
                // Support p.Coord.X <= 12.3
                if (node.Right is ConstantExpression constantExpression)
                {
                    if (memberExpression.Member.Name == "X")
                    {
                        if (node.NodeType == ExpressionType.GreaterThan ||
                            node.NodeType == ExpressionType.GreaterThanOrEqual)
                            XMin = Convert.ToDouble(constantExpression.Value);
                        else
                            XMax = Convert.ToDouble(constantExpression.Value);
                    }
                    else if (memberExpression.Member.Name == "Y")
                    {
                        if (node.NodeType == ExpressionType.GreaterThan ||
                            node.NodeType == ExpressionType.GreaterThanOrEqual)
                            YMin = Convert.ToDouble(constantExpression.Value);
                        else
                            YMax = Convert.ToDouble(constantExpression.Value);
                    }
                    else unsupportedExpression = true;
                }
                // Support: p.Coord.X <= bounds.XMax
                else if (node.Right is MemberExpression rightMemberExpression &&
                    rightMemberExpression.Type == typeof(double) &&
                    rightMemberExpression.Expression is MemberExpression fieldExpression &&
                    fieldExpression.Expression is ConstantExpression constantExpression2 &&
                    constantExpression2.NodeType == ExpressionType.Constant)
                {
                    object source = constantExpression2.Value!;
                    string fieldName = fieldExpression.Member.Name;
                    object? obj = ReflectionHelper.GetFieldValue(source, fieldName);
                    if (obj is Bounds bounds)
                    {
                        string subMemberName = rightMemberExpression.Member.Name;
                        object? value = ReflectionHelper.GetPropertyValue(obj, subMemberName);
                        if (value is double)
                        {

                            if (memberExpression.Member.Name == "X")
                            {
                                if (node.NodeType == ExpressionType.GreaterThan ||
                                    node.NodeType == ExpressionType.GreaterThanOrEqual)
                                    XMin = (double)value;
                                else
                                    XMax = (double)value;
                            }
                            else if (memberExpression.Member.Name == "Y")
                            {
                                if (node.NodeType == ExpressionType.GreaterThan ||
                                    node.NodeType == ExpressionType.GreaterThanOrEqual)
                                    YMin = (double)value;
                                else
                                    YMax = (double)value;
                            }
                        }
                        else unsupportedExpression = true;
                    }
                    else unsupportedExpression = true;
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