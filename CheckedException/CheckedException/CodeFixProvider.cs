using CheckedException.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CheckedException
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NotHandledCodeFixProvider)), Shared]
    public class NotHandledCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(NotHandledAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Resources.FixByTryCatch,
                    createChangedDocument: c => AddTryCatchAsync(context, c)),
                context.Diagnostics);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Resources.FixByAnnotation,
                    createChangedDocument: f => AddAnnotationAsync(context, f)),
                context.Diagnostics);
        }

        private static async Task<Document> AddTryCatchAsync(CodeFixContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            SyntaxToken invocation = root.FindToken(diagnosticSpan.Start);

            InvocationExpressionSyntax completeMethod = await GetInvokedMethodAsync(context, cancellationToken);

            SemanticModel sm = await document.GetSemanticModelAsync();
            var calleeAttributes = NotHandledAnalyzer.GetAllAttributes(sm, completeMethod);
            var catchedAttributes = await GetCallerAttributesAsync(context, cancellationToken);
            var tryElement = invocation.Parent.FirstAncestorOrSelf<TryStatementSyntax>();

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
            SyntaxNode newRoot = null;

            var catches = new List<CatchClauseSyntax>();
            foreach (var attrib in calleeAttributes)
            {
                var skip = false;
                var typeParameter = attrib.AttributeData.ConstructorArguments.FirstOrDefault(f => f.Type.TypeKind == TypeKind.Class);
                if (typeParameter.Type == null)
                    continue;

                var exceptionName = typeParameter.Value.ToString();
                foreach (var catchedAttribute in catchedAttributes)
                {
                    var typeOfExp = catchedAttribute.DescendantNodes().OfType<TypeOfExpressionSyntax>();
                    if (typeOfExp == null || !typeOfExp.Any())
                    {
                        skip = true;
                        continue;
                    }

                    var identifier = typeOfExp.First().DescendantNodes().OfType<IdentifierNameSyntax>();
                    if (identifier == null || !identifier.Any())
                    {
                        skip = true;
                        continue;
                    }

                    var semanticType = sm.GetTypeInfo(identifier.First()).Type;
                    if (semanticType != null && exceptionName.Equals(semanticType.ToString()))
                    {
                        skip = true;
                        break;
                    }
                }

                if (skip)
                    continue;

                bool createCatchPart = tryElement == null;
                if (!createCatchPart)
                {
                    var exists = false;
                    foreach (var f in tryElement.Catches)
                    {
                        if (f.Declaration != null)
                            foreach (var k in f.Declaration.DescendantNodes().OfType<IdentifierNameSyntax>())
                            {
                                var typeInfo = sm.GetTypeInfo(k);
                                if (typeInfo.Type == null)
                                    continue;

                                if (typeInfo.Type.ToString().Equals(typeof(Exception).FullName) ||
                                    typeInfo.Type.ToString().Equals(exceptionName))
                                {
                                    exists = true;
                                    break;
                                }
                            }

                        if (exists)
                            break;
                    }

                    createCatchPart = !exists;
                }
                
                if (createCatchPart)
                {
                    IdentifierNameSyntax catchTypeSyntax = SyntaxFactory.IdentifierName(exceptionName);
                    var catchDeclaration = SyntaxFactory.CatchDeclaration(catchTypeSyntax, new SyntaxToken());
                    var blockSyntax = SyntaxFactory.Block();
                    var catchPart = SyntaxFactory.CatchClause(catchDeclaration, null, blockSyntax);

                    catches.Add(catchPart);
                }
            }

            try
            {
                if (tryElement != null)
                    newRoot = oldRoot.InsertNodesAfter(tryElement.Catches.Last(), catches);
                else
                {
                    var body = completeMethod.FirstAncestorOrSelf<StatementSyntax>();
                    var expressionIndex = body.Parent.ChildNodesAndTokens().ToList().IndexOf(body);
                    BlockSyntax block = SyntaxFactory.Block(body);

                    TryStatementSyntax trySyntax = SyntaxFactory.TryStatement(block, new SyntaxList<CatchClauseSyntax>(), null);
                    trySyntax = trySyntax.AddCatches(catches.ToArray()).NormalizeWhitespace(elasticTrivia:true);

                    newRoot = oldRoot.ReplaceNode(body, trySyntax);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return document.WithSyntaxRoot(newRoot);
        }

        private static async Task<Document> AddAnnotationAsync(CodeFixContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            SemanticModel sm = await document.GetSemanticModelAsync();
            SyntaxToken invocation = root.FindToken(diagnosticSpan.Start);
            InvocationExpressionSyntax completeMethod = await GetInvokedMethodAsync(context, cancellationToken);

            MethodDeclarationSyntax callerMethodContainer = (MethodDeclarationSyntax)invocation.Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();

            var calleeAttributes = NotHandledAnalyzer.GetAllAttributes(sm, completeMethod);
            var catchedAttributes = await GetCallerAttributesAsync(context, cancellationToken);
            var tryElement = invocation.Parent.FirstAncestorOrSelf<TryStatementSyntax>();

            var newAttributes = callerMethodContainer.AttributeLists;
            foreach(var attrib in calleeAttributes)
            {
                var skip = false;
                var typeParameter = attrib.AttributeData.ConstructorArguments.FirstOrDefault(f => f.Type.TypeKind == TypeKind.Class);
                if (typeParameter.Type == null)
                    continue;

                var exceptionName = typeParameter.Value.ToString();
                foreach (var catchedAttribute in catchedAttributes)
                {
                    var typeOfExp = catchedAttribute.DescendantNodes().OfType<TypeOfExpressionSyntax>();
                    if (typeOfExp == null || !typeOfExp.Any())
                    {
                        skip = true;
                        continue;
                    }

                    var identifier = typeOfExp.First().DescendantNodes().OfType<IdentifierNameSyntax>();
                    if (identifier == null || !identifier.Any())
                    {
                        skip = true;
                        continue;
                    }

                    var semanticType = sm.GetTypeInfo(identifier.First()).Type;
                    if (semanticType != null && exceptionName.Equals(semanticType.ToString()))
                    {
                        skip = true;
                        break;
                    }
                }

                if (!skip && tryElement != null)
                {
                    foreach (var f in tryElement.Catches)
                    {
                        if (f.Declaration != null)
                            foreach (var k in f.Declaration.DescendantNodes().OfType<IdentifierNameSyntax>())
                            {
                                var typeInfo = sm.GetTypeInfo(k);
                                if (typeInfo.Type == null)
                                    continue;

                                if (typeInfo.Type.ToString().Equals(typeof(Exception).FullName) ||
                                    typeInfo.Type.ToString().Equals(exceptionName))
                                {
                                    skip = true;
                                    break;
                                }
                            }

                        if (skip)
                            break;
                    }
                }

                if (skip)
                    continue;

                var attributeName = typeof(ThrowsExceptionAttribute).FullName.Substring(0, typeof(ThrowsExceptionAttribute).FullName.IndexOf("Attribute"));
                newAttributes = newAttributes.Add(
                    SyntaxFactory.AttributeList(
                        SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                        SyntaxFactory.Attribute(
                            SyntaxFactory.IdentifierName(attributeName)).WithArgumentList(
                            SyntaxFactory.AttributeArgumentList(
                                SyntaxFactory.SingletonSeparatedList<AttributeArgumentSyntax>(
                                SyntaxFactory.AttributeArgument(
                                    SyntaxFactory.TypeOfExpression(
                                    SyntaxFactory.IdentifierName(exceptionName)))))))));
            }

            try
            {
                return document.WithSyntaxRoot(root.ReplaceNode(callerMethodContainer, callerMethodContainer.WithAttributeLists(newAttributes)));
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
                return document.WithSyntaxRoot(root);
            }
        }

        private static async Task<IEnumerable<AttributeListSyntax>> GetCallerAttributesAsync(CodeFixContext context, CancellationToken cancellationToken)
        {
            SemanticModel sm = await context.Document.GetSemanticModelAsync();
            var root = await context.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            SyntaxToken invocation = root.FindToken(diagnosticSpan.Start);

            MethodDeclarationSyntax callerMethod = (MethodDeclarationSyntax)invocation.Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            SyntaxNode callerClass = invocation.Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            try
            {
                var result = ((MethodDeclarationSyntax)callerMethod).AttributeLists
                    .Union(((ClassDeclarationSyntax)callerClass).AttributeLists)
                    .Where(f => (f.DescendantNodes().OfType<QualifiedNameSyntax>().Any() &&
                                sm.GetTypeInfo(f.DescendantNodes().OfType<QualifiedNameSyntax>().FirstOrDefault()).Type != null &&
                                sm.GetTypeInfo(f.DescendantNodes().OfType<QualifiedNameSyntax>().FirstOrDefault()
                                                .DescendantNodes().OfType<IdentifierNameSyntax>().Last()).Type.ToString().Equals(typeof(ThrowsExceptionAttribute).FullName)) ||
                                (f.DescendantNodes().OfType<IdentifierNameSyntax>().Any() &&
                                 sm.GetTypeInfo(f.DescendantNodes().OfType<IdentifierNameSyntax>().First()).Type != null &&
                                 sm.GetTypeInfo(f.DescendantNodes().OfType<IdentifierNameSyntax>().First()).Type.ToString().Equals(typeof(ThrowsExceptionAttribute).FullName))).ToList();
                return result;

            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
                throw ex;
            }
        }

        private static async Task<InvocationExpressionSyntax> GetInvokedMethodAsync(CodeFixContext context, CancellationToken cancellationToken)
        {
            var root = await context.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            SyntaxToken invocation = root.FindToken(diagnosticSpan.Start);
            return invocation.Parent.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        }
    }

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DuplicateCodeFixProvider)), Shared]
    public class DuplicateCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DuplicateAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Resources.RemoveDuplicateAttribute,
                    createChangedDocument: f => RemoveDuplicateAttribute(context, f)),
                context.Diagnostics);
        }

        private static async Task<Document> RemoveDuplicateAttribute(CodeFixContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            SyntaxToken invocation = root.FindToken(diagnostic.Location.SourceSpan.Start);

            AttributeListSyntax attribute = invocation.Parent.AncestorsAndSelf().OfType<AttributeListSyntax>().FirstOrDefault();
            root = root.RemoveNode(attribute, SyntaxRemoveOptions.KeepNoTrivia);

            return document.WithSyntaxRoot(root);
        }
    }

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DuplicateCodeFixProvider)), Shared]
    public class RedundantCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RedundantAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Resources.RemoveRedundantAttribute,
                    createChangedDocument: f => RemoveRedundantAttribute(context, f)),
                context.Diagnostics);
        }

        private static async Task<Document> RemoveRedundantAttribute(CodeFixContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            SyntaxToken invocation = root.FindToken(diagnostic.Location.SourceSpan.Start);

            AttributeListSyntax attribute = invocation.Parent.AncestorsAndSelf().OfType<AttributeListSyntax>().FirstOrDefault();
            root = root.RemoveNode(attribute, SyntaxRemoveOptions.AddElasticMarker);

            return document.WithSyntaxRoot(root);
        }
    }
}