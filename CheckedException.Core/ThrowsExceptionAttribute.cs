﻿using System;

namespace CheckedException.Core
{
    [AttributeUsage(AttributeTargets.Method|AttributeTargets.Class|AttributeTargets.Interface, AllowMultiple = true)]
    public class ThrowsExceptionAttribute : Attribute
    {
        public ThrowsExceptionAttribute(Type exceptionType)
        {
            this.ExceptionType = exceptionType;
        }

        public ThrowsExceptionAttribute(Type exceptionType, DiagnosticSeverity severity)
        {
            this.ExceptionType = exceptionType;
            this.Severity = severity;
        }

        public Type ExceptionType { get; private set; }

        public DiagnosticSeverity Severity { get; private set; }
    }
}
