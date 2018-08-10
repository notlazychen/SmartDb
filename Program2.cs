using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace SmartDb
{
    class Program
    {
        static void Main(string[] args)
        {
            var command = SmartDbEntityAgentFactory.Of<Command>();
            //var command = new Command2();
            //command.Execute();
            //command.Hello("China");
            command.Name = "China";
            //command.Pass = "AAAA";
            command.Number = 11;
            Console.WriteLine("Hi, Dennis, great, we got the interceptor works.");
            Console.ReadLine();
        }
    }

    public class Command
    {
        public virtual string Name { get; set; }
        //public virtual string Pass { get; set; }
        public virtual int Number { get; set; }


        public virtual void Execute()
        {
            Console.WriteLine("Hello Kitty!");
        }

        public virtual string Hello(string w)
        {
            Console.WriteLine($"Hello {w}!");
            return w;
        }
    }

    public class CommandProxy : Command
    {
        
    }
}