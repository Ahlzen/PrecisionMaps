using MapLib.FileFormats.Vector;
using MapLib.Geometry;
using MapLib.Tests.Util;
using MapLib.Util;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        where T : Shape
    {
        TextWriter logger = Console.Out;

        // Evaluate the LINQ-to-OSM expression
        OsmExpressionVisitor visitor = new();
        visitor.Visit(expression);
        visitor.RunPostProcessing();
        
        // Build Overpass Query (OQL)
        string overpassQuery = visitor.BuildOverpassQuery(typeof(T));
        logger.WriteLine("---");
        logger.WriteLine("Expression:" + Environment.NewLine + expression.ToString());
        logger.WriteLine("Overpass QL:" + Environment.NewLine + overpassQuery);
        if (EvaluateOnly)
        {
            return [];
        }

        // Execute Overpass Query
        using HttpClient client = new();
        StringContent content = new(overpassQuery, Encoding.UTF8, "application/x-www-form-urlencoded");
        HttpResponseMessage response = await client.PostAsync(_overpassApiUrl, content);
        response.EnsureSuccessStatusCode();
        string rawXml = await response.Content.ReadAsStringAsync();

        // Parse and return the relevant data
        VectorData data = new OsmXmlDataReader().Parse(rawXml);
        // This can probably be made more efficient
        // TODO: Can we support multi-geometries?
        if (typeof(T) == typeof(Point))
            return data.Points.Cast<T>();
        else if (typeof(T) == typeof(Line))
            return data.Lines.Cast<T>();
        else if (typeof(T) == typeof(Polygon))
            return data.Lines.Cast<T>();
        else if (typeof(T) == typeof(Shape))
            return data.Points.Cast<T>()
                .Union(data.Lines.Cast<T>())
                .Union(data.Polygons.Cast<T>());
        else
            throw new NotSupportedException(
                $"Geometry type not supported: {typeof(T)}");
    }
}

internal class OsmExpressionVisitor : ExpressionVisitor
{
    // Bounding box, if specified. Expression should specify all or none.
    public double? XMin, XMax, YMin, YMax;

    /// <summary>
    /// Dictionaries of key/values to filter on. See remarks.
    /// </summary>
    /// <remarks>
    /// The value "*" is a wildcard.
    /// A value starting with '!' is negated (not equal to).
    /// Each dictionary is a list of conditions that all have to be
    /// true (intersection of all tag/value pairs).
    /// If more than one dictionary, the result is a union of these.
    /// 
    /// For example:
    /// 
    /// p["highway"] == "primary" && p["surface"] != "asphalt"
    ///   -> {{"highway","primary"} {"surface","!asphalt"}} (one dictionary)
    ///   -> OQL: ["highway"="primary"]["surface"!="asphalt"]
    ///
    /// p["highway"] == "primary" || p["highway"] == "secondary"
    ///   -> {{"highway","primary"}}, {{"highway","secondary"}} (two dictionaries)
    ///   -> OQL: (["highway"="primary"];["highway"="secondary"];)
    /// </remarks>
    public List<Dictionary<string, string>> TagFilterSets = new();

    /// <summary>
    /// List we're currently adding to. This ultimately gets
    /// added to TagFilters.
    /// </summary>
    private Dictionary<string, string> CurrentFilterSet = new();

    // Object type in OQL, such as "node", "way", "relation" or "nwr" (all).
    public string? OverpassObjectType = "nwr";


    /// <summary>
    /// Runs any processing to be peformed after the tree has been visited.
    /// </summary>
    public void RunPostProcessing()
    {
        // Make sure the filter set we were currently working on
        // is added to the main list.
        if (CurrentFilterSet.Any())
            TagFilterSets.Add(CurrentFilterSet);
    }

    private void AddToFilterSet(string key, string value, bool negate = false)
    {
        if (negate)
            CurrentFilterSet[key] = "!" + value;
        else
            CurrentFilterSet[key] = value;
    }

    private void StartNewFilterSet()
    {
        if (CurrentFilterSet.Any())
        {
            TagFilterSets.Add(CurrentFilterSet);
            CurrentFilterSet = new();
        }
    }

