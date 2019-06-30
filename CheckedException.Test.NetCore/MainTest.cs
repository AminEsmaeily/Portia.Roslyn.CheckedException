using CheckedException.Core;
using System;

namespace CheckedException.Test.NetCore
{
    public class MainTest
    {
        public void MainMethod()
        {
            //// Local class
            MethodWithoutReturnValue(); // Expression Statement

            Console.WriteLine(MethodWithReturnValue()); // Expression Statement

            var value1 = MethodWithReturnValue(); // Local Declaration Statement

            if (MethodWithReturnValue() == 10) // If Statement
            {
            }

            while(MethodWithReturnValue() == 5) // While Statement
            { }
            //////////////////////

            //// Direct Class
            var directClass = new DirectClass();

            directClass.MethodWithoutReturnValue();

            directClass.MethodWithReturnValue();
            //////////////////////

            // Interface based
            IInterfaceBased interfaceBased = new InterfaceBased();

            interfaceBased.MethodWithoutReturnValue();

            interfaceBased.MethodWithReturnValue();
            //////////////////////

            // Abstract based
            AAbstractBased abstractBased = new AbstractBased();
            abstractBased.MethodWithoutReturnValue();

            abstractBased.MethodWithReturnValue();

            abstractBased.MethodToHide();
            //////////////////////
        }

        [ThrowsException(typeof(InvalidOperationException))]
        private void MethodWithoutReturnValue()
        {

        }

        [ThrowsException(typeof(ArgumentException))]
        private int MethodWithReturnValue()
        {
            return 10;
        }

        [ThrowsException(typeof(FormatException), DiagnosticSeverity.Error)]
        [ThrowsException(typeof(FormatException), DiagnosticSeverity.Warning)]
        [ThrowsException(typeof(FormatException))]
        private void DuplicateAttributeUsage()
        {

        }
    }
}
