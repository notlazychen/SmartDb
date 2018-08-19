# SmartDb

一直希望能够在服务器上只关心业务逻辑, 不用写SQL的增删改查. 对数据库中的数据, 只要我读取出来就可以只操作内存中的结构就可以了, 多么美好. 不想去关心数据修改之后要如何保存. 于是我封装了这个类库.
比如有一个类型Student

```C#
class Student
{
  public int Id{get;set;}
  public string Name{get;set;}
}
```

只需要:

1 按约定修改成符合托管条件的实体类型:
```C#
  class Student
  {
    [Key]//必须定义主键(可以标记多个形成组合键)
    public int Id{get;set;}
    public virtual string Name{get;set;}//属性必须virtual才能被托管
  }
```

2 创建razor, 创建dbset, 并将student送入托管
```C#  
  var razor = new Razor();    
  razor.StartWork();
  var personset = razor.CreateDbSet<Student>(); 
  var student = new Student{Id = 1, Name="xxx"};
  personset.Insert(ref student);
  student.Name = "xxx2";
  //...  
  //do sth...
  
  razor.StopWork();//结束写入线程
```

之后我就可以像使用list一样操作personset进行add/remove, 然后添加进set中的person实例也不需要担心其中属性的修改. 一切都会被托管持久化到数据库中.

如同无须调用EF的DbContext.SaveChange()就能save changes一样简单.

详见工程内Test目录
