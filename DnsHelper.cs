using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NET2
{
    public static class DnsHelper
    {
        static Socket sock;
        static IPEndPoint dnsServer;
        static DnsHelper()
        {
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sock.ReceiveTimeout = 500;

            // Send dns queries to Google's dns server
            dnsServer = new IPEndPoint(0x08080808L, 53);
            sock.Connect(dnsServer);
        }

        public static string GetDomain(string addr)
        {
            string[] splits = addr.Split('.');
            
            string hostname = splits[3] + "." + splits[2] + "." + splits[1] + "." + splits[0] + ".in-addr.arpa";
            splits = hostname.Split('.');

            var buf = new byte[500];

            // Minimum dns packet length + string length and padding
            var message = new byte[16 + hostname.Length + 2];
            // Identifier = 58
            message[1] = 58;
            message[2] = 1; // Recursive lookup
            message[5] = 1; // One question

            // Index 12 - first byte of string
            int messageInd = 12;
            for (int i = 0; i < splits.Length; ++i)
            {
                message[messageInd] = (byte)splits[i].Length;
                ++messageInd;

                for (int j = 0; j < splits[i].Length; ++j)
                {
                    message[messageInd] = (byte)splits[i][j];
                    ++messageInd;
                }
            }

            // End string with 0
            message[messageInd] = 0;

            // Question config
            messageInd += 2;

            // PTR type
            message[messageInd] = 12;

            // Config stuff
            messageInd += 2;
            message[messageInd] = 1;

            int bytes;

            // This is non-zero in case there's no response
            buf[3] = 15;

            for (int i = 0; i < 3; ++i)
            {
                try
                {
                    sock.Send(message);
                    bytes = sock.Receive(buf);

                    if (buf[1] == 58)
                        break;
                }
                catch (SocketException e) { }
            }

            // Return if error
            if ((buf[3] & 15) != 0)
                return null;

            // Go through our question
            messageInd = 12;
            while (buf[messageInd] != 0)
                ++messageInd;

            messageInd += 5;
            // Now we're at the 1st answer
            
            messageInd += 10;
            // Now we're at the data size of the 1st answer

            // Length of address
            int len = buf[messageInd] << 8 + buf[messageInd + 1];
            messageInd += 2;


            var res = new StringBuilder();
            while (buf[messageInd] != 0)
            {
                int sLen = buf[messageInd];
                ++messageInd;
                for (int i = 0; i < sLen; ++i)
                    res.Append((char)buf[messageInd + i]);
                res.Append('.');

                messageInd += sLen;
            }
            res.Remove(res.Length - 1, 1);

            return res.ToString();
        }

        public static IPAddress GetIP(string hostname)
        {
            string[] splits = hostname.Split('.');

            var buf = new byte[200];

            // Minimum dns packet length + string length and padding
            var message = new byte[16 + hostname.Length + 2];
            // Identifier = 58
            message[1] = 58;
            message[2] = 1; // Recursive lookup
            message[5] = 1; // One question

            // index 12 - first byte of string
            int messageInd = 12;
            for (int i = 0; i < splits.Length; ++i)
            {
                message[messageInd] = (byte)splits[i].Length;
                ++messageInd;

                for (int j = 0; j < splits[i].Length; ++j)
                {
                    message[messageInd] = (byte)splits[i][j];
                    ++messageInd;
                }
            }

            // End string with 0
            message[messageInd] = 0;

            // Question config
            messageInd += 2;
            message[messageInd] = 1;
            messageInd += 2;
            message[messageInd] = 1;

            int bytes = 0;

            // This is non-zero in case there's no response
            buf[3] = 15;

            for (int i = 0; i < 3; ++i)
            {
                try
                {
                    sock.Send(message);
                    bytes = sock.Receive(buf);

                    if (buf[1] == 58)
                        break;
                }
                catch (SocketException e) { }
            }

            // If no error
            if ((buf[3] & 15) != 0)
                return null;

            messageInd = bytes - 4;
            return new IPAddress(buf[messageInd..bytes]);
        }
    }
}
