using System;
using System.Collections.Generic;
using System.Text;

namespace SmartDb
{
    public class SmartDbExecuteException : Exception
    {
        public string SQL { get; private set; }
        public SmartDbExecuteException(string sql, string reason)
            : base(reason)
        {
            this.SQL = sql;
        }
    }
}
