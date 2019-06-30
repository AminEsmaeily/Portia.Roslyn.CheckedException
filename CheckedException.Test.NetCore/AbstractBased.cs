using System;

namespace CheckedException.Test.NetCore
{
    public class AbstractBased : AAbstractBased
    {
        public override void MethodWithoutReturnValue()
        {
            throw new NotImplementedException();
        }

        public override int MethodWithReturnValue()
        {
            return 0;
        }

        public void MethodToHide()
        {

        }
    }
}
