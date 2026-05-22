using Gobo.Printer.DocTypes;
using Gobo.SyntaxNodes.Gml;

namespace Gobo.SyntaxNodes.PrintHelpers;

internal class PrintedNode
{
    public Doc Doc;
    public GmlSyntaxNode Node;

    public PrintedNode(GmlSyntaxNode node, Doc doc)
    {
        Doc = doc;
        Node = node;
    }
};

internal interface IMemberChainable
{
    public GmlSyntaxNode Object { get; set; }
    public Doc PrintInChain(PrintContext ctx);
    public void SetObject(GmlSyntaxNode node);
}

internal static class MemberChain
{
    public static Doc PrintMemberChain(PrintContext ctx, GmlSyntaxNode node)
    {
        var printedNodes = new List<PrintedNode>();

        FlattenAndPrintNodes(ctx, node, printedNodes);

        if (ctx.Options.FlatExpressions)
        {
            return Doc.ForceFlat(printedNodes.Select(x => x.Doc).ToArray());
        }

        bool isSimpleStatement = InitializationContext.IsInSimpleStatement(node);
        int callCount = printedNodes.Count(o => o.Node is CallExpression);
        bool forceBreak = ctx.Options.MultilineChainedMethods && callCount > 1 && isSimpleStatement;

        List<List<PrintedNode>> groups = printedNodes.Any(o => o.Node is CallExpression)
            ? GroupPrintedNodesPrettierStyle(printedNodes)
            : GroupPrintedNodesOnLines(printedNodes);

        Doc[] oneLine = groups.SelectMany(o => o).Select(o => o.Doc).ToArray();

        if (forceBreak)
        {
            int firstCallGroupIndex = FindFirstGroupWithCallExpression(groups);

            var firstPart = Doc.Concat(
                groups
                    .Take(firstCallGroupIndex + 1)
                    .SelectMany(g => g)
                    .Select(p => p.Doc)
                    .ToArray()
            );

            var remainingGroups = groups.Skip(firstCallGroupIndex + 1).ToList();
            var restPart = PrintIndentedGroup(node, remainingGroups);

            return Doc.Concat(firstPart, restPart);
        }

        bool shouldMergeFirstTwoGroups = ShouldMergeFirstTwoGroups(groups);

        int cutoff = shouldMergeFirstTwoGroups ? 3 : 2;

        bool forceOneLine =
            groups.Count <= cutoff
            && groups
                .Skip(shouldMergeFirstTwoGroups ? 1 : 0)
                .Any(o => o.Last().Node is not CallExpression)
            && !oneLine.Any(DocUtilities.ContainsBreak);

        if (forceOneLine)
        {
            return Doc.Group(oneLine);
        }

        if (groups.Count > 1 && groups[0].Last().Node.TrailingComments.Any())
        {
            // Indent trailing comment doc at the end of the first group
            IList<Doc> contents = ((Concat)groups[0].Last().Doc).Contents;
            contents[^1] = Doc.Indent(contents[^1]);
        }

        var expanded = Doc.Concat(
            Doc.Concat(groups[0].Select(o => o.Doc).ToArray()),
            shouldMergeFirstTwoGroups
                ? Doc.Concat(groups[1].Select(o => o.Doc).ToArray())
                : Doc.Null,
            PrintIndentedGroup(node, groups.Skip(shouldMergeFirstTwoGroups ? 2 : 1).ToList())
        );

        return oneLine.Skip(1).Any(DocUtilities.ContainsBreak)
            ? expanded
            : Doc.ConditionalGroup(Doc.Concat(oneLine), expanded);
    }

    private static void FlattenAndPrintNodes(
        PrintContext ctx,
        GmlSyntaxNode expression,
        List<PrintedNode> printedNodes
    )
    {
        if (expression is IMemberChainable chainable)
        {
            FlattenAndPrintNodes(ctx, chainable.Object, printedNodes);

            Doc printed;

            if (expression.Parent is IMemberChainable)
            {
                if (
                    expression.TrailingComments.Any()
                    && expression is MemberDotExpression
                    && expression.Parent is CallExpression or MemberIndexExpression
                )
                {
                    // We are attempting to print a comment before an argument list or element access.
                    // Print comments as leading instead.

                    printed = Doc.Concat(
                        Doc.HardLineIfNoPreviousLine,
                        expression.PrintTrailingComments(ctx, CommentType.Dangling),
                        Doc.HardLine,
                        chainable.PrintInChain(ctx)
                    );
                }
                else
                {
                    printed = Doc.Concat(
                        chainable.PrintInChain(ctx),
                        expression.PrintTrailingComments(ctx)
                    );
                }
            }
            else
            {
                // Top-level parent nodes in chain expressions do not need comment handling
                printed = chainable.PrintInChain(ctx);
            }

            printedNodes.Add(new PrintedNode(expression, printed));
        }
        else
        {
            printedNodes.Add(new PrintedNode(expression, expression.Print(ctx)));
        }
    }

