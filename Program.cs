using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectB
{
    public class Program
    {
        // C# entry point
        public static void Main(string[] args)
        {
            try
            {
                new Bot().RunAsync().GetAwaiter().GetResult();
            }
            catch(Exception exception)
            {
                Console.Write(exception.Message);
            }
        }
    }
}
