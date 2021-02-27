using System;

namespace P2Loader
{
    class Program
    {
        static string FileName;

        static void Main(string[] args)
        {

            string Port = args[0];
            FileName = args[1];

            if (Port.StartsWith("com", StringComparison.OrdinalIgnoreCase))
                new Port(Port, FileName);
            else
                new WiFi(Port, FileName);
        }

    }


}
