namespace Scripts;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using ScriptsBase.Translation;
using ScriptsBase.Utilities;

/// <summary>
///   Extracts C# translation method calls for translation template generation
/// </summary>
public class CSharpTranslationExtractor : TranslationExtractorBase
{
    public CSharpTranslationExtractor(IReadOnlyCollection<string> callsToLookFor) : base(".cs")
    {
        CallsToLookFor = callsToLookFor;
    }

    private IReadOnlyCollection<string> CallsToLookFor { get; }

    public override async IAsyncEnumerable<ExtractedTranslation> Handle(string path,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var fileContent = await File.ReadAllBytesAsync(path, cancellationToken);

        var sourceText = SourceText.From(fileContent, fileContent.Length, Encoding.UTF8);

        // We define the debug symbol here as some text is present in the debug sections and those are otherwise
        // skipped from the syntax tree
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceText,
            new CSharpParseOptions(preprocessorSymbols: new[] { "DEBUG" }), path, cancellationToken);

        var walker = new TranslationCollector(sourceText, path, CallsToLookFor);

        var root = syntaxTree.GetCompilationUnitRoot(cancellationToken);
        walker.Visit(root);

        foreach (var result in walker.ExtractedTranslations)
        {
            yield return result;

            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private class TranslationCollector : CSharpSyntaxWalker
    {
        public readonly List<ExtractedTranslation> ExtractedTranslations = new();
        private readonly SourceText sourceText;
        private readonly string path;
        private readonly IReadOnlyCollection<string> callsToLookFor;

        private bool grabNext;

        public TranslationCollector(SourceText sourceText, string path, IReadOnlyCollection<string> callsToLookFor)
        {
            this.sourceText = sourceText;
            this.path = path;
            this.callsToLookFor = callsToLookFor;
        }

        public override void VisitArgumentList(ArgumentListSyntax node)
        {
            base.VisitArgumentList(node);

            if (!grabNext)
                return;

            grabNext = false;
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            base.VisitInvocationExpression(node);

            // TODO: if we want full semantic analysis here, we need to actually compile the given file
            // https://stackoverflow.com/questions/55118805/extract-called-method-information-using-roslyn
            var methodNameInCallText = node.Expression.ToString();

            if (callsToLookFor.All(c => c != methodNameInCallText))
                return;

            HandlePotentialTranslationCall(node.ArgumentList);
        }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            base.VisitObjectCreationExpression(node);

            var classType = node.Type.ToString();

            if (callsToLookFor.All(c => c != classType))
                return;

            if (node.ArgumentList == null)
                return;

            HandlePotentialTranslationCall(node.ArgumentList);
        }

        public override void VisitAttribute(AttributeSyntax node)
        {
            base.VisitAttribute(node);

            var attributeName = node.Name.ToString();

            if (callsToLookFor.All(c => c != attributeName))
                return;

            if (node.ArgumentList == null)
                return;

            HandlePotentialTranslationCall(node.ArgumentList);
        }

        private void HandlePotentialTranslationCall(BaseArgumentListSyntax arguments)
        {
            if (arguments.Arguments.Count < 1)
                return;

            var firstArgument = arguments.Arguments[0];

            if (firstArgument.Expression is not LiteralExpressionSyntax literal)
                return;

            HandleArgumentCallValue(literal, firstArgument.Span);
        }

        private void HandlePotentialTranslationCall(AttributeArgumentListSyntax arguments)
        {
            if (arguments.Arguments.Count < 1)
                return;

            var firstArgument = arguments.Arguments[0];

            if (firstArgument.Expression is not LiteralExpressionSyntax literal)
                return;

            HandleArgumentCallValue(literal, firstArgument.Span);
        }

        private void HandleArgumentCallValue(LiteralExpressionSyntax literal, TextSpan span)
        {
            if (!literal.IsKind(SyntaxKind.StringLiteralExpression))
                return;

            // Found a valid call
            var text = literal.Token.Text;

            var line = sourceText.Lines.GetLinePosition(span.Start).Line + 1;

            if (!text.StartsWith('\"'))
            {
                // We probably found something we shouldn't
                ColourConsole.WriteWarningLine($"Found something almost like a translation at {path}:{line}");
                return;
            }

            text = text.Substring(1, text.Length - 2);

            ExtractedTranslations.Add(new ExtractedTranslation(text, path, line));
        }
    }
}
