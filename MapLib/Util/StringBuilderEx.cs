using System;
using System.Text;

namespace MapLib.Util;

public class StringBuilderEx
{
    private readonly StringBuilder _sb;

    private int _indentLevel = 0;
    private string _indentString = "  "; // Default: 2 spaces

    // Constructors

    public StringBuilderEx()
    {
        _sb = new();
    }

    public StringBuilderEx(string s)
    {
        _sb = new(s);
    }

    public StringBuilderEx(int capacity)
    {
        _sb = new(capacity);
    }

    // Properties

    public int Capacity { get => _sb.Capacity; set => _sb.Capacity = value; }
    public int MaxCapacity => _sb.MaxCapacity;
    public int Length { get => _sb.Length; set => _sb.Length = value; }

    public int IndentLevel => _indentLevel;
    public string IndentString
    {
        get => _indentString;
        set => _indentString = value ?? "";
    }

    // Indexers

    public char this[int index]
    {
        get => _sb[index];
        set => _sb[index] = value;
    }

    // Indentation methods

    public void Indent()
    {
        _indentLevel++;
    }

    public void Unindent()
    {
        if (_indentLevel > 0)
            _indentLevel--;
    }

    private void AppendIndent()
    {
        if (_indentLevel > 0)
            _sb.Append(string.Concat(Enumerable.Repeat(_indentString, _indentLevel)));
    }

    // Append methods

    public StringBuilderEx Append(string value)
    {
        _sb.Append(value);
        return this;
    }

    public StringBuilderEx Append(object value)
    {
        _sb.Append(value);
        return this;
    }

    public StringBuilderEx StartLine()
    {
        AppendIndent();
        return this;
    }

    public StringBuilderEx StartLine(string value)
    {
        AppendIndent();
        _sb.Append(value);
        return this;
    }

    public StringBuilderEx StartLine(object value)
    {
        AppendIndent();
        _sb.Append(value);
        return this;
    }

    public StringBuilderEx AppendLine(string value)
    {
        AppendIndent();
        _sb.AppendLine(value);
        return this;
    }

    public StringBuilderEx AppendLine(object value)
    {
        AppendIndent();
        _sb.Append(value);
        _sb.AppendLine();
        return this;
    }

    public StringBuilderEx AppendLine()
    {
        _sb.AppendLine();
        return this;
    }

    // Other methods

    public StringBuilderEx Clear()
    {
        _sb.Clear();
        return this;
    }

    public override string ToString()
    {
        return _sb.ToString();
    }
}