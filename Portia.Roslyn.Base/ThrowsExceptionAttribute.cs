using System;

namespace Portia.Roslyn.Base
{
    [AttributeUsage(validOn: AttributeTargets.Method, AllowMultiple = true)]
    public class ThrowsExceptionAttribute : Attribute
    {
        public ThrowsExceptionAttribute(Type exceptionType)
        {
            ExceptionType = exceptionType;
        }

        private Type ExceptionType { get; set; }
    }
}