    private static List<List<PrintedNode>> GroupPrintedNodesOnLines(List<PrintedNode> printedNodes)
    {
        // We want to group the printed nodes in the following manner
        //
        //   a.b.c.d

        // so that we can print it like this if it breaks
        //   a
        //     .b
        //     .c
        //     .d

        var groups = new List<List<PrintedNode>>();

        var currentGroup = new List<PrintedNode> { printedNodes[0] };
        groups.Add(currentGroup);

        for (int index = 1; index < printedNodes.Count; index++)
        {
            if (printedNodes[index].Node is MemberDotExpression or Identifier)
            {
                currentGroup = new List<PrintedNode>();
                groups.Add(currentGroup);
            }

            currentGroup.Add(printedNodes[index]);
        }

        return groups;
    }

    private static List<List<PrintedNode>> GroupPrintedNodesPrettierStyle(
        List<PrintedNode> printedNodes
    )
    {
        // We want to group the printed nodes in the following manner
        //
        //   a().b.c().d().e
        // will be grouped as
        //   [
        //     [Identifier, ArgumentsList],
        //     [MemberDotExpression]
        //     [MemberDotExpression, ArgumentsList],
        //     [MemberDotExpression, ArgumentsList],
        //     [MemberDotExpression],
        //   ]

        // so that we can print it as
        //   a()
        //     .b
        //     .c()
        //     .d()
        //     .e

        var groups = new List<List<PrintedNode>>();
        var currentGroup = new List<PrintedNode> { printedNodes[0] };
        int index = 1;
        for (; index < printedNodes.Count; index++)
        {
            if (printedNodes[index].Node is CallExpression)
            {
                currentGroup.Add(printedNodes[index]);
            }
            else
            {
                break;
            }
        }

        if (
            printedNodes[0].Node is not CallExpression
            && index < printedNodes.Count
            && printedNodes[index].Node is MemberIndexExpression
        )
        {
            currentGroup.Add(printedNodes[index]);
            index++;
        }

        groups.Add(currentGroup);
        currentGroup = new List<PrintedNode>();

        bool hasSeenNodeThatRequiresBreak = false;
        for (; index < printedNodes.Count; index++)
        {
            if (hasSeenNodeThatRequiresBreak && printedNodes[index].Node is MemberDotExpression)
            {
                groups.Add(currentGroup);
                currentGroup = new List<PrintedNode>();
                hasSeenNodeThatRequiresBreak = false;
            }

            if (
                printedNodes[index].Node
                is CallExpression or MemberDotExpression or MemberIndexExpression
            )
            {
                hasSeenNodeThatRequiresBreak = true;
            }
            currentGroup.Add(printedNodes[index]);
        }

        if (currentGroup.Count != 0)
        {
            groups.Add(currentGroup);
        }

        return groups;
    }

    private static Doc PrintIndentedGroup(GmlSyntaxNode node, IList<List<PrintedNode>> groups)
    {
        if (groups.Count == 0)
        {
            return Doc.Null;
        }

        return Doc.Indent(
            Doc.Group(
                Doc.HardLine,
                Doc.Join(
                    Doc.HardLine,
                    groups.Select(o => Doc.Group(o.Select(p => p.Doc).ToArray()))
                )
            )
        );
    }

    private static bool ShouldMergeFirstTwoGroups(List<List<PrintedNode>> groups)
    {
        if (groups.Count < 2)
        {
            return false;
        }

        GmlSyntaxNode firstNode = groups[0][0].Node;

        if (groups[0].Count == 1 && firstNode is Identifier)
        {
            return true;
        }

        if (groups.Count == 2 && groups.All(g => g.Count == 2 && g[1].Node is CallExpression))
        {
            List<PrintedNode> firstGroup = groups[0];
            List<PrintedNode> lastGroup = groups[1];

            if (
                firstGroup.Last().Node.TrailingComments.Any()
                || (
                    lastGroup[0].Node is MemberDotExpression member
                    && member.Property.LeadingComments.Any()
                )
            )
            {
                return false;
            }

            return true;
        }

        return false;
    }

    private static int FindFirstGroupWithCallExpression(List<List<PrintedNode>> groups)
    {
        for (int i = 0; i < groups.Count; i++)
        {
            if (groups[i].Any(p => p.Node is CallExpression))
            {
                return i;
            }
        }
        return 0;
    }
}
