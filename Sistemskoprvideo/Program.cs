using System;

namespace Sistemskoprvideo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ImageServer server = new ImageServer(
                port: 5050,
                workerCount: 4,
                cacheExpirationSeconds: 60
            );

            server.Start();

            Console.WriteLine();
            Console.WriteLine("Server radi.");
            Console.WriteLine("Primer poziva: http://localhost:5050/test.jpg");
            Console.WriteLine("Pritisni ENTER za zaustavljanje servera...");
            Console.ReadLine();

            server.Stop();
        }
    }
}