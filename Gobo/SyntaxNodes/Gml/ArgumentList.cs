using Gobo.Printer.DocTypes;
using Gobo.SyntaxNodes.PrintHelpers;

namespace Gobo.SyntaxNodes.Gml;

internal sealed class ArgumentList : GmlSyntaxNode
{
    public List<GmlSyntaxNode> Arguments => Children;
    public static Doc EmptyArguments => "()";

    public ArgumentList(TextSpan span, List<GmlSyntaxNode> arguments)
            : base(span)
    {
        while (arguments.Count > 0 && IsRedundant(arguments[^1]))
        {
            arguments.RemoveAt(arguments.Count - 1);
        }

        _ = AsChildren(arguments);
    }

    public override Doc PrintNode(PrintContext ctx)
    {
        Doc result;

        PrintOwnComments = false;

        if (Children.Count == 0 && !DanglingComments.Any())
        {
            return EmptyArguments;
        }

        if (ctx.Options.MultilineArguments && Children.Count > 1)
        {
            result = DelimitedList.PrintInBrackets(ctx, "(", this, ")", ",", forceBreak: true);
        }
        else if (ShouldBreakOnLastArgument())
        {
            var printedArguments = PrintChildren(ctx);

            Doc optionA;

            if (Children.Count == 1)
            {
                optionA = Doc.Group("(", Doc.Concat(printedArguments), ")");
            }
            else
            {
                var last = printedArguments.Last();
                var allExceptLast = printedArguments.SkipLast(1);

                var separator = Doc.Concat(",", " ");

                optionA = Doc.Group("(", Doc.Join(separator, allExceptLast), separator, last, ")");
            }

            var optionB = DelimitedList.PrintInBrackets(ctx, "(", this, ")", ",");

            result = Doc.ConditionalGroup(optionA, optionB);
        }
        else
        {
            bool anyChildHasComments = Children.Any(c => c.Comments.Count != 0);

            if (anyChildHasComments)
            {
                result = DelimitedList.PrintInBrackets(
                    ctx,
                    "(",
                    this,
                    ")",
                    ",",
                    allowTrailingSeparator: true,
                    forceBreak: true,
                    leadingContents: PrintLeadingComments(ctx)
                );
            }
            else
            {
                result = DelimitedList.PrintInBrackets(
                    ctx,
                    "(",
                    this,
                    ")",
                    ",",
                    leadingContents: PrintLeadingComments(ctx)
                );
            }
        }

        var printed = Doc.Concat(result, PrintTrailingComments(ctx));

        return ctx.Options.FlatExpressions ? Doc.ForceFlat(printed) : printed;
    }

    private bool ShouldBreakOnLastArgument()
    {
        if (
            Children.Count == 0
            || LeadingComments.Any() // The leading comment(s) will end up inside the argument list
            || Children.Any(c => c.Comments.Any(g => g.Placement == CommentPlacement.OwnLine))
        )
        {
            return false;
        }

        if (Children.Count == 1)
        {
            return Children[0] is FunctionDeclaration or StructExpression;
        }

        return Children.Last() is FunctionDeclaration or StructExpression
            && Children.Take(Children.Count - 1).All(arg => arg is not FunctionDeclaration);
    }

    private static bool IsRedundant(GmlSyntaxNode node) => node.Comments.Count == 0
            && (node is UndefinedArgument || (node is Identifier { Name: "undefined" }));
}
