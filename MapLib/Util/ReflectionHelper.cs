using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.Util;

public static class ReflectionHelper
{
    public static IEnumerable<string> GetFieldNames(object source)
    {
        var type = source.GetType();
        var fields = type.GetFields(
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);
        return fields.Select(f => f.Name);
    }

    public static object? GetFieldValue(object source, string fieldName)
    {
        var type = source.GetType();
        var field = type.GetField(fieldName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);
        if (field == null)
            throw new ArgumentException($"Field '{fieldName}' not found in type '{type.FullName}'");
        return field.GetValue(source);
    }



    public static object? GetPropertyValue(object source, string propertyName)
    {
        var type = source.GetType();
        var field = type.GetProperty(propertyName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);
        if (field == null)
            throw new ArgumentException($"Property '{propertyName}' not found in type '{type.FullName}'");
        return field.GetValue(source);
    }
}
