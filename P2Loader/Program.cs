using System;
using System.IO;

namespace P2Loader
{
    class Program
    {
        public static bool Patch = false;
        static string FileName;
        public static byte[] elf = { 127, 69, 76, 70 };
        public static string PropHex = "> Prop_Hex 0 0 0 0";
        public static string PropTxt = "> Prop_Txt 0 0 0 0 ";
        public static string PropClk = "> Prop_Clk 0 0 0 0 ";
        public static string PropChk = "> Prop_Chk 0 0 0 0  ";
        public static int frequency = 200;
        public static int mode = 0x14c00f8;
        public static int baud = 230400;

        static void Main(string[] args)
        {
            int i = 0;

            if (args.Length < 2)
            {
                Console.WriteLine("Wrong number of parameters");
                Console.WriteLine("p2loader [patch cpu baud] port(wifi) filename");
                return;
            }

            if (args[i].Equals("patch", StringComparison.OrdinalIgnoreCase))
            {
                Patch = true;
          
                frequency = int.Parse(args[++i]);
                if (frequency > 500)
                    return;
                mode = mode | (frequency - 1) << 8;
                baud = int.Parse(args[++i]);
            }

            string Port = args[++i];
            FileName = args[++i];

            if (!File.Exists(FileName))
            {
                Console.WriteLine("File {0} Not Found!", FileName);
                return;
            }

            if (Port.StartsWith("com", StringComparison.OrdinalIgnoreCase))
                new Port(Port, FileName);
            else
                new WiFi(Port, FileName);
        }

    }

    class ValueConvert
    {
        public const uint _clkfreq = 0x14;
        public const uint _clkmode = 0x18;
        public const uint _baudrate = 0x1c;

        public static Int32 value;
        public static byte[] data = new byte[4];

        public static void ValueCopy(byte[] b, uint p, Int32 v)
        {
            value = v;
            data[0] = (byte)value;
            data[1] = (byte)(value >> 8);
            data[2] = (byte)(value >> 16);
            data[3] = (byte)(value >> 24);

            b[p++] = data[0];
            b[p++] = data[1];
            b[p++] = data[2];
            b[p] = data[3];
        }
    }

}
