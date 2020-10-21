using System;
using System.Threading;

namespace DotNetCoreIoT
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to .Net Core IoT on WBZ Mini!");
            Console.WriteLine("Staring...");            
            IoTController.Instance.Start();

            Console.WriteLine("Running...");

            while(IoTController.Instance.Started){
                Thread.Sleep(1000);
            }

            Console.WriteLine("Completed...");

        }
    }
}
