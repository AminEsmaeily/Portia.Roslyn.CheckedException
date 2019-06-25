using Microsoft.CodeAnalysis;

namespace CheckedException.Infrastructure
{
    public class AttributeInfo
    {
        public AttributeData AttributeData { get; set; }

        public Core.DiagnosticSeverity Severity { get; set; } = Core.DiagnosticSeverity.Error;
    }
}
