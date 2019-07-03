using CheckedException.Core;
using System;

namespace CheckedException.Test.NetCore
{
    [ThrowsException(typeof(InvalidOperationException))]
    public class MainTest
    {
        [CheckedException.Core.ThrowsException(typeof(System.IO.FileNotFoundException))]
        public void MainMethod()
        {
            //// Local class
            MethodWithoutReturnValue(); // Should be ommitted, because of the class attribute

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

            directClass.MethodWithoutReturnValue(); // Should be ommitted, because of the method attribute

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

        [ThrowsException(typeof(InvalidOperationException))] // Should show the SAE003 information
        private void MethodWithoutReturnValue()
        { 

        }

        [ThrowsException(typeof(ArgumentException), CheckedException.Core.DiagnosticSeverity.Warning)]
        private int MethodWithReturnValue()
        {
            return 10;
        }

        [ThrowsException(typeof(FormatException), DiagnosticSeverity.Error)]
        [ThrowsException(typeof(System.FormatException), DiagnosticSeverity.Warning)]
        [CheckedException.Core.ThrowsException(typeof(FormatException))]
        private void DuplicateAttributeUsage()
        {

        }
    }
}
