using CheckedException.Core;
using System;

namespace CheckedException.Test.NetCore
{
    public interface IInterfaceBased
    {
        [ThrowsException(typeof(ArrayTypeMismatchException))]
        void MethodWithoutReturnValue();

        [ThrowsException(typeof(System.ArgumentOutOfRangeException))]
        int MethodWithReturnValue();
    }
}
