using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SmartDb.Sample
{
    /// <summary>
    /// 一个基础实体必须满足一下条件:
    /// 1 必须用KeyAttribute标记主键(可以标记多个组成组合键)
    /// 2 要托管维护的属性必须是virtual
    /// 3 通过SmartDbSet<T>托管实体
    /// </summary>
    public class Student
    {
        [Key]
        public virtual string Id { get; set; }
        public virtual string SchoolId { get; set; }
        public virtual string Name { get; set; }
        public virtual int Age { get; set; }
    }
}
