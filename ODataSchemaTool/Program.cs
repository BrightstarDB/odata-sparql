using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODataSchemaTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var parsedArgs = new Args();
            if (CommandLine.Parser.ParseArgumentsWithUsage(args, parsedArgs))
            {
                var converter = new OwlConverter(parsedArgs);
                converter.Execute();
            }
        }
    }
}
