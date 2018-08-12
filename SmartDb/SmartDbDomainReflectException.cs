using System;
using System.Collections.Generic;
using System.Text;

namespace SmartDb
{
    public class SmartDbDomainReflectException:Exception
    {
        public Type DomainType { get; private set; }
        public SmartDbDomainReflectException(Type type, string reason)
            : base(reason)
        {
            this.DomainType = type;
        }
    }
}
