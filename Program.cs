using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NET2
{
    static class Program
    {
        static byte[] GetChecksum(byte[] arr)
        {
            ushort sum = 0;
            
            for (int i = 0; i < arr.Length; i += 2)
                sum += BitConverter.ToUInt16(arr, i);

            return BitConverter.GetBytes((short) ~sum);
        }

        static string TripleEcho(Socket sock, IPEndPoint dest, byte[] message, byte[] buf)
        {
            int bytesReceived;
            var timeTrack = new Stopwatch();
            string node = null;

            for (int i = 0; i < 3; ++i)
            {
                try 
                {
                    timeTrack.Reset();

                    sock.SendTo(message, dest);
                    timeTrack.Start();

                    bytesReceived = sock.Receive(buf);

                    timeTrack.Stop();

                    int ipv4size = (buf[0] & 15) * 4;

                    // If echo or ttl expired
                    if (buf[ipv4size] == 11 || buf[ipv4size] == 0)
                    {
                        if (timeTrack.ElapsedMilliseconds > 0)
                            Console.Write("{0,4} ms ", timeTrack.ElapsedMilliseconds);
                        else
                            Console.Write("  <1 ms ");

                        // get ip address
                        node = String.Join('.', buf[12..16]);
                    }
                    else
                        Console.Write("   *    ");
                }
                catch(SocketException e)
                {
                    Console.Write("   *    ");
                }
            }

            return node;
        }

        static void Trace(string destname, int limit, bool resolveName)
        {
            var message = new byte[8];
            var buf = new byte[250];

            // type = echo
            message[0] = 8;
            // identifier = 58
            message[5] = 58;

            // Copy checksum to bytes 2 and 3
            byte[] checksum = GetChecksum(message);
            Array.Copy(checksum, 0, message, 2, 2);

            var sender = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            
            var localEndPoint = new IPEndPoint(IPAddress.Any, 0);
            sender.Bind(localEndPoint);
            sender.ReceiveTimeout = 1000;

            IPAddress ipaddr = null;

            if (IPAddress.TryParse(destname, out ipaddr) && ipaddr.AddressFamily == AddressFamily.InterNetwork)
            {
                Console.Write(destname);

                string domain = DnsHelper.GetDomain(destname);

                if (domain != null)
                    Console.Write(" [" + domain + "]");
                else
                    Console.Write(" [Domain not found]");

                Console.WriteLine();
            }
            else
            {
                // Domain name or not IPv4 address

                ipaddr = DnsHelper.GetIP(destname);
                if (ipaddr == null)
                {
                    Console.WriteLine("Couldn't resolve address " + destname);
                    return;
                }
                else
                    Console.WriteLine("{1} [{0}]", destname, ipaddr.ToString());
            }

            var dest = new IPEndPoint(ipaddr, 0);
            
            int ipv4size;
            bool destReached = false;

            for (int ttl = 1; ttl <= limit; ++ttl)
            {
                Console.Write("{0,2} ", ttl);
                sender.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, ttl);

                string addr = TripleEcho(sender, dest, message, buf);

                if (addr != null)
                {
                    Console.Write(" {0,-15}", addr);

                    if (resolveName)
                    {
                        string domain = DnsHelper.GetDomain(addr);
                        if (domain != null)
                            Console.Write(" [" + domain + "]");
                    }

                    Console.WriteLine();
                }
                else
                    Console.WriteLine(" Node unknown");

                // calculate IPv4 header size in bytes
                ipv4size = (buf[0] & 15) * 4;
                
                // If received echo reply, stop tracing
                if (buf[ipv4size] == 0)
                {
                    destReached = true;
                    break;
                }
            }

            if (!destReached)
                Console.Write("Node limit reached. ");

            Console.WriteLine("Tracing over");

            sender.Close();
        }

        static void Main()
        {
            while (true)
            {
                string[] splits = Console.ReadLine().Split(' ');

                if (splits.Length == 4)
                    Trace(splits[1], int.Parse(splits[2]), splits[3] == "y");
                else
                    Console.WriteLine("Invalid command");

                Console.WriteLine();
            }
        }
    }
}
