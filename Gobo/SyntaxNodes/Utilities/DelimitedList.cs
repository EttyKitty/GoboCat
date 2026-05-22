using Gobo.Printer.DocTypes;
using Gobo.SyntaxNodes.Gml;

namespace Gobo.SyntaxNodes.PrintHelpers;

internal class DelimitedList
{
    public static Doc PrintInBrackets(
        PrintContext ctx,
        string openToken,
        GmlSyntaxNode arguments,
        string closeToken,
        string separator,
        bool allowTrailingSeparator = false,
        bool forceBreak = false,
        Doc? leadingContents = null
    )
    {
        var parts = new List<Doc> { openToken };

        var groupId = Guid.NewGuid().ToString();

        if (
            arguments.Children.Count == 0
            && leadingContents is null
            && !arguments.DanglingComments.Any()
        )
        {
            return Doc.Concat(openToken, closeToken);
        }

        leadingContents ??= Doc.Null;

        var isStruct = arguments is StructExpression;
        var isArray = arguments is ArrayExpression;

        var isSimpleStatement = InitializationContext.IsInSimpleStatement(arguments);

        var forceVerticalLayout = forceBreak
            || (isSimpleStatement
                && ((isStruct && ctx.Options.MultilineStructs)
                    || (isArray && ctx.Options.MultilineArrays && arguments.Children.Count > 1)));

        if (arguments.Children.Count > 0)
        {
            Doc printedArguments = Print(
                ctx,
                arguments,
                separator,
                allowTrailingSeparator,
                forceBreak,
                leadingContents,
                forceInternalBreaks: forceVerticalLayout
            );

            LineDoc lineBreak = Doc.SoftLine;

            if (forceVerticalLayout)
            {
                lineBreak = Doc.HardLine;
            }
            else if (openToken == "[")
            {
                // Special case: add a space between [ and #.
                int index = arguments.Children[0].Span.Start;
                char firstCharacter = ctx.SourceText.ReadSpan(index, index + 1)[0];
                if (firstCharacter == '#')
                {
                    lineBreak = Doc.Line;
                }
            }

            var contents = Doc.Concat(lineBreak, printedArguments);
            parts.Add(Doc.IndentIfBreak(contents, groupId));
        }
        else
        {
            parts.Add(
                Doc.Indent(Doc.SoftLine, leadingContents, arguments.PrintDanglingComments(ctx))
            );
        }

        if (forceVerticalLayout)
        {
            parts.Add(Doc.HardLine);
        }
        else
        {
            parts.Add(Doc.SoftLine);
        }
        parts.Add(closeToken);

        return Doc.GroupWithId(groupId, parts);
    }

    public static Doc Print(
        PrintContext ctx,
        GmlSyntaxNode list,
        string separator,
        bool allowTrailingSeparator = false,
        bool forceBreak = false,
        Doc? leadingContents = null,
        bool forceInternalBreaks = false
    )
    {
        leadingContents ??= Doc.Null;

        if (list.Children.Count == 0 && leadingContents is NullDoc)
        {
            return Doc.Null;
        }

        var parts = new List<Doc> { leadingContents };

        for (var i = 0; i < list.Children.Count; i++)
        {
            var child = list.Children[i];

            parts.Add(child.Print(ctx));

            if (i != list.Children.Count - 1)
            {
                parts.Add(separator);

                if (forceInternalBreaks)
                {
                    parts.Add(Doc.HardLine);
                }
                else
                {
                    parts.Add(forceBreak ? Doc.HardLine : Doc.Line);
                }
            }
            else if (allowTrailingSeparator)
            {
                parts.Add(Doc.IfBreak(separator, Doc.Null));
            }
        }

        return Doc.Concat(parts);
    }
}