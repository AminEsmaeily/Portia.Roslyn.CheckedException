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
    }
}
