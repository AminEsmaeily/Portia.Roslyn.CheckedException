using CheckedException.Core;
using System;
using System.ComponentModel;

namespace CheckedException.Test.NetCore
{
    [ThrowsException(typeof(InvalidOperationException))]
    public class MainTest
    {
        [Description]
        public void MainMethod()
        {
            //// Local class
            MethodWithoutReturnValue(); // Should be ommitted, because of the class attribute

            Console.WriteLine(MethodWithReturnValue()); // Expression Statement

            var value1 = MethodWithReturnValue(); // Local Declaration Statement

            try
            {
                if (MethodWithReturnValue() == 10) // If Statement
                {
                }
            }
            catch (ArgumentException)
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

            try
            {
                interfaceBased.MethodWithReturnValue();
            }
            catch (System.ArgumentOutOfRangeException)
            {
            }            
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
        [ThrowsException(typeof(NullReferenceException))]
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
