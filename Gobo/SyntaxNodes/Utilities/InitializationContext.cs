using Gobo.SyntaxNodes.Gml;

namespace Gobo.SyntaxNodes.PrintHelpers;

internal static class InitializationContext
{
    public static bool IsInSimpleStatement(GmlSyntaxNode? node)
    {
        GmlSyntaxNode? current = node?.Parent;
        while (current != null)
        {
            if (current is VariableDeclarator or AssignmentExpression or ReturnStatement)
            {
                return true;
            }

            if (current is ConditionalExpression
                or CallExpression
                or BinaryExpression
                or SwitchCase)
            {
                return false;
            }

            current = current.Parent;
        }

        return false;
    }
}
