using CheckedException.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace CheckedException
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CheckedExceptionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SAE001";
        
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Exception Handling";

        private static bool diagnosticReported = false;

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, Microsoft.CodeAnalysis.DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(SyntaxNodeAnalyze, SyntaxKind.InvocationExpression);
        }

        private static void SyntaxNodeAnalyze(SyntaxNodeAnalysisContext context)
        {
            try
            {
                var attribs = GetAllAttributes(context.SemanticModel, (InvocationExpressionSyntax)context.Node);

                diagnosticReported = false;
                foreach (var attrib in attribs)
                {
                    CheckExceptionHandling(attrib, context);
                    if (diagnosticReported)
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private static void CheckExceptionHandling(AttributeData throwExceptionAttrib, SyntaxNodeAnalysisContext context)
        {
            var attributeArgument = "";
            
            foreach (var item in throwExceptionAttrib.ConstructorArguments)
            {
                attributeArgument = item.Value.ToString();
                break;
            }

            SyntaxNode callerMethod = context.Node.Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            SyntaxNode callerClass = context.Node.Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();

            if (callerMethod == null || callerClass == null)
                return;

            var allAttributes = ((MethodDeclarationSyntax)callerMethod).DescendantNodes().OfType<AttributeSyntax>()
                .Union(((ClassDeclarationSyntax)callerClass).DescendantNodes().OfType<AttributeSyntax>())
                .ToList();

            foreach(var attribute in allAttributes)
            {
                var attributeIdentifier = attribute.DescendantNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();
                if (attributeIdentifier == null)
                    continue;
                var identifierType = context.SemanticModel.GetTypeInfo((IdentifierNameSyntax)attributeIdentifier);
                if (identifierType.Type == null || !identifierType.Type.ToString().Equals(typeof(ThrowsExceptionAttribute).FullName))
                    continue;

                var attributeArgs = attribute.DescendantNodes().OfType<AttributeArgumentListSyntax>();
                if (!attributeArgs.Any()) // It consist of all exception types
                    return;

                foreach (var argument in attributeArgs)
                {
                    var typeOf = argument.DescendantNodes().OfType<TypeOfExpressionSyntax>();
                    if (typeOf != null && typeOf.Any())
                    {
                        var identifiers = typeOf.First().DescendantNodes().OfType<IdentifierNameSyntax>();
                        if (identifiers != null && identifiers.Any())
                        {
                            var semanticType = context.SemanticModel.GetTypeInfo(identifiers.First()).Type;
                            if (semanticType != null && semanticType.ToString().Equals(attributeArgument))
                                return;
                        }
                    }
                    else return;
                }
            }

            var callerMethodAttribs = from attrib in ((MethodDeclarationSyntax)callerMethod).DescendantNodes().OfType<AttributeSyntax>()
                          from throwsIdentifier in attrib.DescendantNodes().OfType<IdentifierNameSyntax>()
                          from attribArgument in attrib.DescendantNodes().OfType<AttributeArgumentSyntax>()
                          from identifier in attribArgument.DescendantNodes().OfType<IdentifierNameSyntax>()
                          let throwsType = context.SemanticModel.GetTypeInfo(throwsIdentifier)
                          let identifierType = context.SemanticModel.GetTypeInfo(identifier)
                          where throwsType.Type != null && throwsType.Type.ToString().Equals(typeof(ThrowsExceptionAttribute).FullName) &&
                                identifierType.Type != null && identifierType.Type.ToString().Equals(attributeArgument)
                          select attrib;

            if (callerMethodAttribs.Any())
                return;

            // Checking try catch block
            foreach (var g in context.Node.Parent.AncestorsAndSelf().OfType<TryStatementSyntax>())
            {
                foreach (var f in g.Catches)
                {
                    if (f.Declaration == null)
                        return;

                    foreach (var k in f.Declaration.DescendantNodes().OfType<IdentifierNameSyntax>())
                    {
                        var typeInfo = context.SemanticModel.GetTypeInfo(k);
                        if (typeInfo.Type == null)
                            continue;

                        if (typeInfo.Type.ToString().Equals(typeof(Exception).FullName) ||
                            typeInfo.Type.ToString().Equals(attributeArgument))
                            return;
                    }
                }
            }
            ////////////////////////////////////////////////////////

            Diagnostic diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation(), attributeArgument);
            context.ReportDiagnostic(diagnostic);
            diagnosticReported = true;
        }

        public static List<AttributeData> GetAllAttributes(SemanticModel semanticModel, InvocationExpressionSyntax method)
        {
            var info = semanticModel.GetSymbolInfo(method).Symbol;
            if (info == null)
                return new List<AttributeData>();
            
            return info.GetAttributes().Where(f => f.AttributeClass.MetadataName.Equals(typeof(ThrowsExceptionAttribute).Name)).ToList();
        }
    }
}
