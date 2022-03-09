using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;

namespace P2Loader
{
    class Port
    {
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

        static SerialPort port;
        static byte[] Data = new byte[4096];
        byte[] program;
        ELF e;

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern uint GetLastError();

        public Port(string portname, string FileName)
        {
            string s;
            int i;

            program = File.ReadAllBytes(FileName);
            for (i = 0;i< Program.elf.Length;i++)
            {
                if (program[i] != Program.elf[i])
                    break;
            }
            
            if (i == Program.elf.Length)
            {
                e = new ELF(program);

                program = e.getProgram(program);
                //File.WriteAllBytes("file.bin", program);
            }

            // patch frequency and baud rate
            if (Program.Patch)
            {
                ValueConvert.ValueCopy(program, ValueConvert._clkfreq, Program.frequency * 1000000);
                ValueConvert.ValueCopy(program, ValueConvert._clkmode, Program.mode);
                ValueConvert.ValueCopy(program, ValueConvert._baudrate, Program.baud);
            }

            System.Console.WriteLine(string.Format("Loading {0} bytes.", program.Length));

            port = new SerialPort(portname, 2000000);
            port.Open();

            ResetProp();

            if (port.BytesToRead > 0)
                port.Read(Data, 0, port.BytesToRead);

            /*
             * Check if Propeller is there
             */
            port.WriteLine(Program.PropChk);
            Thread.Sleep(100);
            if (port.BytesToRead == 0)
            {
                System.Console.WriteLine("No Propeller Found!");
                port.Close();
                return;
            }

            /*
             * Get Propeller Version
             */
            port.Read(Data, 0, port.BytesToRead);
            if (Data[11] < 'A' || Data[11] > 'G')
            {
                System.Console.WriteLine("Wrong Propeller Found!");
                port.Close();
                return;
            }

            /*
             * Send Base64 Encoding Program
             */
            s = Convert.ToBase64String(program);
            i = s.Length;
            port.Write(Program.PropTxt);
            port.Write(s);
            port.Write(" ~");
            i = port.BytesToWrite;
            if (port.BytesToRead > 0)
            {
                port.Read(Data, 0, port.BytesToRead);
                if (Data[0] != '.')
                {
                    System.Console.WriteLine("Program Did Not Load!");
                    port.Close();
                    return;
                }
            }

            Terminal(FileName);

            port.Close();
            Console.WriteLine("\u001b[31mTerminal Ended\u001b[0m");

        }

        void ResetProp()
        {
            port.DtrEnable = true;
            Thread.Sleep(500);
            port.DtrEnable = false;
            Thread.Sleep(50);
        }

        static void Terminal(string filename)
        {
            ConsoleKeyInfo key;
            char[] c = new char[128];
            string s;
            int i;
            bool Ok = true;

            var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
            if (!GetConsoleMode(iStdOut, out uint outConsoleMode))
            {
                Console.WriteLine("failed to get output console mode");
                Console.ReadKey();
                return;
            }

            outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;
            if (!SetConsoleMode(iStdOut, outConsoleMode))
            {
                Console.WriteLine($"failed to set output console mode, error code: {GetLastError()}");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Entering terminal mode.  Press Ctrl-] to exit. ");
            port.BaudRate = Program.baud;
            Console.Title = filename;

            i = 0;
            while (Ok)
            {
                if (i > 0)
                    port.Write(c, 0, i);
                i = port.BytesToRead;
                if (i > 0)
                {
                    port.Read(Data, 0, i);
                    s = Encoding.UTF8.GetString(Data, 0, i);
                    Console.Write(s);
                }
                i = 0;
                while (Console.KeyAvailable)
                {
                    key = Console.ReadKey(true);
                    c[i] = key.KeyChar;
                    if ((c[i] == 29) || (c[i++] == 26))
                        Ok = false;
                }
            }
        }

    }
}
