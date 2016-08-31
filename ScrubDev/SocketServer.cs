using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ScrubDev
{
    static class SocketServer
    {
        // https://msdn.microsoft.com/en-us/library/fx6588te(v=vs.110).aspx -- used as a guide and expanded upon for ongoing connections

        public class StateObject
        {
            public Socket ClientSocket = null;
            public const int BufferSize = 1024;
            public byte[] RecvBuffer = new byte[BufferSize];
            public StringBuilder RecvData = new StringBuilder();

            public void Reset()
            {
                // resets the state object's buffer/string
                RecvBuffer = new byte[BufferSize];
                RecvData = new StringBuilder();
            }
        }

        public class AsyncSocketListener
        {
            public static ManualResetEvent AllDone = new ManualResetEvent(false); // thread signal

            public static void Listen(int port)
            {
                byte[] bytes = new Byte[1024]; // Incoming data buffer

                // resolve the info of the endpoint of the socket
                IPAddress ipAddress = IPAddress.Any; // all interfaces
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

                // make the tcp socket
                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // try bind socket
                try
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(100);

                    Console.WriteLine($"Started socket server on {localEndPoint.Address}:{localEndPoint.Port}.");

                    while (true)
                    {
                        AllDone.Reset(); // reset the event
                        listener.BeginAccept(new AsyncCallback(AcceptCallback), listener); // start to accept and hit accept callback when done
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            public static void AcceptCallback(IAsyncResult ar)
            {
                AllDone.Set();

                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar); // accept connection and get the socket

                Console.WriteLine($"Accepted sockets connection from {handler.LocalEndPoint}.");

                StateObject state = new StateObject(); // state
                state.ClientSocket = handler;

                try
                {
                    handler.BeginReceive(state.RecvBuffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state); // begin receiving data, call read when done
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            public static void ReadCallback(IAsyncResult ar)
            {
                String content = String.Empty;

                // Get the socket & the state obj from the async result
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.ClientSocket;

                try
                {
                    int bytesRead = handler.EndReceive(ar);

                    if (bytesRead > 0)
                    {
                        // theres bytes!
                        state.RecvData.Append(Encoding.UTF8.GetString(state.RecvBuffer, 0, bytesRead)); // add them to our stringbuilder

                        content = state.RecvData.ToString();
                        if (content.IndexOf("\r\n") > -1)
                        {
                            // all data read, it has hit our end of line
                            Console.WriteLine($"Data recv from socket <{handler.LocalEndPoint}>: {content.Trim()}");
                            state.Reset();
                            Send($"ACK READ {bytesRead}\r\n", state); // just reply with an acknowledgment of how many bytes read
                        }
                        else
                        {
                            // more to get as havent hit end of line character
                            handler.BeginReceive(state.RecvBuffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            public static void Send(String d, StateObject st)
            {
                // stringify to utf8
                byte[] bytes = Encoding.UTF8.GetBytes(d);

                try
                {
                    st.ClientSocket.BeginSend(bytes, 0, bytes.Length, 0, new AsyncCallback(SendCallback), st); // send the utf8 data to client
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            private static void SendCallback(IAsyncResult ar)
            {
                try
                {
                    StateObject handler = (StateObject)ar.AsyncState; // socket from the state

                    int bytesSent = handler.ClientSocket.EndSend(ar);
                    Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                    // reset state and listen for more again!
                    handler.Reset();
                    handler.ClientSocket.BeginReceive(handler.RecvBuffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), handler);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
}
