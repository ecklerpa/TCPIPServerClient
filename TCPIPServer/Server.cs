using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace TCPServer
{
    /// <summary>
    /// A class that listens on a port for client connections, sends and recieves messages from connected clients,
    /// and periodically broadcasts UDP messages.
    /// </summary>
    public class Server
    {
        /// <summary>
        /// A delegate type called when a client initially connects to the server.  Void return type.
        /// </summary>
        /// <param name="clientNumber">A unique identifier of the client that has connected to the server.</param>
        public delegate void ClientConnectCallback(int clientNumber);

        /// <summary>
        /// A delegate type called when a client disconnects from the server.  Void return type.
        /// </summary>
        /// <param name="clientNumber">A unique identifier of the client that has disconnected from the server.</param>
        public delegate void ClientDisconnectCallback(int clientNumber);

        /// <summary>
        /// A delegate type called when the server receives data from a client.
        /// </summary>
        /// <param name="clientNumber">A unique identifier of the client that has disconnected from the server.</param>
        /// <param name="message">A byte array representing the message sent.</param>
        /// <param name="messageSize">The size in bytes of the message.</param>
        public delegate void ReceiveDataCallback(int clientNumber, byte[] message, int messageSize);

        private ClientConnectCallback _clientConnect = null;
        private ClientDisconnectCallback _clientDisconnect = null;
        private ReceiveDataCallback _receive = null;

        private Socket _mainSocket;
        private System.Threading.Timer _broadcastTimer;
        private int _currentClientNumber = 0;

        public class UserSock
        {
            public UserSock(int nClientID, Socket s)
            {
                _iClientID = nClientID;
                _UserSocket = s;
                _dTimer = DateTime.Now;//Initialize the ping timer to the current time
                _szStationName = string.Empty;
                _szClientName = string.Empty;
                _UserListentingPort = 9998;//default
                _szAlternateIP = string.Empty;
                _pingStatClass = new PingStatsClass();
            }

            public int iClientID { get { return _iClientID; } }
            public Socket UserSocket { get { return _UserSocket; } }
            public DateTime dTimer { get { return _dTimer; } set { _dTimer = value; } }
            public string szClientName { get { return _szClientName; } set { _szClientName = value; } }
            public string szStationName { get { return _szStationName; } set { _szStationName = value; } }
            public UInt16 UserListentingPort { get { return _UserListentingPort; } set { _UserListentingPort = value; } }
            public string szAlternateIP { get { return _szAlternateIP; } set { _szAlternateIP = value; } }
            public PingStatsClass PingStatClass { get { return _pingStatClass; } set { _pingStatClass = value; } }


            public int ZeroDataCount { get; internal set; }

            private Socket _UserSocket;
            private DateTime _dTimer;
            private int _iClientID;
            private string _szClientName;
            private string _szStationName;
            private UInt16 _UserListentingPort;
            private string _szAlternateIP;
            private PingStatsClass _pingStatClass;
        }

       // public Dictionary<int, Socket > workerSockets = new Dictionary<int, Socket>();
        public Dictionary<int, UserSock> workerSockets = new Dictionary<int, UserSock>();
        

        /// <summary>
        /// Modify the callback function used when a client initially connects to the server.
        /// </summary>
        public ClientConnectCallback OnClientConnect
        {
            get
            {
                return _clientConnect;
            }

            set
            {
                _clientConnect = value;
            }
        }

        /// <summary>
        /// Modify the callback function used when a client disconnects from the server.
        /// </summary>
        public ClientDisconnectCallback OnClientDisconnect
        {
            get
            {
                return _clientDisconnect;
            }

            set
            {
                _clientDisconnect = value;
            }
        }

        /// <summary>
        /// Modify the callback function used when the server receives a message from a client.
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
        /// Whether or not the server is currently listening for new client connections.
        /// </summary>
        public bool IsListening
        {
            get
            {
                if (_mainSocket == null)
                    return false;
                else
                    return _mainSocket.IsBound;
            }
        }

        /// <summary>
        /// Make the server listen for client connections on a specific port.
        /// </summary>
        /// <param name="listenPort">The number of the port to listen for connections on.</param>
        public void Listen(int listenPort)
        {
            try
            {
                Stop();

                _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _mainSocket.Bind(new IPEndPoint(IPAddress.Any, listenPort));
                _mainSocket.Listen(100);
                _mainSocket.BeginAccept(new AsyncCallback(OnReceiveConnection), null);
            }

            catch (SocketException se)
            {
                System.Console.WriteLine(se.Message);
            }
        }

        /// <summary>
        /// Stop listening for new connections and close all currently open connections.
        /// </summary>
        public void Stop()
        {
            lock (workerSockets)
            {
                foreach (UserSock s in workerSockets.Values)
                {
                    if (s.UserSocket.Connected)
                        s.UserSocket.Close();
                }
                workerSockets.Clear();
            }

            if (IsListening)
                _mainSocket.Close();
        }

        /// <summary>
        /// Send a message to all connected clients.
        /// </summary>
        /// <param name="message">A byte array representing the message to send.</param>
        //public void SendMessage(byte[] message)
        //{
        //    try
        //    {
        //        foreach (UserSock s in workerSockets.Values)
        //        {
        //            if (s.UserSocket.Connected)
        //            {
        //                try
        //                {
        //                    s.UserSocket.Send(message);
        //                }
        //                catch { }
        //            }
        //        }
        //    }
        //    catch (SocketException se)
        //    {
        //        System.Console.WriteLine(se.Message);
        //    }
        //}

        public void SendMessage(byte[] message, bool testConnections = false)
        {
            if (testConnections)
            {
                List<int> ClientsToRemove = new List<int>();
                foreach (int clientId in workerSockets.Keys)
                {
                    if (workerSockets[clientId].UserSocket.Connected)
                    {
                        try
                        {
                            workerSockets[clientId].UserSocket.Send(message);
                        }
                        catch
                        {
                            ClientsToRemove.Add(clientId);
                        }

                        Thread.Sleep(10);// this is for a client Ping so stagger the send messages
                    }
                    else
                        ClientsToRemove.Add(clientId);
                }

                //lock (workerSockets)//Already locked from the caller
                {
                    if (ClientsToRemove.Count > 0)
                    {
                        foreach (int cID in ClientsToRemove)
                        {
                            //Socket gets closed and removed from OnClientDisconnect
                            if (OnClientDisconnect != null)
                            {
                                OnClientDisconnect(cID);
                            }
                        }
                    }
                }
                ClientsToRemove.Clear();
                ClientsToRemove = null;
            }
            else
            {
                foreach (UserSock s in workerSockets.Values)
                {
                    try
                    {
                        if (s.UserSocket.Connected)
                            s.UserSocket.Send(message);
                    }
                    catch (SocketException se)
                    {
                        System.Console.WriteLine(se.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Send a message to a specific client.
        /// </summary>
        /// <param name="clientNumber">A unique identifier of the client that has connected to the server.</param>
        /// <param name="message">A byte array representing the message to send.</param>
        public void SendMessage(int clientNumber, byte[] message)
        {
            if (!workerSockets.ContainsKey(clientNumber))
            {
                //throw new ArgumentException("Invalid Client Number", "clientNumber");
                System.Console.WriteLine("Invalid Client Number");
                return;
            }
            try
            {
                //workerSockets[clientNumber].Send(message);
                ((UserSock)workerSockets[clientNumber]).UserSocket.Send(message);
            }
            catch (SocketException se)
            {
                System.Console.WriteLine(se.Message);
            }
        }

        /// <summary>
        /// Begin broadcasting a message over UDP every several seconds.
        /// </summary>
        /// <param name="message">A byte array representing the message to send.</param>
        /// <param name="port">The port over which to send the message.</param>
        /// <param name="frequency">Frequency to send the message in seconds.</param>
        public void BeginBroadcast(byte[] message, int port, int frequency)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sock.EnableBroadcast = true;

            Packet pack = new Packet(sock, port);
            pack.DataBuffer = message;

            if (_broadcastTimer != null)
                _broadcastTimer.Dispose();

            _broadcastTimer = new System.Threading.Timer(new TimerCallback(BroadcastTimerCallback), pack, 0, frequency * 1000);
        }

        /// <summary>
        /// Stop broadcasting UDP messages.
        /// </summary>
        public void EndBroadcast()
        {
            if (_broadcastTimer != null)
                _broadcastTimer.Dispose();
        }

        /// <summary>
        /// A callback called by the broadcast timer.  Broadcasts a message.
        /// </summary>
        /// <param name="state">An object representing the byte[] message to be broadcast.</param>
        private void BroadcastTimerCallback(object state)
        {
            ((Packet)state).CurrentSocket.SendTo(((Packet)state).DataBuffer, new IPEndPoint(IPAddress.Broadcast, ((Packet)state).ClientNumber));
        }

        /// <summary>
        /// An internal callback triggered when a client connects to the server.
        /// </summary>
        /// <param name="asyn"></param>
        private void OnReceiveConnection(IAsyncResult asyn)
        {
            try
            {
                lock (workerSockets)
                {
                    Interlocked.Increment(ref _currentClientNumber); // Thread Safe
                    UserSock us = new UserSock(_currentClientNumber, _mainSocket.EndAccept(asyn));
                    workerSockets.Add(_currentClientNumber, us);
                }

                if (_clientConnect != null)
                    _clientConnect(_currentClientNumber);

                WaitForData(_currentClientNumber);
                _mainSocket.BeginAccept(new AsyncCallback(OnReceiveConnection), null);
            }
            catch (ObjectDisposedException)
            {
                System.Console.WriteLine("OnClientConnection: Socket has been closed");
            }
            catch (SocketException se)
            {
                //Console.WriteLine("SERVER EXCEPTION in OnReceiveConnection: " + se.Message);
                System.Diagnostics.Debug.WriteLine("SERVER EXCEPTION in OnReceiveConnection: " + se.Message);//pe 4-22-2015

                if (workerSockets.ContainsKey(_currentClientNumber))
                {
                    Console.WriteLine("RemoteEndPoint: " + workerSockets[_currentClientNumber].UserSocket.RemoteEndPoint.ToString());
                    Console.WriteLine("LocalEndPoint: " + workerSockets[_currentClientNumber].UserSocket.LocalEndPoint.ToString());

                    Console.WriteLine("Closing socket from OnReceiveConnection");
                }

                //Socket gets closed and removed from OnClientDisconnect
                if (OnClientDisconnect != null)
                    OnClientDisconnect(_currentClientNumber);
            }
        }

        /// <summary>
        /// Begins an asynchronous wait for data for a particular client.
        /// </summary>
        /// <param name="clientNumber">A unique identifier of the client that has connected to the server.</param>
        private void WaitForData(int clientNumber)
        {
            if (!workerSockets.ContainsKey(clientNumber))
            {
                //Console.WriteLine("NO KEY: " + clientNumber.ToString());
                return;
            }

            try
            {
                Packet pack = new Packet(workerSockets[clientNumber].UserSocket, clientNumber);
                workerSockets[clientNumber].UserSocket.BeginReceive(pack.DataBuffer, 0, pack.DataBuffer.Length, SocketFlags.None, new AsyncCallback(OnDataReceived), pack);
            }
            catch (SocketException se)
            {
                try
                {
                    //Socket gets closed and removed from OnClientDisconnect
                    if (OnClientDisconnect != null)
                        OnClientDisconnect(clientNumber);

                    //Console.WriteLine("SERVER EXCEPTION in WaitForClientData: " + se.Message);
                    System.Diagnostics.Debug.WriteLine($"SERVER EXCEPTION in WaitForClientData: {se.Message}");//pe 4-22-2015
                }
                catch { }
            }
            catch (Exception ex)
            {
                //Socket gets closed and removed from OnClientDisconnect
                if (OnClientDisconnect != null)
                    OnClientDisconnect(clientNumber);

                string msg = (ex.InnerException != null) ? ex.InnerException.Message : ex.Message;
                System.Diagnostics.Debug.WriteLine($"SERVER EXCEPTION in WaitForClientData2: {msg}");//pe 5-3-2017
            }
        }

        /// <summary>
        /// An internal callback triggered when the server recieves data from a client.
        /// </summary>
        /// <param name="asyn"></param>
        private void OnDataReceived(IAsyncResult asyn)
        {
            Packet socketData = (Packet)asyn.AsyncState;

            try
            {
                int dataSize = socketData.CurrentSocket.EndReceive(asyn);

                if (dataSize.Equals(0))
                {
                    //System.Diagnostics.Debug.WriteLine($"OnDataReceived datasize is 0, zerocount = {((UserSock)workerSockets[socketData.ClientNumber]).ZeroDataCount}");//pe 5-3-2017

                    if (workerSockets.ContainsKey(socketData.ClientNumber))
                    {
                        if (((UserSock)workerSockets[socketData.ClientNumber]).ZeroDataCount++ == 10)
                        {
                            if (OnClientDisconnect != null)
                                OnClientDisconnect(socketData.ClientNumber);
                        }
                    }
                }
                else
                {
                    //if (_receive != null)
                        _receive(socketData.ClientNumber, socketData.DataBuffer, dataSize);

                    ((UserSock)workerSockets[socketData.ClientNumber]).ZeroDataCount = 0;
                }

                WaitForData(socketData.ClientNumber);
            }
            catch (ObjectDisposedException)
            {
                System.Console.WriteLine("OnDataReceived: Socket has been closed");

                //Socket gets closed and removed from OnClientDisconnect
                if (OnClientDisconnect != null)
                    OnClientDisconnect(socketData.ClientNumber);
            }
            catch (SocketException se)
            {
                //10060 - A connection attempt failed because the connected party did not properly respond after a period of time,
                //or established connection failed because connected host has failed to respond.
                if (se.ErrorCode == 10054 || se.ErrorCode == 10060) //10054 - Error code for Connection reset by peer
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("SERVER EXCEPTION in OnClientDataReceived, ServerObject removed:(" + se.ErrorCode.ToString() +  ") " + socketData.ClientNumber + ", (happens during a normal client exit)");
                        System.Diagnostics.Debug.WriteLine("RemoteEndPoint: " + workerSockets[socketData.ClientNumber].UserSocket.RemoteEndPoint.ToString());
                        System.Diagnostics.Debug.WriteLine("LocalEndPoint: " + workerSockets[socketData.ClientNumber].UserSocket.LocalEndPoint.ToString());
                    }
                    catch { }
                    
                    //Socket gets closed and removed from OnClientDisconnect
                    if (OnClientDisconnect != null)
                        OnClientDisconnect(socketData.ClientNumber);

                    Console.WriteLine("Closing socket from OnDataReceived");
                }
                else
                {
                    string mess = "CONNECTION BOOTED for reason other than 10054: code = " + se.ErrorCode.ToString() + ",   " + se.Message;
                    Console.WriteLine(mess);
                    ToFile(mess);
                }
            }
        }

        /// <summary>
        /// Represents a TCP/IP transmission containing the socket it is using, the clientNumber
        ///  (used by server communication only), and a data buffer representing the message.
        /// </summary>
        private class Packet
        {
            public Socket CurrentSocket;
            public int ClientNumber;
            public byte[] DataBuffer = new byte[1024];

            /// <summary>
            /// Construct a Packet Object
            /// </summary>
            /// <param name="sock">The socket this Packet is being used on.</param>
            /// <param name="client">The client number that this packet is from.</param>
            public Packet(Socket sock, int client)
            {
                CurrentSocket = sock;
                ClientNumber = client;
            }
        }

        private void ToFile(string message)
        {
            string AppPath = CommonClassLibs.GeneralFunction.GetAppPath;//Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);//System.Windows.Forms.Application.StartupPath;

            System.IO.StreamWriter sw = null;
            try
            {
                sw = System.IO.File.AppendText(System.IO.Path.Combine(AppPath, "ServerSocketIssue.txt"));
                string logLine = System.String.Format("{0:G}: {1}.", System.DateTime.Now, message);
                sw.WriteLine(logLine);
            }
            catch// (Exception ex)
            {
                //Console.WriteLine("\n\nError in ToFile:\n" + message + "\n" + ex.Message + "\n\n");
               // System.Windows.Forms.MessageBox.Show("ERROR:\n\n" + ex.Message, "Possible Permissions Issue!");
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
                { }
            }
        }
    }

    public class PingStatsClass
    {
        public PingStatsClass()//Int32 ClientID)
        {
            //clientID = ClientID;
            sw = new System.Diagnostics.Stopwatch();
            PingCounter = 0;
            PingTimeTotal = 0;
            LongestPing = 0;
            LongestPingDateTimeStamp = DateTime.Now;
        }

        private System.Diagnostics.Stopwatch sw = null;

        public Int32 PingCounter;

        /// <summary>
        /// Time is in milliseconds
        /// </summary>
        public Int64 PingTimeTotal;

        /// <summary>
        /// Time is in milliseconds
        /// </summary>
        public Int64 LongestPing;
        public DateTime LongestPingDateTimeStamp;

        /// <summary>
        /// returns the elapsed ping time in miliseconds
        /// </summary>
        /// <returns></returns>
        public Int64 StopTheClock()
        {
            if (sw.IsRunning)
            {
                sw.Stop();

                PingCounter++;

                if (sw.ElapsedMilliseconds > LongestPing)
                {
                    LongestPing = sw.ElapsedMilliseconds;
                    LongestPingDateTimeStamp = DateTime.Now;
                }

                PingTimeTotal += sw.ElapsedMilliseconds;
            }

            return sw.ElapsedMilliseconds;
        }

        public void StartTheClock()
        {
            sw.Reset();

            if (!sw.IsRunning)
                sw.Start();
            else
                sw.Restart();
        }

        public Int64 GetElapsedTime
        {
            get { return sw.ElapsedMilliseconds; }
        }
    }
}
