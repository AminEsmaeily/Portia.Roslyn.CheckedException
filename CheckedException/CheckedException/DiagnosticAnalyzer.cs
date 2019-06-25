using CheckedException.Core;
using CheckedException.Infrastructure;
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
                var defaultSeverity = Core.DiagnosticSeverity.Error;
                var attribs = GetAllAttributes(context.SemanticModel, (InvocationExpressionSyntax)context.Node, ref defaultSeverity);

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

        private static void CheckExceptionHandling(AttributeInfo throwExceptionAttrib, SyntaxNodeAnalysisContext context)
        {
            var attributeArgument = "";
            
            foreach (var item in throwExceptionAttrib.AttributeData.ConstructorArguments)
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

            Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, (Microsoft.CodeAnalysis.DiagnosticSeverity)throwExceptionAttrib.Severity, isEnabledByDefault: true, description: Description);
            Diagnostic diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation(), attributeArgument);
            context.ReportDiagnostic(diagnostic);
            diagnosticReported = true;
        }

        public static List<AttributeInfo> GetAllAttributes(SemanticModel semanticModel, InvocationExpressionSyntax method, ref Core.DiagnosticSeverity defaultSeverity)
        {
            var info = semanticModel.GetSymbolInfo(method).Symbol;            
            if (info == null)
                return new List<AttributeInfo>();

            var methodAttributes = info.GetAttributes().Where(f => f.AttributeClass.MetadataName.Equals(typeof(ThrowsExceptionAttribute).Name))
                .Select(f => new
                {
                    Attribute = f,
                    Order = 1
                }).Union(info.ContainingSymbol.GetAttributes().Where(f => f.AttributeClass.MetadataName.Equals(typeof(ThrowsExceptionAttribute).Name))
                .Select(f => new
                {
                    Attribute = f,
                    Order = 2
                }));

            var result = new List<AttributeInfo>();
            foreach(var attr in methodAttributes)
            {
                var exceptionType = attr.Attribute.ConstructorArguments.FirstOrDefault(f => f.Type.TypeKind == TypeKind.Class);

                if (exceptionType.Type == null || 
                    result.Any(f => f.AttributeData.ConstructorArguments.FirstOrDefault(g => g.Type.TypeKind == TypeKind.Class).Value == exceptionType.Value))
                    continue;

                var severities = methodAttributes.Where(f => f.Attribute.ConstructorArguments.Any(g => g.Type.TypeKind == TypeKind.Class && g.Value == exceptionType.Value) &&
                                                             f.Attribute.ConstructorArguments.Any(x => x.Type.TypeKind == TypeKind.Enum))
                                                 .OrderBy(f => f.Order);
                Core.DiagnosticSeverity severity = severities.Any() ?
                    (Core.DiagnosticSeverity)severities.First().Attribute.ConstructorArguments.FirstOrDefault(f => f.Type.TypeKind == TypeKind.Enum).Value :
                    Core.DiagnosticSeverity.Error;

                result.Add(new AttributeInfo
                {
                    AttributeData = attr.Attribute,
                    Severity = severity
                });
            }
            return result;
        }
    }
}
