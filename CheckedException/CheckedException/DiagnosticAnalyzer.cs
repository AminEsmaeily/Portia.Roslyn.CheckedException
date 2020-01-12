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
    public class NotHandledAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Exception Handling";
        public const string DiagnosticId = "SAE001";
        
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.NotHandledAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString FormattedMessage = new LocalizableResourceString(nameof(Resources.NotHandledAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.NotHandledAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, FormattedMessage, Category, Microsoft.CodeAnalysis.DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(InvocationAnalyzer, SyntaxKind.InvocationExpression);
        }

        private static void InvocationAnalyzer(SyntaxNodeAnalysisContext context)
        {
            try
            {
                var attribs = GetAllAttributes(context.SemanticModel, (InvocationExpressionSyntax)context.Node);

                foreach (var attrib in attribs)
                {
                    CheckExceptionHandling(attrib, context);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private static void CheckExceptionHandling(AttributeInfo throwExceptionAttrib, SyntaxNodeAnalysisContext context)
        {
            if (!throwExceptionAttrib.AttributeData.ConstructorArguments.Any())
                return;

            var attributeArgument = throwExceptionAttrib.AttributeData.ConstructorArguments.First().Value.ToString();

            MethodDeclarationSyntax callerMethod = context.Node.Parent.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            ClassDeclarationSyntax callerClass = context.Node.Parent.FirstAncestorOrSelf<ClassDeclarationSyntax>();

            if (callerMethod == null || callerClass == null)
                return;

            var callerClassAndMethodsAttributes = callerMethod.AttributeLists
                .Union(callerClass.AttributeLists)
                .ToList();

            var detectedAttributes = from attributeList in callerClassAndMethodsAttributes
                                     let info = RedundantAnalyzer.GetAttributeParameterType(context, attributeList)
                                     where info != null && info.ToString().Equals(attributeArgument)
                                     select attributeList;

            if (detectedAttributes.Any())
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

            Rule = new DiagnosticDescriptor(DiagnosticId, Title, FormattedMessage, Category, 
                (Microsoft.CodeAnalysis.DiagnosticSeverity)throwExceptionAttrib.Severity, isEnabledByDefault: true, description: Description);
            Diagnostic diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation(), attributeArgument);
            context.ReportDiagnostic(diagnostic);
        }

        public static List<AttributeInfo> GetAllAttributes(SemanticModel semanticModel, InvocationExpressionSyntax method)
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
            foreach (var attr in methodAttributes)
            {
                var exceptionType = attr.Attribute.ConstructorArguments.FirstOrDefault(f => f.Type.TypeKind == TypeKind.Class);

                if (exceptionType.Type == null ||
                    result.Any(f => f.AttributeData.ConstructorArguments.FirstOrDefault(g => g.Type.TypeKind == TypeKind.Class).Value == exceptionType.Value))
                    continue;

                var severities = methodAttributes.Where(f => f.Attribute.ConstructorArguments.Any(g => g.Type.TypeKind == TypeKind.Class && g.Value == exceptionType.Value) &&
                                                             f.Attribute.ConstructorArguments.Any(x => x.Type.TypeKind == TypeKind.Enum))
                                                 .OrderBy(f => f.Order);

                Core.DiagnosticSeverity severity = Core.DiagnosticSeverity.Error;
                if (severities.Any() &&
                    severities.First().Attribute.ConstructorArguments.Any(f => f.Type.TypeKind == TypeKind.Enum))
                {
                    var sev = severities.First().Attribute.ConstructorArguments.FirstOrDefault(f => f.Type.TypeKind == TypeKind.Enum);
                    if (sev.Value != null)
                        severity = (Core.DiagnosticSeverity)sev.Value;
                }

                result.Add(new AttributeInfo
                {
                    AttributeData = attr.Attribute,
                    Severity = severity
                });
            }
            return result;
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DuplicateAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Exception Handling";
        public const string DiagnosticId = "SAE002";
        
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.DuplicateAttributeAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString FormattedMessage = new LocalizableResourceString(nameof(Resources.DuplicateAttributeAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.DuplicateAttributeAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, FormattedMessage, Category, Microsoft.CodeAnalysis.DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(MethodDeclarationAnalyzer, SyntaxKind.MethodDeclaration);
        }

        private static void MethodDeclarationAnalyzer(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;
            var allAttributes = method.AttributeLists;
            var checkedTypes = new List<ITypeSymbol>();
            foreach(var attrib in allAttributes)
            {
                var info = RedundantAnalyzer.GetAttributeParameterType(context, attrib);
                if (info == null)
                    continue;

                bool exists = false;
                foreach (var addedAttrib in checkedTypes)
                {
                    if (addedAttrib.Name.Equals(info.Name))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    checkedTypes.Add(info);
                    continue;
                }

                Diagnostic diagnostic = Diagnostic.Create(Rule, attrib.GetLocation(), info.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RedundantAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Exception Handling";
        public const string DiagnosticId = "SAE003";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.RedundantAttributeAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString FormattedMessage = new LocalizableResourceString(nameof(Resources.RedundantAttributeAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.RedundantAttributeAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, FormattedMessage, Category, Microsoft.CodeAnalysis.DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(MethodDeclarationAnalyzer, SyntaxKind.MethodDeclaration);
        }

        private static void MethodDeclarationAnalyzer(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;
            if (method == null)
                return;

            var methodAttributes = method.AttributeLists;
            if (methodAttributes == null)
                return;

            var classDeclaration = method.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            if (classDeclaration == null)
                return;

            var classAttributes = classDeclaration.AttributeLists;
            if (classAttributes == null)
                return;

            var repeatedAttributes = from classAttributeList in classAttributes
                                     from methodAttributeList in methodAttributes
                                     let classAttributeInfo = GetAttributeParameterType(context, classAttributeList)
                                     let methodAttributeInfo = GetAttributeParameterType(context, methodAttributeList)
                                     let classAttributeSeverity = GetAttributeParameterSeverity(context, classAttributeList)
                                     let methodAttributeSeverity = GetAttributeParameterSeverity(context, methodAttributeList)
                                     where classAttributeInfo != null && methodAttributeInfo != null &&
                                        methodAttributeSeverity != null && classAttributeSeverity != null &&
                                        classAttributeInfo.ToString().Equals(methodAttributeInfo.ToString()) &&
                                        classAttributeSeverity == methodAttributeSeverity
                                     select new { methodAttributeList, methodAttributeInfo };

            foreach(var methodAttrib in repeatedAttributes)
            {
                Diagnostic diagnostic = Diagnostic.Create(Rule, methodAttrib.methodAttributeList.GetLocation(), methodAttrib.methodAttributeInfo.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }

        public static ITypeSymbol GetAttributeParameterType(SyntaxNodeAnalysisContext context, AttributeListSyntax attrib)
        {
            if (attrib.ChildNodes().Count() == 0)
                return null;

            IdentifierNameSyntax attributeType = null;
            if (attrib.ChildNodes().First().ChildNodes().FirstOrDefault() is QualifiedNameSyntax)
                attributeType = attrib.ChildNodes().First().ChildNodes().FirstOrDefault().DescendantNodes().OfType<IdentifierNameSyntax>().LastOrDefault();
            else
                attributeType = attrib.ChildNodes().First().DescendantNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();

            if (attributeType == null)
                return null;

            var attributeInfo = context.SemanticModel.GetTypeInfo(attributeType);
            if (attributeInfo.Type == null)
                return null;

            if (attributeInfo.Type.ToString() != typeof(ThrowsExceptionAttribute).ToString())
                return null;

            var typeOf = attrib.Attributes.First().ArgumentList.DescendantNodes().OfType<TypeOfExpressionSyntax>().FirstOrDefault();
            if (typeOf == null)
                return null;

            IdentifierNameSyntax exceptionType = null;
            var qualifiedName = typeOf.DescendantNodes().OfType<QualifiedNameSyntax>().FirstOrDefault();
            if (qualifiedName == null)
                exceptionType = typeOf.DescendantNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();
            else
                exceptionType = qualifiedName.DescendantNodes().OfType<IdentifierNameSyntax>().LastOrDefault();

            if (exceptionType == null)
                return null;

            var info = context.SemanticModel.GetTypeInfo(exceptionType);
            return info.Type;
        }

        public static CheckedException.Core.DiagnosticSeverity? GetAttributeParameterSeverity(SyntaxNodeAnalysisContext context, AttributeListSyntax attrib)
        {
            if (attrib.ChildNodes().Count() == 0)
                return null;

            IdentifierNameSyntax attributeType = null;
            if (attrib.ChildNodes().First().ChildNodes().FirstOrDefault() is QualifiedNameSyntax)
                attributeType = attrib.ChildNodes().First().ChildNodes().FirstOrDefault().DescendantNodes().OfType<IdentifierNameSyntax>().LastOrDefault();
            else
                attributeType = attrib.ChildNodes().First().DescendantNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();

            if (attributeType == null)
                return null;

            var attributeInfo = context.SemanticModel.GetTypeInfo(attributeType);
            if (attributeInfo.Type == null)
                return null;

            if (attributeInfo.Type.ToString() != typeof(ThrowsExceptionAttribute).ToString())
                return null;

            var severity = attrib.Attributes.First().ArgumentList.DescendantNodes().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
            if (severity == null)
                return Core.DiagnosticSeverity.Error;

            var typeInfo = context.SemanticModel.GetTypeInfo(severity.Expression);
            if (typeInfo.Type == null || typeInfo.Type.ToString() != typeof(Core.DiagnosticSeverity).ToString())
                return null;

            Core.DiagnosticSeverity? result = null;
            foreach (var sev in Enum.GetValues(typeof(Core.DiagnosticSeverity)))
            {
                if (((Core.DiagnosticSeverity)sev).ToString().Equals(severity.Name.Identifier.Text))
                {
                    result = (Core.DiagnosticSeverity)sev;
                    break;
                }
            }

            return result;
        }
    }
}
