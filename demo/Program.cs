using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeetestCrack;
namespace demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var gk = new Geek("421b84eeaee7b2aed4c0ec5706d8b571", "http://www.geetest.com/");
            var jsonObj = gk.GetValidate();
            if (jsonObj != null)
                Console.WriteLine(jsonObj["validate"]);
            else
                Console.WriteLine("*** failed ***");
        }
    }
}
