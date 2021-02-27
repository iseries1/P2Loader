using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Net.NetworkInformation;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;

namespace P2Loader
{
    class WiFi
    {
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

        static byte[] program;
        static string PropHex = "> Prop_Hex 0 0 0 0";
        static string PropTxt = "> Prop_Txt 0 0 0 0 ";
        static string PropClk = "> Prop_Clk 0 0 0 0 ";
        static string PropChk = "> Prop_Chk 0 0 0 0  ";
        TcpClient tcpClient;
        TcpClient Telnet;
        static byte[] Data = new byte[4096];
        static NetworkStream NS;
        static NetworkStream TS;

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern uint GetLastError();

        public WiFi(string Port, string filename)
        {
            byte[] b;
            byte[] data = new byte[256];
            int i;
            string s;
            ESP esp = null;
            List<ESP> W;

            if (!File.Exists(filename))
            {
                Console.WriteLine("File {0} Not Found!", filename);
                return;
            }

            program = File.ReadAllBytes(filename);

            if ((Port.Substring(0, 1).CompareTo("0") >= 0) && (Port.Substring(0, 1).CompareTo("9") <= 0))
            {
                ESP eSP = new ESP();
                eSP.IP = new IPEndPoint(IPAddress.Parse(Port), 23);
                eSP.name = Port;
                W = new List<ESP>();
                W.Add(eSP);
            }
            else
            {
                W = FindProps();
            }

            foreach(ESP sP in W)
            {
                if (sP.name.Equals(Port))
                    esp = sP;
            }

            if (esp == null)
            {
                Console.WriteLine("WiFi Module Not Found!");
                return;
            }

            tcpClient = new TcpClient();
            Telnet = new TcpClient();
            tcpClient.Connect(esp.IP);
            esp.IP.Port = 23;
            Telnet.Connect(esp.IP);
            Telnet.NoDelay = true;
            TS = Telnet.GetStream();
            NS = tcpClient.GetStream();
            b = Encoding.UTF8.GetBytes("GET /propeller/reset HTTP/1.1\r\n\r\n");
            NS.Write(b, 0, b.Length);
            Thread.Sleep(50);
            b = Encoding.UTF8.GetBytes(PropChk);
            TS.Write(b, 0, b.Length);
            if (NS.DataAvailable)
            {
                i = NS.Read(data, 0, data.Length);
                s = Encoding.UTF8.GetString(data, 0, i);
                //Console.WriteLine(s);
            }    
            Thread.Sleep(50);
            if (TS.DataAvailable)
            {
                i = TS.Read(data, 0, data.Length);
                s = Encoding.UTF8.GetString(data, 0, i);
                //Console.WriteLine(s);
            }
            i = data[11];
            NS.Close();
            if ((i < 'A') || (i > 'G'))
            {
                Console.WriteLine("No Propeller Found!");
                TS.Close();
                return;
            }

            /*
             * Send Base64 Encoding Program
            */
            b = Encoding.UTF8.GetBytes(PropTxt);
            TS.Write(b, 0, b.Length);

            s = Convert.ToBase64String(program);
            b = Encoding.UTF8.GetBytes(s);
            TS.Write(b, 0, b.Length);
            s = " ~";
            b = Encoding.UTF8.GetBytes(s);
            TS.Write(b, 0, b.Length);
            Console.WriteLine("Program Loaded");

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
            Terminal(filename);
            TS.Close();
        }

        static List<ESP> FindProps()
        {
            IPAddress address;
            List<ESP> L;

            address = null;
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            for (int i = 0; i < adapters.Length; i++)
            {
                NetworkInterface n = adapters[i];
                if (n.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties p = n.GetIPProperties();
                    UnicastIPAddressInformationCollection c = p.UnicastAddresses;
                    foreach (UnicastIPAddressInformation f in c)
                    {
                        address = f.Address;
                        i = adapters.Length;
                        break;
                    }
                }
            }
            L = Discovery(address);
            if (L == null)
                L = Discovery(address);

            return L;
        }

        static List<ESP> Discovery(IPAddress address)
        {
            byte[] packet;
            UdpClient udp;
            ESP esp;

            DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(ESP));

            byte[] b = address.GetAddressBytes();
            b[b.Length - 1] = 255;
            address = new IPAddress(b);
            IPEndPoint iP = new IPEndPoint(address, 32420);
            byte[] discover = new byte[4];
            udp = new UdpClient(32420);
            udp.Send(discover, 4, iP);
            Thread.Sleep(250);
            udp.Send(discover, 4, iP);
            Thread.Sleep(250);
            List<ESP> L = new List<ESP>();
            while (udp.Available > 0)
            {
                packet = udp.Receive(ref iP);
                if (packet.Length > 4)
                {
                    MemoryStream S = new MemoryStream(packet);
                    esp = (ESP)js.ReadObject(S);
                    esp.IP = iP;
                    esp.IP.Port = 80;
                    L.Add(esp);
                }
            }
            udp.Close();
            return L;
        }

        static void Terminal(string filename)
        {
            ConsoleKeyInfo key;
            char[] c = new char[1];
            byte[] b = new byte[1];
            string s;
            int i;
            bool Ok = true;

            Console.WriteLine("Entering terminal mode.  Press Ctrl-] to exit. ");
            Console.Title = filename;

            c[0] = (char)0;
            while (Ok)
            {
                if (c[0] > 0)
                {
                    b[0] = (byte)c[0];
                    TS.Write(b, 0, 1);
                }
                c[0] = (char)0;
                
                if (TS.DataAvailable)
                {
                    i = TS.Read(Data, 0, Data.Length);
                    s = Encoding.UTF8.GetString(Data, 0, i);
                    Console.Write(s);
                }
                if (Console.KeyAvailable)
                {
                    key = Console.ReadKey(true);
                    c[0] = key.KeyChar;
                    if (c[0] == 29)
                        Ok = false;
                }
            }
        }
    }

    [DataContract]
    public class ESP
    {
        [DataMember]
        public string name;
        [DataMember]
        public string description;
        [DataMember(Name = "reset pin")]
        public string resetpin;
        [DataMember(Name = "rx pullup")]
        public string rxpullup;
        [DataMember(Name = "mac address")]
        public string macaddress;
        public IPEndPoint IP;
    }
}
