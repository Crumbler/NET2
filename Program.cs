using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

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

        static void Trace()
        {
            var message = new byte[8];
            var buf = new byte[250];
            message[0] = 8;
            message[5] = 1;

            byte[] checksum = GetChecksum(message);
            Array.Copy(checksum, 0, message, 2, 2);

            var sender = new Socket(SocketType.Raw, ProtocolType.Icmp);
            sender.ReceiveTimeout = 1000;
            IPHostEntry host = Dns.GetHostEntry("google.com");
            var dest = new IPEndPoint(host.AddressList[0], 0);
            
            int echoNum = 1, ipv4size;

            do
            {
                Console.Write(echoNum + " ");
                sender.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, echoNum);
                ++echoNum;


                int bytesReceived;
                do 
                {
                    sender.SendTo(message, dest);
                    bytesReceived = sender.Receive(buf);
                } while (bytesReceived == 0);
                

                Console.Write("Protocol: " + buf[9].ToString() + " ");

                Console.WriteLine(String.Join('.', buf[12..16]));

                ipv4size = (buf[0] & 15) * 4;
            } while(buf[ipv4size] != 0);
        }

        static void Main(string[] args)
        {
            try 
            {
                Trace();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.ReadKey();
        }
    }
}
