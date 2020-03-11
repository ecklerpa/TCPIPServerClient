using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Security.Permissions;

namespace TCPClient
{
    public class Client
    {
        /// <summary>
        /// A delegate type called when a client recieves data from a server.  Void return type.
        /// </summary>
        /// <param name="message">A byte array representing the message received from the server.</param>
        /// <param name="messageSize">The size, in bytes of the message.</param>
        public delegate void ReceiveDataCallback(byte[] message, int messageSize);

        /// <summary>
        /// A delegate type called when a client receives a broadcast message.  Void return type.
        /// </summary>
        /// <param name="message">A byte array representing the message received from the server.</param>
        /// <param name="messageSize">The size, in bytes of the message.</param>
        public delegate void ReceiveBroadcastCallback(byte[] message, int messageSize);

        /// <summary>
        /// A delegate called when disconnected from the server.
        /// </summary>
        public delegate void DisconnectCallback();

        private ReceiveDataCallback _receive = null;
        private ReceiveBroadcastCallback _broadcast = null;
        private DisconnectCallback _disconnect = null;

        private Socket _clientSocket;
        private Socket _broadcastSocket = null;
        private bool _receiveBroadcasts = false;
        private int _broadcastPort = 0;
        public DateTime LastDataFromServer;

        /// <summary>
        /// Modify the callback function used when data is received from the server.
        /// </summary>
        public ReceiveDataCallback OnReceiveData
        {
            get
            {
                return _receive;
            }

            set
            {
                _receive = value;
            }
        }

        /// <summary>
        /// Modify the callback function used when data is received from the server.
        /// </summary>
        public ReceiveBroadcastCallback OnReceiveBroadcast
        {
            get
            {
                return _broadcast;
            }

            set
            {
                _broadcast = value;
            }
        }

        /// <summary>
        /// Modify the callback function used when data is received from the server.
        /// </summary>
        public DisconnectCallback OnDisconnected
        {
            get
            {
                return _disconnect;
            }

            set
            {
                _disconnect = value;
            }
        }

        public bool Connected
        {
            get
            {
                if (_clientSocket == null)
                    return false;
                else
                    return _clientSocket.Connected;
            }
        }

        public bool ReceiveBroadcasts
        {
            get
            {
                return _receiveBroadcasts;
            }

            set
            {
                if (_receiveBroadcasts != value)
                {
                    _receiveBroadcasts = value;
                    if (_receiveBroadcasts)
                    {
                        if (_broadcastPort > 0)
                            SetupBroadcastSocket();
                    }

                    else if (_broadcastSocket != null)
                        _broadcastSocket.Close();
                }
            }
        }

        public int BroadcastPort
        {
            get
            {
                return _broadcastPort;
            }

            set
            {
                if (_broadcastPort != value)
                {
                    _broadcastPort = value;

                    if (_receiveBroadcasts)
                        SetupBroadcastSocket();
                }
            }
        }

        ///// <summary>
        ///// Returns the Socket owned by this class. I may not need it!
        ///// </summary>
        //public Socket getTheSocket
        //{
        //    get
        //    {
        //        if (_clientSocket == null)
        //            return null;
        //        else
        //            return _clientSocket;
        //    }
        //}

        /// <summary>
        /// Construct a Client setting the callback.
        /// </summary>
        /// <param name="receive">Callback, called when the client receives data from the server.</param>
        public Client()
        {
            LastDataFromServer = DateTime.Now;//initialize to current time
        }

        /// <summary>
        /// Connect to a server at a specific IP address and port.
        /// </summary>
        /// <param name="address">The IP address of the server to connect to.  
        /// To get an IP address from a string use System.Net.IPAddress.Parse("0.0.0.0")</param>
        /// <param name="port">The port number on the server to connect to.</param>
        public void Connect(IPAddress address, int port)
        {
            try
            {
                Disconnect();

                _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _clientSocket.Connect(new IPEndPoint(address, port));

                if (_clientSocket.Connected)
                    WaitForData();
            }

            catch (SocketException se)
            {
                System.Console.WriteLine("Client EXCEPTION in Connect: " + se.Message);
                ToFile(se.Message);
            }
        }

        /// <summary>
        /// Disconnect a client from a server.  If the client is not connected to a server when this function is called, there is no effect.
        /// </summary>
        public void Disconnect()
        {
            if (_clientSocket != null)
                _clientSocket.Close();
        }

        /// <summary>
        /// Send a message to the server we are connected to.
        /// </summary>
        /// <param name="message">A byte array representing the message to send.</param>
        public void SendMessage(byte[] message)
        {
            if (_clientSocket != null)
                if (_clientSocket.Connected)
                    _clientSocket.Send(message);
        }

