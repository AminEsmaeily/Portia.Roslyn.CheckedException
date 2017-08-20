﻿using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using CheckedException.Base;
using System;

namespace Portia.Roslyn.CheckedException
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PortiaRoslynCheckedExceptionCodeFixProvider)), Shared]
    public class PortiaRoslynCheckedExceptionCodeFixProvider : CodeFixProvider
    {
        private const string title = "Add missed exception handler";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(PortiaRoslynCheckedExceptionAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest


            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => AddTryCatchAsync(context, c),
                    equivalenceKey: title),
                context.Diagnostics.First());
        }

        private async Task<Document> AddTryCatchAsync(CodeFixContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            SyntaxToken invocation = root.FindToken(diagnosticSpan.Start);

            InvocationExpressionSyntax completeMethod = invocation.Parent.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            var methodAttribs = completeMethod.Parent.FirstAncestorOrSelf<MethodDeclarationSyntax>().AttributeLists;

            SemanticModel sm = await document.GetSemanticModelAsync();
            var attribs = PortiaRoslynCheckedExceptionAnalyzer.GetAllAttributes(sm, completeMethod);

            var tryElement = invocation.Parent.FirstAncestorOrSelf<TryStatementSyntax>();

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
            SyntaxNode newRoot = null;

            var catches = new List<CatchClauseSyntax>();
            foreach (var attrib in attribs)
            {
                var typeName = "";
                // I used this way because of the exception described in https://github.com/dotnet/roslyn/issues/6226
                foreach (var item in attrib.ConstructorArguments)
                {
                    typeName = item.Value.ToString();
                    break;
                }

                var attribItems = from element in methodAttribs
                                  from identifier in element.DescendantNodes().OfType<IdentifierNameSyntax>()
                                  from argument in element.DescendantNodes().OfType<AttributeArgumentSyntax>()
                                  from identifier2 in argument.DescendantNodes().OfType<IdentifierNameSyntax>()
                                  let identifierType = sm.GetTypeInfo(identifier)
                                  let identifier2Type = sm.GetTypeInfo(identifier2)
                                  where identifierType.Type != null && identifierType.Type.ToString().Equals(typeof(ThrowsExceptionAttribute).FullName) &&
                                        identifier2Type.Type != null && identifier2Type.Type.ToString().Equals(typeName)
                                  select element;

                if (attribItems.Any())
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
                                    typeInfo.Type.ToString().Equals(typeName))
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

                //if (tryElement == null || !tryElement.Catches.Any(f => f.Declaration.Type is IdentifierNameSyntax && ((IdentifierNameSyntax)f.Declaration.Type).Identifier.Text.Equals(typeName)))
                if(createCatchPart)
                {
                    IdentifierNameSyntax catchTypeSyntax = SyntaxFactory.IdentifierName(typeName);
                    var catchDeclaration = SyntaxFactory.CatchDeclaration(catchTypeSyntax, new SyntaxToken());
                    var blockSyntax = SyntaxFactory.Block();
                    var catchPart = SyntaxFactory.CatchClause(catchDeclaration, null, blockSyntax);

                    catches.Add(catchPart);
                }
            }

            if (tryElement != null)
                newRoot = oldRoot.InsertNodesAfter(tryElement.Catches.Last(), catches);
            else
            {
                ExpressionStatementSyntax body = completeMethod.FirstAncestorOrSelf<ExpressionStatementSyntax>();
                var expressionIndex = body.Parent.ChildNodesAndTokens().ToList().IndexOf(body);
                var prevSyntax = (SyntaxNode)body.Parent.ChildNodesAndTokens().ToList()[expressionIndex - 1];
                BlockSyntax block = SyntaxFactory.Block(body);

                
                TryStatementSyntax trySyntax = SyntaxFactory.TryStatement(block, new SyntaxList<CatchClauseSyntax>(), null);
                trySyntax = trySyntax.AddCatches(catches.ToArray());
                
                newRoot = oldRoot.ReplaceNode(body, trySyntax);
            }

            return document.WithSyntaxRoot(newRoot);
        }        
    }
}