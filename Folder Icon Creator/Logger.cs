using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Folder_Icon_Creator
{
    public static class Logger
    {
        public static StringBuilder LogString = new StringBuilder();
        public static void Out(string str)
        {
            Console.WriteLine(str);
            LogString.Append(str).Append(Environment.NewLine);
        }

        public static void Out(string str, params object[] args)
        {
            string ret = String.Format(str, args);
            Console.WriteLine(ret);
            LogString.Append(ret).Append(Environment.NewLine);
        }
    }
}
