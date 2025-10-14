using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.Linq;

// Utility classes for visualizing how expressions
// are structured and visited by ExpressionVisitor classes.

// Based on: https://blog.jeremylikness.com/blog/look-behind-the-iqueryable-curtain/


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


public class HierarchicalExpressionConsoleWriter : ExpressionVisitor
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
