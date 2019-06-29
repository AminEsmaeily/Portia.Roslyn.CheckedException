using System;

namespace CheckedException.Test.NetCore
{
    public class InterfaceBased : IInterfaceBased
    {
        public void MethodWithoutReturnValue()
        {
            throw new NotImplementedException();
        }

        public int MethodWithReturnValue()
        {
            throw new NotImplementedException();
        }
    }
}
