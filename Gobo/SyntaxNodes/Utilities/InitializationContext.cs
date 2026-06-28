using Gobo.SyntaxNodes.Gml;

namespace Gobo.SyntaxNodes.PrintHelpers;

internal static class InitializationContext
{
    public static bool IsInSimpleStatement(GmlSyntaxNode? node)
    {
        foreach (GmlSyntaxNode ancestor in AncestorsOf(node))
        {
            if (ancestor is ConditionalExpression or CallExpression or BinaryExpression or SwitchCase or FunctionDeclaration)
            {
                return false;
            }

            if (ancestor is VariableDeclarator or AssignmentExpression or ReturnStatement)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsInNewExpression(GmlSyntaxNode? node)
        => AncestorsOf(node).Any(a => a is NewExpression);

    private static IEnumerable<GmlSyntaxNode> AncestorsOf(GmlSyntaxNode? node)
    {
        GmlSyntaxNode? current = node?.Parent;
        while (current != null)
        {
            yield return current;
            current = current.Parent;
        }
    }
}