    /// <summary>Evaluates the specified expression as a value.</summary>
    /// <typeparam name="T">Expected type of the ealuated expression.</typeparam>
    /// <param name="ex">Expression to be evaluated.</param>
    /// <param name="value">Evaluated value, null if function returns fall.</param>
    /// <returns>True if successful, in which case value contains the result.</returns>
    private bool TryEvaluateValue<T>(Expression ex, [NotNullWhen(true)] out T? value)
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
                if (TryEvaluateValue<KeyValuePair<string, string>>(node.Arguments[1], out var kvp))
                    AddToFilterSet(kvp.Key, kvp.Value);
                else unsupportedExpression = true;
            }
            // Handle: IEnumerable<string>.Contains(p["tag"])
            else if (node.Arguments.Count > 1 &&
                node.Arguments[1] is MethodCallExpression mce &&
                mce.Method.Name == "get_Item")
            {
                if (TryEvaluateValue(mce.Arguments[0], out string? key))
                {
                    if (TryEvaluateValue(node.Arguments[0], out IEnumerable<string>? values))
                    {
                        foreach (string value in values) {
                            StartNewFilterSet();
                            AddToFilterSet(key, value);
                        }
                    }
                    else unsupportedExpression = true;
                }
                else unsupportedExpression = true;
            }
            else unsupportedExpression = true;
        }
        // Handle: .HasTag(value)
        else if (node.Method.Name == "HasTag")
        {
            // Right hand side must be something we can evaluate to a constant value
            if (TryEvaluateValue(node.Arguments[0], out string? key))
            {
                if (key != null)
                    AddToFilterSet(key, "*");
                else unsupportedExpression = true;
            }
            else unsupportedExpression = true;
        }
        // Handle: .IsWithin(bounds)
        else if (node.Method.Name == "IsWithin")
        {
            if (TryEvaluateValue(node.Arguments[0], out Bounds bounds)) {
                // Can't use deconstructor since LHS is nullable :(
                XMin = bounds.XMin;
                XMax = bounds.XMax;
                YMin = bounds.YMin;
                YMax = bounds.YMax;
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
                OverpassObjectType = "way"; // TODO: save actual target type?
            else if (typeArg == typeof(Shape))
                OverpassObjectType = "nwr";
            else unsupportedExpression = true;
            Visit(node.Arguments[0]);
        }
        else if (node.Method.Name == "get_Item")
        {
            // Indexer. This is handled elsewhere. Do nothing here.
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

        // Support: conditionA && conditionB (for tag filters)
        if (node.NodeType == ExpressionType.AndAlso)
        {
            Visit(node.Left);
            Visit(node.Right);
        }
        // Support: conditionA || conditionB (for tag filters)
        else if (node.NodeType == ExpressionType.OrElse)
        {
            Visit(node.Left);
            StartNewFilterSet();
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
                    AddToFilterSet(stringKey, stringValue,
                        negate: node.NodeType == ExpressionType.NotEqual);
                    return node;
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

    /// <summary>
    /// Calculates a sort-of hash for the whole dictionary. See
    /// usage further down for how and why this is needed.
    /// </summary>
    public static int CalcDictHash(Dictionary<string, string> dict) {
        // NOTE: this is not efficient, but it doesn't matter for this purpose
        // Adding up the hash of each KeyValuePair does not work since
        // that appears to be based only on the key, not the value.
        // 
        StringBuilder sb = new();
        foreach (var item in dict.OrderBy(kvp => kvp.Key)) {
            sb.Append(item.Key);
            sb.Append(item.Value);
            sb.Append(",");
        }
        return sb.ToString().GetHashCode();
    }

    public string BuildOverpassQuery(Type osmType)
    {
        var sb = new StringBuilder();
        sb.Append("[out:xml];\n");

        // Object type filter
        sb.Append(OverpassObjectType ?? "nwr"); // nwr is OQL for node, way, relation

        // Bit of a HACK!
        // To avoid a fair amount of bookkeeping traversing the tree
        // we may end up with duplicate dictionaries.
        // Dictionary<K,E> doesn't compute predictable hash codes
        // even when their elements do, so just Distinct() doesn't work,
        // hence DistinctBy() using our own function.
        // (stricly speaking this hash is not guaranteed to be unique, but
        // for our purposes it's extremely unlikely that we'll have a collision)
        IList<Dictionary<string, string>> filterSets =
            TagFilterSets.DistinctBy(CalcDictHash).ToList();

        // Tag type filter(s)
        bool hasFilterSets = filterSets.Count > 0;
        bool hasMultipleFilterSets = filterSets.Count > 1;

        if (hasFilterSets)
        {
            // If we have multiple filter sets, we need to take
            // the union of those, or in OQL (filter1;filter2;...)
            if (hasMultipleFilterSets) sb.Append("(\n  ");

            foreach (Dictionary<string,string> filterSet in filterSets)
            {
                foreach (KeyValuePair<string, string> item in filterSet)
                {
                    sb.Append($"[\"{item.Key}\"");
                    if (item.Value != "*")
                    {
                        if (item.Value.StartsWith("!"))
                            sb.Append($"!=\"{item.Value.Substring(1)}\"");
                        else
                            sb.Append($"=\"{item.Value}\"");
                    }
                    sb.Append("]");
                }
                if (hasMultipleFilterSets) sb.Append(";\n  ");
            }

            if (hasMultipleFilterSets) sb.Append(")");
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
