using CheckedException.Core;
using System;

namespace CheckedException.Test.NetCore
{
    public abstract class AAbstractBased
    {
        [ThrowsException(typeof(ArrayTypeMismatchException))]
        public abstract void MethodWithoutReturnValue();

        [ThrowsException(typeof(System.ArgumentOutOfRangeException))]
        public virtual int MethodWithReturnValue()
        {
            // Do something
            return 0;
        }

        [ThrowsException(typeof(DllNotFoundException))]
        public virtual void MethodToHide()
        {

        }
    }
}