        /// <summary>
        /// Start an asynchronous wait for data from the server.  When data is recieved, a callback will be triggered.
        /// </summary>
        private void WaitForData()
        {
            try
            {
                Packet pack = new Packet(_clientSocket);
                _clientSocket.BeginReceive(pack.DataBuffer, 0, pack.DataBuffer.Length, SocketFlags.None, new AsyncCallback(OnDataReceived), pack);
            }

            catch (SocketException se)
            {
                System.Console.WriteLine("Client EXCEPTION in WaitForData: " + se.Message);
                ToFile(se.Message);
            }
        }

        /// <summary>
        /// A callback triggered by receiving data from the server.
        /// </summary>
        /// <param name="asyn">The packet object received from the server containing the received message.</param>
        private void OnDataReceived(IAsyncResult asyn)
        {
            try
            {
                Packet socketData = (Packet)asyn.AsyncState;
                int dataSize = socketData.CurrentSocket.EndReceive(asyn);

                if (_receive != null)
                    _receive(socketData.DataBuffer, dataSize);

                WaitForData();
            }

            catch (ObjectDisposedException)
            {
                System.Console.WriteLine("Client EXCEPTION in OnDataReceived: Socket has been closed");
            }

            catch (SocketException se)
            {
                System.Console.WriteLine("Client EXCEPTION in OnDataReceived: " + se.Message);

                if (OnDisconnected != null)
                    OnDisconnected();

                ToFile(se.Message);
            }
        }

        private void SetupBroadcastSocket()
        {
            if (_broadcastSocket != null)
                _broadcastSocket.Close();

            _broadcastSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _broadcastSocket.Bind((EndPoint)(new IPEndPoint(IPAddress.Any, _broadcastPort)));

            WaitForBroadcast();
        }

        /// <summary>
        /// Start an asynchronous wait for data from the server.  When data is recieved, a callback will be triggered.
        /// </summary>
        private void WaitForBroadcast()
        {
            try
            {
                Packet pack = new Packet(_broadcastSocket);
                EndPoint port = (EndPoint)(new IPEndPoint(IPAddress.Any, _broadcastPort));

                _broadcastSocket.BeginReceiveFrom(pack.DataBuffer, 0, pack.DataBuffer.Length, SocketFlags.None, ref port, new AsyncCallback(OnBroadcastReceived), pack);
            }

            catch (SocketException se)
            {
                System.Console.WriteLine("Client EXCEPTION in WaitForBroadcast: " + se.Message);
            }
        }

        private void OnBroadcastReceived(IAsyncResult asyn)
        {
            try
            {
                Packet socketData = (Packet)asyn.AsyncState;
                int dataSize = socketData.CurrentSocket.EndReceive(asyn);

                if (_broadcast != null)
                    _broadcast(socketData.DataBuffer, dataSize);

                WaitForBroadcast();
            }

            catch (ObjectDisposedException)
            {
                System.Console.WriteLine("Client EXCEPTION in OnBroadcastReceived: Socket has been closed");
            }

            catch (SocketException se)
            {
                System.Console.WriteLine("Client EXCEPTION in OnBroadcastReceived: " + se.Message);
            }
        }

        /// <summary>
        /// Represents a TCP/IP transmission containing the socket it is using, the clientNumber
        ///  (used by server communication only), and a data buffer representing the message.
        /// </summary>
        private class Packet
        {
            public Socket CurrentSocket;
            //public byte[] DataBuffer = new byte[4096];
            public byte[] DataBuffer = new byte[1024];

            /// <summary>
            /// Construct a Packet Object
            /// </summary>
            /// <param name="sock">The socket this Packet is being used on.</param>
            /// <param name="client">The client number that this packet is from.</param>
            public Packet(Socket sock)
            {
                CurrentSocket = sock;
            }
        }

        private void ToFile(string message)
        {
            string AppPath = CommonClassLibs.GeneralFunction.GetAppPath;
            
            System.IO.StreamWriter sw = null;
            try
            {
                sw = System.IO.File.AppendText(System.IO.Path.Combine(AppPath, "SockClient.txt"));
                sw.WriteLine(String.Format("{0:G}: {1}.", DateTime.Now, message));
            }
            catch (Exception ex)
            {
                //Console.WriteLine("\n\nError in ToFile:\n" + message + "\n" + ex.Message + "\n\n");
                System.Windows.Forms.MessageBox.Show($"ERROR:\n\n{ex.Message}, Possible Permissions Issue!");
            }
            finally
            {
                try
                {
                    if (sw != null)
                    {
                        sw.Close();
                        sw.Dispose();
                    }
                }
                catch 
                {
                    //Console.WriteLine("\n\nISSUE HERE TOO:\n" + ex2.Message + "\n\n");
                }
            }
        }
    }
}
