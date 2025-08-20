using DataConcentrator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tester
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Tag t1 = new Tag("DI_1", "Digital Input 1", "DI01", TagType.DI);
            t1.AddProperty("scantime", 2);
            t1.AddProperty("onoffscan", true);
            //t1.AddProperty("lowlimit", 20);
            Console.WriteLine(t1);
        }
    }
}
