using System;

namespace CheckedException.Base
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
