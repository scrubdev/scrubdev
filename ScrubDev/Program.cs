using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrubDev
{
    class Program
    {
        static void Main(string[] args)
        {
            SocketServer.AsyncSocketListener.Listen(3069);
        }
    }
}
