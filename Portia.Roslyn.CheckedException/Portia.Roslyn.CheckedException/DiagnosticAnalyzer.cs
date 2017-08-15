using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CheckedException.Base;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Portia.Roslyn.CheckedException
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PortiaRoslynCheckedExceptionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CheckedException";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Exception Handling";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        private static bool diagnosticReported = false;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(SyntaxNodeAnalyze, SyntaxKind.InvocationExpression);
        }

        private async static void SyntaxNodeAnalyze(SyntaxNodeAnalysisContext context)
        {
            try
            {
                var attribs = GetAllAttributes(context.SemanticModel, (InvocationExpressionSyntax)context.Node);

                diagnosticReported = false;
                foreach (var attrib in attribs)
                {
                     await CheckExceptionHandling(attrib, context);
                    if (diagnosticReported)
                        break;
                }
            }
            catch (Exception ex)
            {
                Debugger.Break();
            }
        }

        private async static Task CheckExceptionHandling(AttributeData throwExceptionAttrib, SyntaxNodeAnalysisContext context)
        {
            var attributeArgument = "";

            // I used this way because of the exception described in https://github.com/dotnet/roslyn/issues/6226
            foreach (var item in throwExceptionAttrib.ConstructorArguments)
            {
                attributeArgument = item.Value.ToString();
                break;
            }

            SyntaxNode parent = context.Node.Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();

            if (parent == null)
                return;

            var attribs = from attrib in ((MethodDeclarationSyntax)parent).DescendantNodes().OfType<AttributeSyntax>()
                      from throwsIdentifier in attrib.DescendantNodes().OfType<IdentifierNameSyntax>()
                      from attribArgument in attrib.DescendantNodes().OfType<AttributeArgumentSyntax>()
                      from identifier in attribArgument.DescendantNodes().OfType<IdentifierNameSyntax>()
                      let throwsType = context.SemanticModel.GetTypeInfo(throwsIdentifier)
                      let identifierType = context.SemanticModel.GetTypeInfo(identifier)
                      where throwsType.Type != null && throwsType.Type.ToString().Equals(typeof(ThrowsExceptionAttribute).FullName) &&
                            identifierType.Type != null && identifierType.Type.ToString().Equals(attributeArgument)
                      select attrib;

            if (attribs.Any())
                return;

            // Checking try catch block
            foreach(var g in context.Node.Parent.AncestorsAndSelf().OfType<TryStatementSyntax>())
            {
                foreach(var f in g.Catches)
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

            var attribs = info.GetAttributes().Where(f => f.AttributeClass.MetadataName.Equals(typeof(ThrowsExceptionAttribute).Name));

            return attribs.ToList();
        }
    }
}
