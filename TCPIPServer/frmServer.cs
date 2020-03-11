using CommonClassLibs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TCPServer;

namespace TCPIPServer
{
    public partial class frmServer : Form
    {
        public enum INK
        {
            CLR_BLACK = 0,
            CLR_RED = 1,
            CLR_BLUE = 2,
            CLR_GREEN = 3,
            CLR_PURPLE = 4
        }
        
        private bool ValidBrowser = false;
        private bool displayReady = false;

        bool ServerIsExiting = false;
        int MyPort = 9999;

        /*******************************************************/
        /// <summary>
        /// TCPiP server
        /// </summary>
        Server svr = null;

        private Dictionary<int, MotherOfRawPackets> dClientRawPacketList = null;
        private Queue<FullPacket> FullPacketList = null;
        static AutoResetEvent autoEvent;//mutex
        static AutoResetEvent autoEvent2;//mutex
        private Thread DataProcessThread = null;
        private Thread FullPacketDataProcessThread = null;
        /*******************************************************/

        System.Timers.Timer timerGarbagePatrol = null;
        System.Timers.Timer timerPing = null;

        public frmServer()
        {
            InitializeComponent();
        }

        private void frmServer_Load(object sender, EventArgs e)
        {
            /**********************************************/
            // Init the communications window so we cann whats going on
            ValidBrowser = BrowserVersion();
            // Setup data monitor
            CommunicationsDisplay.Navigate("about:blank");

            OnCommunications($"Loading... {GeneralFunction.GetDateTimeFormatted}", INK.CLR_BLUE);
            /**********************************************/

            /**********************************************/
            //Create a directory we can write stuff too
            CheckOnApplicationDirectory();
            /**********************************************/

            /**********************************************/
            //Start listening for TCPIP client connections
            StartPacketCommunicationsServiceThread();
            /**********************************************/

            /********************************************************/
            // Create some timers for maintenence
            timerPing = new System.Timers.Timer();
            timerPing.Interval = 240000;// 4 minute ping timer
            timerPing.Enabled = true;
            timerPing.Elapsed += timerPing_Elapsed;

            timerGarbagePatrol = new System.Timers.Timer();
            timerGarbagePatrol.Interval = 600000; // 5 minute connection integrity patrol
            timerGarbagePatrol.Enabled = true;
            timerGarbagePatrol.Elapsed += timerGarbagePatrol_Elapsed;
            /********************************************************/

            // enumerate my IP's
            SetHostNameAndAddress();
        }
        
        private void frmServer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (svr != null)
            {
                PACKET_DATA xdata = new PACKET_DATA();

                xdata.Packet_Type = (UInt16)PACKETTYPES.TYPE_HostExiting;
                xdata.Data_Type = 0;
                xdata.Packet_Size = 16;
                xdata.maskTo = 0;
                xdata.idTo = 0;
                xdata.idFrom = 0;

                byte[] byData = PACKET_FUNCTIONS.StructureToByteArray(xdata);

                svr.SendMessage(byData);

                Thread.Sleep(250);
            }

            ServerIsExiting = true;
            try
            {
                if (timerGarbagePatrol != null)
                {
                    timerGarbagePatrol.Stop();
                    timerGarbagePatrol.Elapsed -= timerGarbagePatrol_Elapsed;
                    timerGarbagePatrol.Dispose();
                    timerGarbagePatrol = null;
                }
            }
            catch { }

            try
            {
                if (timerPing != null)
                {
                    timerPing.Stop();
                    timerPing.Elapsed -= timerPing_Elapsed;
                    timerPing.Dispose();
                    timerPing = null;
                }
            }
            catch { }

            KillTheServer();
        }
        

        private void StartPacketCommunicationsServiceThread()
        {
            try
            {
                //Packet processor mutex and loop
                autoEvent = new AutoResetEvent(false); //the RawPacket data mutex
                autoEvent2 = new AutoResetEvent(false);//the FullPacket data mutex
                DataProcessThread = new Thread(new ThreadStart(NormalizeThePackets));
                FullPacketDataProcessThread = new Thread(new ThreadStart(ProcessReceivedData));
               

                //Lists
                dClientRawPacketList = new Dictionary<int, MotherOfRawPackets>();
                FullPacketList = new Queue<FullPacket>();

                //Create HostServer
                svr = new Server();

                svr.Listen(MyPort);//MySettings.HostPort);
                svr.OnReceiveData += new Server.ReceiveDataCallback(OnDataReceived);
                svr.OnClientConnect += new Server.ClientConnectCallback(NewClientConnected);
                svr.OnClientDisconnect += new Server.ClientDisconnectCallback(ClientDisconnect);

                DataProcessThread.Start();
                FullPacketDataProcessThread.Start();

                OnCommunications($"TCPiP Server is listening on port {MyPort}", INK.CLR_GREEN);
            }
            catch(Exception ex)
            {
                var exceptionMessage = (ex.InnerException != null) ? ex.InnerException.Message : ex.Message;
                //Debug.WriteLine($"EXCEPTION IN: StartPacketCommunicationsServiceThread - {exceptionMessage}");
                OnCommunications($"EXCEPTION: TCPiP FAILED TO START, exception: {exceptionMessage}", INK.CLR_RED);
            }
        }

        private void KillTheServer()
        {
            try
            {
                if (svr != null)
                {
                    svr.Stop();
                }
            }
            catch { }

            try
            {
                if (autoEvent != null)
                {
                    autoEvent.Set();

                    Thread.Sleep(30);
                    autoEvent.Close();
                    autoEvent.Dispose();
                    autoEvent = null;
                }
            }
            catch { }

            try
            {
                if (autoEvent2 != null)
                {
                    autoEvent2.Set();

                    Thread.Sleep(30);
                    autoEvent2.Close();
                    autoEvent2.Dispose();
                    autoEvent2 = null;
                }
            }
            catch { }

            Thread.Sleep(15);

            try
            {
                if (dClientRawPacketList != null)
                {
                    dClientRawPacketList.Clear();
                    dClientRawPacketList = null;
                }
            }
            catch { }

            svr = null;
        }

