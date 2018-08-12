using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SmartDb.Sample
{
    public class Program
    {
        public static Razor razor;
        public static void Main()
        {
            //string connstr = "Server=localhost;database=test;uid=root;pwd=root;SslMode=None;charset=utf8;pooling=false";
            string connstr = "Server=140.143.28.95;database=db-test;uid=chenrong;pwd=abcd1234;SslMode=None;charset=utf8;pooling=false";
            razor = new Razor(connstr, 2000);
            razor.SqlCommandExecuted += Razor_SqlCommandExecuted;
            razor.UnHandledSqlCommandExecuteException += Razor_UnHandledSqlCommandExecuteException;
            razor.StartWork();
            TestRazor();
            Console.ReadLine();
        }

        public static void TestRazor()
        {
            Console.WriteLine(DateTime.Now);
            var dbset = razor.QueryDbSet<Student>("select * from Student");//创建托管容器
            for (int i = 0; i < 1000; i++)
            {
                Student s = new Student
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = $"s{i}",
                    Age = i,
                    SchoolId = "0002",
                };
                dbset.Add(s);//将数据放入容器进行托管, 测试是否插入保存到数据库
            }

            foreach (var s in dbset)
            {
                s.Name = $"upda{s.Name}";//测试托管数据属性修改能否自动保存到数据库中
            }
            //dbset.Clear();//清空数据, 测试是否清空数据库
            Console.WriteLine($"{DateTime.Now}: OVER");
        }

        private static void Razor_UnHandledSqlCommandExecuteException(object sender, Exception e)
        {
            Console.WriteLine("触发异常: {0}", e.Message);
        }

        private static void Razor_SqlCommandExecuted(int exeLines, long exeMs, int remainLines)
        {
            Console.WriteLine($"{DateTime.Now}: 执行{exeLines}行, 耗时{exeMs}毫秒, 剩余{remainLines}行, 平均每秒执行{(double)exeLines/exeMs * 1000}行");
        }
    }
}
