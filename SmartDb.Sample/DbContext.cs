using System;
using System.Collections.Generic;
using System.Text;

namespace SmartDb.Sample
{
    public class DbContext
    {
        public DbContext()
        {
        }

        public SmartDbSet<Student> StudentDb { get; set; }
    }
}