        #region TIMERS
        /// <summary>
        /// Fires every 4 minutes
        /// </summary>
        void timerPing_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            PingTheConnections();
        }
        void PingTheConnections()
        {
            if (svr == null)
                return;

            try
            {
                PACKET_DATA xdata = new PACKET_DATA();

                xdata.Packet_Type = (UInt16)PACKETTYPES.TYPE_Ping;
                xdata.Data_Type = 0;
                xdata.Packet_Size = 16;
                xdata.maskTo = 0;
                xdata.idTo = 0;
                xdata.idFrom = 0;

                xdata.DataLong1 = DateTime.UtcNow.Ticks;

                byte[] byData = PACKET_FUNCTIONS.StructureToByteArray(xdata);

                //Stopwatch sw = new Stopwatch();

                //sw.Start();
                lock (svr.workerSockets)
                {
                    foreach (Server.UserSock s in svr.workerSockets.Values)
                    {
                        //Console.WriteLine("Ping id - " + s.iClientID.ToString());
                        //Thread.Sleep(25);//allow a slight moment so all the replies dont happen at the same time
                        s.PingStatClass.StartTheClock();

                        try
                        {
                            svr.SendMessage(s.iClientID, byData);

                        }
                        catch { }
                    }
                }
                //sw.Stop();
                //Debug.WriteLine("TimeAfterSend: " + sw.ElapsedMilliseconds.ToString() + "ms");
            }
            catch { }
        }
        /**********************************************************************************************************************/

        //private void GarbagePatrol_Tick(object sender, EventArgs e)
        private void timerGarbagePatrol_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                CheckConnectionTimersGarbagePatrol();
            }
            catch { }
        }

        private void CheckConnectionTimersGarbagePatrol()
        {
            List<int> ClientIDsToClear = new List<int>();

            Debug.WriteLine($"{svr.workerSockets.Values.Count} - List Count: {svr.workerSockets.Values.Count}");

            lock (svr.workerSockets)
            {
                foreach (Server.UserSock s in svr.workerSockets.Values)
                {
                    TimeSpan diff = DateTime.Now - s.dTimer;
                    //Debug.WriteLine("iClientID: " + s.iClientID + " - " + "Time: " + diff.TotalSeconds.ToString());

                    if (diff.TotalSeconds >= 600 || s.UserSocket.Connected == false)//10 minutes
                    {
                        //Punt the ListVeiw item here but we must make a list of
                        //clients that we have lost connection with, its not good to remove
                        //the Servers internal client item while inside its foreach loop;
                        //listView1.Items.RemoveByKey(s.iClientID.ToString());
                        ClientIDsToClear.Add(s.iClientID);
                    }
                }
            }

            Debug.WriteLine($"{DateTime.Now.ToLongTimeString()} - Garbage Patrol num of IDs to remove: {ClientIDsToClear.Count}");

            //Ok remove any internal data items we may have
            if (ClientIDsToClear.Count > 0)
            {
                foreach (int cID in ClientIDsToClear)
                {
                    SendMessageOfClientDisconnect(cID);

                    CleanupDeadClient(cID);
                    Thread.Sleep(5);
                }
            }
        }

        private delegate void CleanupDeadClientDelegate(int clientNumber);
        private void CleanupDeadClient(int clientNumber)
        {
            if (InvokeRequired)
            {
                this.Invoke(new CleanupDeadClientDelegate(CleanupDeadClient), new object[] { clientNumber });
                return;
            }

            try
            {
                lock (dClientRawPacketList)//http://www.albahari.com/threading/part2.aspx#_Locking
                {
                    if (dClientRawPacketList.ContainsKey(clientNumber))
                    {
                        dClientRawPacketList[clientNumber].ClearList();
                        dClientRawPacketList.Remove(clientNumber);
                    }
                }
            }
            catch (Exception ex)
            {
                
            }

            try
            {
                lock (svr.workerSockets)
                {
                    if (svr.workerSockets.ContainsKey(clientNumber))
                    {
                        svr.workerSockets[clientNumber].UserSocket.Close();
                        svr.workerSockets.Remove(clientNumber);
                    }
                }
            }
            catch { }

            try
            {
                if (listView1.Items.ContainsKey(clientNumber.ToString()))
                {
                    listView1.Items.RemoveByKey(clientNumber.ToString());
                }
            }
            catch { }

        }
        #endregion

        #region TCPIP Layer incoming data
        private void OnDataReceived(int clientNumber, byte[] message, int messageSize)
        {
            if (dClientRawPacketList.ContainsKey(clientNumber))
            {
                dClientRawPacketList[clientNumber].AddToList(message, messageSize);

                //Debug.WriteLine("Raw Data From: " + clientNumber.ToString() + ", Size of Packet: " + messageSize.ToString());
                autoEvent.Set();//Fire in the hole
            }
        }
        #endregion

        #region CLIENT CONNECTION PROCESS
        private void NewClientConnected(int ConnectionID)
        {
            try
            {
                Debug.WriteLine($"(RT Client)NewClientConnected: {ConnectionID}");
                OnCommunications($"{GeneralFunction.GetDateTimeFormatted} Incoming Connection {ConnectionID}", INK.CLR_PURPLE);
                if (svr.workerSockets.ContainsKey(ConnectionID))
                {
                    lock (dClientRawPacketList)//http://www.albahari.com/threading/part2.aspx#_Locking
                    {
                        //Add the raw Packet collector
                        if (!dClientRawPacketList.ContainsKey(ConnectionID))
                        {
                            dClientRawPacketList.Add(ConnectionID, new MotherOfRawPackets(ConnectionID));
                        }
                    }

                    SetNewConnectionData_FromThread(ConnectionID);
                }
                else
                {
                    Debug.WriteLine("UNKNOWN CONNECTIONID" + ConnectionID.ToString());
                }
            }
            catch (Exception ex)
            {
                OnCommunications($"EXCEPTION: NewClientConnected on client {ConnectionID}, exception: {ex.Message}", INK.CLR_RED);
            }
        }

        private delegate void SetNewConnectionDataDelegate(int clientNumber);
        private void SetNewConnectionData_FromThread(int clientNumber)
        {
            if(InvokeRequired)
            {
                this.Invoke(new SetNewConnectionDataDelegate(SetNewConnectionData_FromThread), new object[] { clientNumber });
                return;
            }

            try
            {
                lock (svr.workerSockets)
                {
                    /*********************************   Add the data to the Listview  *************************************/
                    ListViewItem li = new ListViewItem(svr.workerSockets[clientNumber].UserSocket.RemoteEndPoint.ToString());
                    li.Name = clientNumber.ToString();//Set the Key as a unique identifier
                    li.Tag = clientNumber;

                    listView1.Items.Add(li);                    //index 0 Clients IP address
                    li.SubItems.Add("Receiving...");            //index 1 Computer name
                    li.SubItems.Add("Receiving...");            //index 2 version
                    li.SubItems.Add(clientNumber.ToString());   //index 3 //Client's ID
                    li.SubItems.Add("Receiving...");            //index 4 Clients Name
                    li.SubItems.Add("...");                     //index 5 Ping time
                    /*******************************************************************************************************/
                }
                if (svr.workerSockets[clientNumber].UserSocket.Connected)
                {
                    OnCommunications($"RequestNewConnectionCredentials from: {clientNumber}", INK.CLR_PURPLE);
                    RequestNewConnectionCredentials(clientNumber);
                }
                else
                {
                    Debug.WriteLine($"ISSUE!!!(RequestNewConnectionCredentials) UserSocket.Connected is FALSE from: {clientNumber}");
                }
            }
            catch (Exception ex)
            {
                OnCommunications($"EXCEPTION: SetNewConnectionData_FromThread on client {clientNumber}, exception: {ex.Message}", INK.CLR_RED);
            }
        }


        private delegate void PostUserCredentialsDelegate(int clientNumber, byte[] message);
        /// <summary>
        /// return bool, TRUE if its a FullClient Connection
        /// </summary>
        /// <param name="clientNumber"></param>
        /// <param name="message"></param>
        private void PostUserCredentials(int clientNumber, byte[] message)
        {
            if (InvokeRequired)
            {
                this.Invoke(new PostUserCredentialsDelegate(PostUserCredentials), new object[] { clientNumber, message });
                return;
            }

            try
            {
                PACKET_DATA IncomingData = new PACKET_DATA();
                IncomingData = (PACKET_DATA)PACKET_FUNCTIONS.ByteArrayToStructure(message, typeof(PACKET_DATA));

                lock (svr.workerSockets)
                {
                    string ComputerName = new string(IncomingData.szStringDataA).TrimEnd('\0');//Station/Computer's name
                    string VersionStr = new string(IncomingData.szStringDataB).TrimEnd('\0');//app version
                    string ClientsName = new string(IncomingData.szStringData150).TrimEnd('\0');//Client's Name

                    listView1.Items[clientNumber.ToString()].SubItems[1].Text = ComputerName;
                    listView1.Items[clientNumber.ToString()].SubItems[2].Text = VersionStr;
                    listView1.Items[clientNumber.ToString()].SubItems[4].Text = ClientsName;

                    if (svr.workerSockets.ContainsKey(clientNumber))
                    {
                        svr.workerSockets[clientNumber].szStationName = ComputerName;

                        svr.workerSockets[clientNumber].szClientName = ClientsName;

                        OnCommunications(string.Format("{0} Registered Connection ({1}) for '{2}' on PC: {3}",
                            GeneralFunction.GetDateTimeFormatted, clientNumber,ClientsName, ComputerName), 
                            INK.CLR_GREEN);

                        SendTheClientListToTheNewClient(clientNumber);
                    }
                }//end lock
                
            }
            catch (Exception ex)
            {
                OnCommunications($"EXCEPTION: PostUserCredentials on client {clientNumber}, exception: {ex.Message}", INK.CLR_RED);
            }


        }
        
        /// <summary>
        /// Send 
        /// </summary>
        /// <param name="clientNumber"></param>
        /// <param name="SendList"></param>
        private void SendTheClientListToTheNewClient(int clientNumber, bool SendList = true)
        {
            if (!svr.workerSockets.ContainsKey(clientNumber))
            {
                //logMessage.ToFile("No key in SendTheClientListToEditor(this is bad)", System.Windows.Forms.Application.StartupPath, "ServerError.txt");
                return;//avoid trouble
            }

            string NewClientIP = string.Empty;
            string NewClientName = string.Empty;
            string NewStationtName = string.Empty;
            UInt16 NewClientPort;
            
            string NewAltIP = string.Empty;
            string clientversion = string.Empty;

            //hang onto the new clients ipaddress to send to the other clients
            lock (svr.workerSockets)
            {
                NewClientIP = ((IPEndPoint)svr.workerSockets[clientNumber].UserSocket.RemoteEndPoint).Address.ToString();
                NewClientName = svr.workerSockets[clientNumber].szClientName;
                NewStationtName = svr.workerSockets[clientNumber].szStationName;
                NewClientPort = svr.workerSockets[clientNumber].UserListentingPort;
                NewAltIP = svr.workerSockets[clientNumber].szAlternateIP;                
            }


            PACKET_CLIENTDATA xdata = new PACKET_CLIENTDATA();
            xdata.Packet_Type = (UInt16)PACKETTYPES.TYPE_ClientData;
            xdata.Data_Type = 0;
            xdata.Packet_Size = (UInt16)Marshal.SizeOf(typeof(PACKET_CLIENTDATA));
            xdata.maskTo = 0;
            xdata.idTo = 0;
            xdata.idFrom = 0;

            if (SendList)//true if this client just connected, then we need the list of ther other connected clients
            {
                lock (svr.workerSockets)
                {
                    foreach (Server.UserSock s in svr.workerSockets.Values)
                    {
                        if (clientNumber != s.iClientID) //Send all of the other connected clients to the one who just connected.
                        {
                            Array.Clear(xdata.szUsersAddress, 0, xdata.szUsersAddress.Length);
                            Array.Clear(xdata.szUserName, 0, xdata.szUserName.Length);
                            Array.Clear(xdata.szStationName, 0, xdata.szStationName.Length);
                            Array.Clear(xdata.szUsersAlternateAddress, 0, xdata.szUsersAlternateAddress.Length);
                            Array.Clear(xdata.szUsersClientVersion, 0, xdata.szUsersClientVersion.Length);

                            string p = s.szClientName;
                            if (p.Length > 49)
                                p.CopyTo(0, xdata.szUserName, 0, 49);
                            else
                                p.CopyTo(0, xdata.szUserName, 0, p.Length);
                            xdata.szUserName[49] = '\0';

                            p = s.szStationName;
                            if (p.Length > 49)
                                p.CopyTo(0, xdata.szStationName, 0, 49);
                            else
                                p.CopyTo(0, xdata.szStationName, 0, p.Length);
                            xdata.szStationName[49] = '\0';

                            string ip = ((IPEndPoint)s.UserSocket.RemoteEndPoint).Address.ToString();
                            ip.CopyTo(0, xdata.szUsersAddress, 0, ip.Length);
                            s.szAlternateIP.CopyTo(0, xdata.szUsersAlternateAddress, 0, s.szAlternateIP.Length);

                            xdata.iClientID = (UInt16)s.iClientID;

                            xdata.ListeningPort = s.UserListentingPort;
                            
                            byte[] byData = PACKET_FUNCTIONS.StructureToByteArray(xdata);

                            svr.SendMessage(clientNumber, byData);
                            Thread.Sleep(10);//set s short delay 
                            //ods.DebugOut("sent out: " + s.iClientID.ToString());
                        }
                    }
                }//end lock
            }

            lock (svr.workerSockets)
            {
                //Go through the list and send to the other clients the arrival or updates of the new client
                foreach (Server.UserSock s in svr.workerSockets.Values)
                {
                    if (clientNumber != s.iClientID)
                    {
                        Array.Clear(xdata.szUsersAddress, 0, xdata.szUsersAddress.Length);
                        Array.Clear(xdata.szUserName, 0, xdata.szUserName.Length);
                        Array.Clear(xdata.szStationName, 0, xdata.szStationName.Length);
                        Array.Clear(xdata.szUsersAlternateAddress, 0, xdata.szUsersAlternateAddress.Length);

                        string p = NewClientName;
                        if (p.Length > 49)
                            p.CopyTo(0, xdata.szUserName, 0, 49);
                        else
                            p.CopyTo(0, xdata.szUserName, 0, p.Length);
                        xdata.szUserName[49] = '\0';

                        p = NewStationtName;
                        if (p.Length > 49)
                            p.CopyTo(0, xdata.szStationName, 0, 49);
                        else
                            p.CopyTo(0, xdata.szStationName, 0, p.Length);
                        xdata.szStationName[49] = '\0';

                        string ip = NewClientIP;
                        ip.CopyTo(0, xdata.szUsersAddress, 0, ip.Length);

                        NewAltIP.CopyTo(0, xdata.szUsersAlternateAddress, 0, NewAltIP.Length);
                        clientversion.CopyTo(0, xdata.szUsersClientVersion, 0, clientversion.Length);

                        xdata.iClientID = (UInt16)clientNumber;
                        xdata.ListeningPort = NewClientPort;

                        byte[] byData = PACKET_FUNCTIONS.StructureToByteArray(xdata);

                        svr.SendMessage(s.iClientID, byData);
                        //Thread.Sleep(10);
                    }
                }
            }//end lock
        }



        private void ClientDisconnect(int clientNumber)
        {
            if (ServerIsExiting)
                return;

            /*******************************************************/
            lock (dClientRawPacketList)//Make sure we don't do this twice
            {
                if (!dClientRawPacketList.ContainsKey(clientNumber))
                {
                    lock (svr.workerSockets)
                    {
                        if (!svr.workerSockets.ContainsKey(clientNumber))
                        {
                            return;
                        }
                    }
                }
            }
            /*******************************************************/
            
            try
            {
                RemoveClient_FromThread(clientNumber);
            }
            catch (Exception ex)
            {
                OnCommunications($"EXCEPTION: ClientDisconnect on client {clientNumber}, exception: {ex.Message}", INK.CLR_RED);
            }

            CleanupDeadClient(clientNumber);

        
            Thread.Sleep(10);
        }

        private void RemoveClient_FromThread(int clientNumber)
        {
            try
            {
                SendMessageOfClientDisconnect(clientNumber);
                OnCommunications(string.Format("{0} - {1} has disconnected", GeneralFunction.GetDateTimeFormatted, clientNumber), INK.CLR_BLUE);
            }
            catch (Exception ex)
            {
                OnCommunications($"EXCEPTION: RemoveClient_FromThread on client {clientNumber}, Exception: {ex.Message}", INK.CLR_RED);
            }
        }
        #endregion

        #region Packet factory Processing from clients
        private void NormalizeThePackets()
        {
            if (svr == null)
                return;

            while (svr.IsListening)
            {
                autoEvent.WaitOne(10000);//wait at mutex until signal, or drop through every 10 seconds for fun

                /**********************************************/
                lock (dClientRawPacketList)//http://www.albahari.com/threading/part2.aspx#_Locking
                {
                    foreach (MotherOfRawPackets MRP in dClientRawPacketList.Values)
                    {
                        if (MRP.GetItemCount.Equals(0))
                            continue;
                        try
                        {
                            byte[] packetplayground = new byte[11264];//good for 10 full packets(10240) + 1 remainder(1024)
                            RawPackets rp;

                            int actualPackets = 0;

                            while (true)
                            {
                                if (MRP.GetItemCount == 0)
                                    break;

                                int holdLen = 0;

                                if (MRP.bytesRemaining > 0)
                                    Copy(MRP.Remainder, 0, packetplayground, 0, MRP.bytesRemaining);

                                holdLen = MRP.bytesRemaining;

                                for (int i = 0; i < 10; i++)//only go through a max of 10 times so there will be room for any remainder
                                {
                                    rp = MRP.GetTopItem;//dequeue

                                    Copy(rp.dataChunk, 0, packetplayground, holdLen, rp.iChunkLen);

                                    holdLen += rp.iChunkLen;

                                    if (MRP.GetItemCount.Equals(0))//make sure there is more in the list befor continuing
                                        break;
                                }

                                actualPackets = 0;

                                #region PACKET_SIZE 1024
                                if (holdLen >= 1024)//make sure we have at least one packet in there
                                {
                                    actualPackets = holdLen / 1024;
                                    MRP.bytesRemaining = holdLen - (actualPackets * 1024);

                                    for (int i = 0; i < actualPackets; i++)
                                    {
                                        byte[] tmpByteArr = new byte[1024];
                                        Copy(packetplayground, i * 1024, tmpByteArr, 0, 1024);
                                        lock (FullPacketList)
                                            FullPacketList.Enqueue(new FullPacket(MRP.iListClientID, tmpByteArr));
                                    }
                                }
                                else
                                {
                                    MRP.bytesRemaining = holdLen;
                                }

                                //hang onto the remainder
                                Copy(packetplayground, actualPackets * 1024, MRP.Remainder, 0, MRP.bytesRemaining);
                                #endregion

                                if (FullPacketList.Count > 0)
                                    autoEvent2.Set();

                            }//end of while(true)
                        }
                        catch (Exception ex)
                        {
                            MRP.ClearList();//pe 03-20-2013
                            string msg = (ex.InnerException == null) ? ex.Message : ex.InnerException.Message;
                            
                            OnCommunications("EXCEPTION in  NormalizeThePackets - " + msg, INK.CLR_RED);
                        }
                    }//end of foreach (dClientRawPacketList)
                }//end of lock
                /**********************************************/
                if (ServerIsExiting)
                    break;
            }//Endof of while(svr.IsListening)

            Debug.WriteLine("Exiting the packet normalizer");
            OnCommunications("Exiting the packet normalizer", INK.CLR_RED);
        }

        private void ProcessReceivedData()
        {
            if (svr == null)
                return;

            while (svr.IsListening)
            {
                //Debug.WriteLine("Before AutoEvent");
                autoEvent2.WaitOne();//wait at mutex until signal
                //Debug.WriteLine("After AutoEvent");

                try
                {
                    while (FullPacketList.Count > 0)
                    {
                        FullPacket fp;
                        lock (FullPacketList)
                            fp = FullPacketList.Dequeue();
                        //Console.WriteLine(GetDateTimeFormatted +" - Full packet fromID: " + fp.iFromClient.ToString() + ", Type: " + ((PACKETTYPES)fp.ThePacket[0]).ToString());
                        UInt16 type = (ushort)(fp.ThePacket[1] << 8 | fp.ThePacket[0]);
                        switch (type)//Interrigate the first 2 Bytes to see what the packet TYPE is
                        {
                            case (UInt16)PACKETTYPES.TYPE_MyCredentials:
                                {
                                    PostUserCredentials(fp.iFromClient, fp.ThePacket);
                                    SendRegisteredMessage(fp.iFromClient, fp.ThePacket);
                                }
                                break;
                            case (UInt16)PACKETTYPES.TYPE_CredentialsUpdate:
                                break;
                            case (UInt16)PACKETTYPES.TYPE_PingResponse:
                                //Debug.WriteLine(DateTime.Now.ToShortDateString() + ", " + DateTime.Now.ToLongTimeString() + " - Received Ping from: " + fp.iFromClient.ToString() + ", on " + DateTime.Now.ToShortDateString() + ", at: " + DateTime.Now.ToLongTimeString());
                                UpdateTheConnectionTimers(fp.iFromClient, fp.ThePacket);
                                break;
                            case (UInt16)PACKETTYPES.TYPE_Close:
                                ClientDisconnect(fp.iFromClient);
                                break;
                            case (UInt16)PACKETTYPES.TYPE_Message:
                                {
                                    AssembleMessage(fp.iFromClient, fp.ThePacket);
                                }
                                break;
                            default:
                                PassDataThru(type, fp.iFromClient, fp.ThePacket);
                                break;
                        }
                    }//END  while (FullPacketList.Count > 0)
                }//try
                catch (Exception ex)
                {
                    try
                    {
                        string msg = (ex.InnerException == null) ? ex.Message : ex.InnerException.Message;
                        OnCommunications($"EXCEPTION in  ProcessRecievedData - {msg}", INK.CLR_RED);
                    }
                    catch { }
                }

                if (ServerIsExiting)
                    break;
            }//End while (svr.IsListening)

            string info2 = string.Format("AppIsExiting = {0}", ServerIsExiting.ToString());
            string info3 = string.Format("Past the ProcessRecievedData loop");

            Debug.WriteLine(info2);
            Debug.WriteLine(info3);

            try
            {
                OnCommunications(info3, INK.CLR_RED);// "Past the ProcessRecievedData loop" also is logged to InfoLog.log
                
            }
            catch { }

            if (!ServerIsExiting)
            {
                //if we got here then something went wrong, we need to shut down the service
                OnCommunications("SOMETHING CRASHED", INK.CLR_RED);
            }
        }
        
        private void PassDataThru(UInt16 type, int MessageFrom, byte[] message)
        {
            try
            {
                int ForwardTo = 0;
                switch (type)
                {
                    case (UInt16)PACKETTYPES.TYPE_FileStart:
                    case (UInt16)PACKETTYPES.TYPE_FileChunk:
                    case (UInt16)PACKETTYPES.TYPE_FileEnd:
                        {
                            /*********************************************************************************************************/
                            // Bitshift the messages 2nd and 3rd bits, which are the 'idTo' for these message types
                            // which contains the hostID of the client this message will be forwarded to.
                            ForwardTo = (ushort)(message[3] << 8 | message[2]);
                        }
                        break;
                    default:
                        {
                            /*********************************************************************************************************/
                            // Bitshift the messages 8th, 9th, 10th and 11th bits, which are the 'idTo' 
                            // which contains the hostID of the client this message will be forwarded to.
                            ForwardTo = (int)message[11] << 24 | (int)message[10] << 16 | (int)message[9] << 8 | (int)message[8];

                            /*********************************************************************************************************/

                            /*********************************************************************************************************/
                            //Then take the sending clients HostID and stuff in who this packet's 'idFrom' so we know who sent it!
                            byte[] x = BitConverter.GetBytes(MessageFrom);
                            message[12] = (byte)x[0];//idFrom
                            message[13] = (byte)x[1];//idFrom
                            message[14] = (byte)x[2];//idFrom
                            message[15] = (byte)x[3];//idFrom
                            /*********************************************************************************************************/
                        }
                        break;
                }

                if (ForwardTo > 0)
                    svr.SendMessage(ForwardTo, message);
                else
                    svr.SendMessage(message);
            }
            catch (Exception ex)
            {
                string msg = (ex.InnerException == null) ? ex.Message : ex.InnerException.Message;
                OnCommunications($"EXCEPTION in  PassDataThru - {msg}", INK.CLR_RED);
            }
        }

        #endregion

        private void AssembleMessage(int clientID, byte[] message)
        {
            try
            {
                PACKET_DATA IncomingData = new PACKET_DATA();
                IncomingData = (PACKET_DATA)PACKET_FUNCTIONS.ByteArrayToStructure(message, typeof(PACKET_DATA));

                switch(IncomingData.Data_Type)
                {
                    case (UInt16)PACKETTYPES_SUBMESSAGE.SUBMSG_MessageStart:
                        {
                            if (svr.workerSockets.ContainsKey(clientID))
                            {
                                if (IncomingData.idTo == 0)// message meant for the server
                                {
                                    OnCommunications(
                                        $"Client '{svr.workerSockets[clientID].szClientName}' sent some numbers and some text... num1= {IncomingData.Data16} and num2= {IncomingData.Data17}:",
                                        INK.CLR_BLUE);
                                    OnCommunications($"Client also said:", INK.CLR_BLUE);

                                    OnCommunications(new string(IncomingData.szStringDataA).TrimEnd('\0'), INK.CLR_GREEN);
                                }
                                else
                                    svr.SendMessage((int)IncomingData.idTo, message);
                            }
                        }
                        break;
                    case (UInt16)PACKETTYPES_SUBMESSAGE.SUBMSG_MessageGuts:
                        {
                            if (svr.workerSockets.ContainsKey(clientID))
                            {
                                if (IncomingData.idTo == 0)// message meant for the server
                                {
                                    OnCommunications(new string(IncomingData.szStringDataA).TrimEnd('\0'), INK.CLR_GREEN);
                                }
                                else
                                    svr.SendMessage((int)IncomingData.idTo, message);
                            }
                        }
                        break;
                    case (UInt16)PACKETTYPES_SUBMESSAGE.SUBMSG_MessageEnd:
                        {
                            if (svr.workerSockets.ContainsKey(clientID))
                            {
                                if (IncomingData.idTo == 0)// message meant for the server
                                {
                                    OnCommunications("FINISHED GETTING MESSAGE", INK.CLR_BLUE);

                                    /****************************************************************/
                                    //Now tell the client the message was received!
                                    PACKET_DATA xdata = new PACKET_DATA();

                                    xdata.Packet_Type = (UInt16)PACKETTYPES.TYPE_MessageReceived;

                                    byte[] byData = PACKET_FUNCTIONS.StructureToByteArray(xdata);

                                    svr.SendMessage(clientID, byData);
                                }
                                else
                                    svr.SendMessage((int)IncomingData.idTo, message);
                            }
                        }
                        break;
                }
            }
            catch
            {
                Console.WriteLine("ERROR Assembling message");
            }
        }

        private void UpdateTheConnectionTimers(int clientNumber, byte[] message)
        {
            lock (svr.workerSockets)
            {
                try
                {
                    if (svr.workerSockets.ContainsKey(clientNumber))
                    {
                        svr.workerSockets[clientNumber].dTimer = DateTime.Now;
                        Int64 elapsedTime = svr.workerSockets[clientNumber].PingStatClass.StopTheClock();
                        //Console.WriteLine("UpdateTheConnectionTimers: " + ConnectionID.ToString());
                        //Debug.WriteLine("Ping Time for " + ConnectionID.ToString() + ": " + elapsedTime.ToString() + "ms");

                        PACKET_DATA IncomingData = new PACKET_DATA();
                        IncomingData = (PACKET_DATA)PACKET_FUNCTIONS.ByteArrayToStructure(message, typeof(PACKET_DATA));
                        
                        Console.WriteLine($"{GeneralFunction.GetDateTimeFormatted}: Ping From Server to client: {elapsedTime}ms");

                        UpdateThePingTimeFromThread(clientNumber, elapsedTime);
                        /****************************************************************************************/

                    }
                }
                catch(Exception ex)
                {
                    string msg = (ex.InnerException == null) ? ex.Message : ex.InnerException.Message;
                    OnCommunications($"EXCEPTION in UpdateTheConnectionTimers - {msg}", INK.CLR_RED);
                }
            }
        }

        private delegate void UpdateThePingTimeFromThreadDelegate(int clientNumber, long elapsedTimeInMilliseconds);
        private void UpdateThePingTimeFromThread(int clientNumber, long elapsedTimeInMilliseconds)
        {
            if(InvokeRequired)
            {
                this.Invoke(new UpdateThePingTimeFromThreadDelegate(UpdateThePingTimeFromThread), new object[] { clientNumber, elapsedTimeInMilliseconds });
                return;
            }

            listView1.Items[clientNumber.ToString()].SubItems[5].Text = string.Format("{0:0.##}ms", elapsedTimeInMilliseconds);
        }

        private void SetHostNameAndAddress()
        {
            string strHostName = Dns.GetHostName();

            labelMyIP.Text = $"Host Name: {strHostName}, Listening on Port: {MyPort}";
            
            IPAddress[] ips = Dns.GetHostAddresses(strHostName);//.GetValue(0).ToString();

            foreach (IPAddress ip in ips)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    listBox1.Items.Add(ip.ToString());
                else
                    listBoxUnusedIPs.Items.Add(ip.ToString());
            }
        }

        private void CheckOnApplicationDirectory()
        {
            try
            {
                string AppPath = GeneralFunction.GetAppPath;

                if (!Directory.Exists(AppPath))
                {
                    Directory.CreateDirectory(AppPath);
                }
            }
            catch (Exception ex)
            {
                string msg = (ex.InnerException == null) ? ex.Message : ex.InnerException.Message;
                OnCommunications($"EXCEPTION: ISSUE CREATING A DIRECTORY - {msg}", INK.CLR_RED);
            }
        }

        #region PACKET MESSAGES
        private void RequestNewConnectionCredentials(int ClientID)
        {
            try
            {
                PACKET_DATA xdata = new PACKET_DATA();

                xdata.Packet_Type = (UInt16)PACKETTYPES.TYPE_RequestCredentials;
                xdata.Data_Type = 0;
                xdata.Packet_Size = 16;
                xdata.maskTo = 0;
                xdata.idTo = (UInt16)ClientID;
                xdata.idFrom = 0;

                xdata.DataLong1 = DateTime.UtcNow.Ticks;

                if (!svr.workerSockets.ContainsKey(ClientID))
                    return;

                lock (svr.workerSockets)
                {
                    //ship back their address for reference to the client
                    string clientAddr = ((IPEndPoint)svr.workerSockets[ClientID].UserSocket.RemoteEndPoint).Address.ToString();
                    clientAddr.CopyTo(0, xdata.szStringDataA, 0, clientAddr.Length);

                    byte[] byData = PACKET_FUNCTIONS.StructureToByteArray(xdata);

                    if (svr.workerSockets[ClientID].UserSocket.Connected)
                    {
                        svr.SendMessage(ClientID, byData);
                        Debug.WriteLine(DateTime.Now.ToShortDateString() + ", " + DateTime.Now.ToLongTimeString() + " - from " + ClientID.ToString());
                    }
                }
            }
            catch { }
        }

        private void SendMessageOfClientDisconnect(int clientId)
        {
            try
            {
                PACKET_DATA xdata = new PACKET_DATA();

                xdata.Packet_Type = (UInt16)PACKETTYPES.TYPE_ClientDisconnecting;
                xdata.Data_Type = 0;
                xdata.Packet_Size = (UInt16)Marshal.SizeOf(typeof(PACKET_DATA));
                xdata.maskTo = 0;
                xdata.idTo = 0;
                xdata.idFrom = (UInt32)clientId;

                byte[] byData = PACKET_FUNCTIONS.StructureToByteArray(xdata);
                svr.SendMessage(byData);
            }
            catch { }
        }

        private void SendRegisteredMessage(int clientId, byte[] message)
        {
            PACKET_DATA IncomingData = new PACKET_DATA();
            IncomingData = (PACKET_DATA)PACKET_FUNCTIONS.ByteArrayToStructure(message, typeof(PACKET_DATA));

            try
            {
                PACKET_DATA xdata = new PACKET_DATA();

                xdata.Packet_Type = (UInt16)PACKETTYPES.TYPE_Registered;
                xdata.Data_Type = 0;
                xdata.Packet_Size = (UInt16)Marshal.SizeOf(typeof(PACKET_DATA));
                xdata.maskTo = 0;
                xdata.idTo = 0;
                xdata.idFrom = (UInt32)clientId;

                xdata.Data6 = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.Major;
                xdata.Data7 = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.Minor;
                xdata.Data8 = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.Build;
                //xdata.Data9 = MySettings.CurrentServiceFeatureVer;
                
                byte[] byData = PACKET_FUNCTIONS.StructureToByteArray(xdata);
                svr.SendMessage(byData);
            }
            catch { }

        }
        #endregion

        #region WEB CONTROL
        private bool BrowserVersion()
        {
            Debug.WriteLine("CommunicationsDisplay.Version: " + CommunicationsDisplay.Version.ToString());

            if (CommunicationsDisplay.Version.Major < 9)
            {
                MessageBox.Show(this, "You must update your web browser to Internet Explorer 9 or greater to see the service output information!", "Message", MessageBoxButtons.OK);
                return false;
            }

            return true;
        }
        
        private delegate void OnCommunicationsDelegate(string str, INK iNK);
        private void OnCommunications(string str, INK iNK)
        {
            if (ValidBrowser == false)
            {
                System.Diagnostics.Debug.WriteLine("INVALID BROWSER, must update Internet Explorer to version 8 or better!!");
                return;
            }
            Int32 line = 0;
            //System.Diagnostics.Debug.WriteLine("~~~~~~ OnCommunications 1:");
            try
            {
                if (InvokeRequired)
                {
                    this.Invoke(new OnCommunicationsDelegate(OnCommunications), new object[] { str, iNK });
                    return;
                }

                //  System.Diagnostics.Debug.WriteLine("~~~~~~ OnCommunications 2");
                HtmlDocument doc = CommunicationsDisplay.Document;
                line = 1;
                //System.Diagnostics.Debug.WriteLine("~~~~~~ OnCommunications 3");
                string style = String.Empty;
                if (iNK.Equals(INK.CLR_GREEN))
                    style = Properties.Settings.Default.StyleGreen;
                else if (iNK.Equals(INK.CLR_BLUE))
                    style = Properties.Settings.Default.StyleBlue;
                else if (iNK.Equals(INK.CLR_RED))
                    style = Properties.Settings.Default.StyleRed;
                else if (iNK.Equals(INK.CLR_PURPLE))
                    style = Properties.Settings.Default.StylePurple;
                else
                    style = Properties.Settings.Default.StyleBlack;
                line = 2;
                //System.Diagnostics.Debug.WriteLine("~~~~~~ OnCommunications 4");
                //doc.Write(String.Format("<div style=\"{0}\">{1}</div><br />", style, str));
                doc.Write(String.Format("<div style=\"{0}\">{1}</div>", style, str));
                //doc.Body.ScrollTop = int.MaxValue;
                //CommunicationsDisplay.Document.Window.ScrollTo(0, int.MaxValue);
                line = 3;
                ScrollMessageIntoView();
                //System.Diagnostics.Debug.WriteLine("~~~~~~ OnCommunications 5");
                line = 4;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EXCEPTION IN OnCommunications @ Line: {line}, {ex.Message}");
            }
        }

        /// <summary>
        /// force the web control to the last item in the window... set to the bottom for the latest activity
        /// </summary>
        private void ScrollMessageIntoView()
        {
            // MOST IMP : processes all windows messages queue
            System.Windows.Forms.Application.DoEvents();

            if (CommunicationsDisplay.Document != null)
            {
                CommunicationsDisplay.Document.Window.ScrollTo(0, CommunicationsDisplay.Document.Body.ScrollRectangle.Height);
            }
        }

        private void ClearEventAndStatusDisplays()
        {
            // Clear communications
            displayReady = false;
            CommunicationsDisplay.Navigate("about:blank");
            while (!displayReady)
            {
                Application.DoEvents();
            }
        }

        private void CommunicationsDisplay_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            //Debug.WriteLine("CommunicationsDisplay_Navigated");
            //OnCommunications("........", INK.CLR_BLACK);
            displayReady = true;
        }
        #endregion

        #region UNSAFE CODE
        // The unsafe keyword allows pointers to be used within the following method:
        static unsafe void Copy(byte[] src, int srcIndex, byte[] dst, int dstIndex, int count)
        {
            try
            {
                if (src == null || srcIndex < 0 || dst == null || dstIndex < 0 || count < 0)
                {
                    Console.WriteLine("Serious Error in the Copy function 1");
                    throw new System.ArgumentException();
                }

                int srcLen = src.Length;
                int dstLen = dst.Length;
                if (srcLen - srcIndex < count || dstLen - dstIndex < count)
                {
                    Console.WriteLine("Serious Error in the Copy function 2");
                    throw new System.ArgumentException();
                }

                // The following fixed statement pins the location of the src and dst objects
                // in memory so that they will not be moved by garbage collection.
                fixed (byte* pSrc = src, pDst = dst)
                {
                    byte* ps = pSrc + srcIndex;
                    byte* pd = pDst + dstIndex;

                    // Loop over the count in blocks of 4 bytes, copying an integer (4 bytes) at a time:
                    for (int i = 0; i < count / 4; i++)
                    {
                        *((int*)pd) = *((int*)ps);
                        pd += 4;
                        ps += 4;
                    }

                    // Complete the copy by moving any bytes that weren't moved in blocks of 4:
                    for (int i = 0; i < count % 4; i++)
                    {
                        *pd = *ps;
                        pd++;
                        ps++;
                    }
                }
            }
            catch (Exception ex)
            {
                var exceptionMessage = (ex.InnerException != null) ? ex.InnerException.Message : ex.Message;
                Debug.WriteLine("EXCEPTION IN: Copy - " + exceptionMessage);
            }

        }
        #endregion
        
    }
}
