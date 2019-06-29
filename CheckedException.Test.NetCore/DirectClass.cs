using CheckedException.Core;
using System;
using System.IO;

namespace CheckedException.Test.NetCore
{
    public class DirectClass
    {
        [ThrowsException(typeof(FileNotFoundException))]
        public void MethodWithoutReturnValue()
        {

        }

        [ThrowsException(typeof(ArgumentNullException))]
        public int MethodWithReturnValue()
        {
            return 10;
        }
    }
}
