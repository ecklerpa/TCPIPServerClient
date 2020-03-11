using System;
using System.Collections.Generic;
using System.Net;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.IO;
using TCPClient;

namespace TCPIPClient
{
    public class UserClients
    {
        public UserClients( string szClientsIP, ushort iClientPort, string szUserName,
            int serverID, string szClientsAltIP, string pcName)
        {
            _ClientsIP = IPAddress.Parse( szClientsIP );
            _HostServerID = serverID;
            _szUserName = szUserName;
            _Port = iClientPort;//22043;

            _szAltIp = szClientsAltIP;
            szStationName = pcName;
            _FileTransferInTransit = 0;
        }        

        public IPAddress ClientsIP { get { return _ClientsIP; } }
        public string szUserName { get { return _szUserName; } set { _szUserName = value; } }
        public string szStationName { get; set; }
        public int HostServerID { get { return _HostServerID; } }
        public int Port { get { return _Port; } }

        public Int16 FileTransferInTransit { get { return _FileTransferInTransit; } set { _FileTransferInTransit = value; } }
        
        public string szAltIp { get { return _szAltIp; } set { _szAltIp = value; } }

        public bool FailedOnSend
        {
            get;
            set;
        }
        public bool UseAltIP
        {
            get
            {
                if (szAltIp.Length > 0)
                    return _UseAltIP;
                else
                    return false;
            } 
            set
            {
                if (szAltIp.Length > 0)
                    _UseAltIP = value;
                else
                    _UseAltIP = false;
            }
        }
        
        private int _HostServerID;
        private string _szUserName;
        private IPAddress _ClientsIP;
        private int _Port;
        
        
        private long _LastStatusTime;//time in NanoSeconds from Midnight 01-01-0001
        private string _szAltIp;
        private bool _UseAltIP;//Uses the alternate address specified by the client for a P2P connection
        private Int16 _FileTransferInTransit;//true while data is enroute to this user        

        public override string ToString()
        {
            return _szUserName;
        }
    }

    
    /*****************************************************************************************************/
    class FilesToSend
    {
        public FilesToSend(UInt16 fileID, string szPathAndFile, int sendTo)
        {
            _szPathAndFile = szPathAndFile;
            _SendTo = sendTo;
            _FileId = fileID;
        }

        public UInt16 FileId { get { return _FileId; } }
        public string szPathAndFile { get { return _szPathAndFile; } }
        public int sendTo { get { return _SendTo; } }

        private UInt16 _FileId;
        private string _szPathAndFile;
        private int _SendTo;
    }

    class RTFGuts
    {
        public RTFGuts(int sizeofguts, char[] theguts)
        { 
            _guts = new char[sizeofguts];
            _guts = theguts;
            _iSizeOfGuts = sizeofguts;
        }

        public char [] guts { get { return _guts; } }
        public int iSizeOfGuts { get { return _iSizeOfGuts; } }

        private char [] _guts;
        private int _iSizeOfGuts;
    }

    class RTFBody
    {
        public RTFBody(int idFromClient, UInt32 sizeoffile, UInt16 RTFID)
        {   
            _iSizOfRTF = sizeoffile;
            _iRTFID = RTFID;
            _iFromClientId = idFromClient;
            _RTFGutsList = new Queue<RTFGuts>();

        }
        ~RTFBody()
        {
            _RTFGutsList.Clear();
        }

        public int iFromClientId { get { return _iFromClientId; } }
        public int KeyID { get { return _iRTFID; } }

        public void AddToRTFGuts(RTFGuts fg)
        {
            lock(_RTFGutsList)
                _RTFGutsList.Enqueue(fg);
        }

        public RTFGuts PopAGut
        {
            get
            {
                RTFGuts rg;
                lock (_RTFGutsList)
                    rg = _RTFGutsList.Dequeue();
                return rg;
            }
        }

        public int GetGutCount()
        {
            return _RTFGutsList.Count;
        }

        private UInt32 _iSizOfRTF;
        private UInt16 _iRTFID;
        private int _iFromClientId;
        
        private Queue<RTFGuts> _RTFGutsList;
    }

    class FileGuts
    {
        public FileGuts(int sizeofguts, byte [] theguts)
        { 
            _guts = new byte[sizeofguts];
            _guts = theguts;
            _iSizeOfGuts = sizeofguts;
        }

        public byte [] guts { get { return _guts; } }
        public int iSizeOfGuts { get { return _iSizeOfGuts; } }

        private byte [] _guts;
        private int _iSizeOfGuts;
    }
    
    class FileBody
    {
        public FileBody(int idFromClient, string szFileName, UInt32 sizeoffile, UInt16 fileID, string szAppFilePath)
        {
            _fileCreateInProgress = true;
            _szFileName = szFileName;
            _iSizOfFile = sizeoffile;
            _iFileID = fileID;
            _iFromClientId = idFromClient;
            _FileGutsList = new Queue<FileGuts>();
            _stillInThread = true;
            _fileGutCounter = 0;
            _appFilePath = szAppFilePath;

            autoEventFileGuts = new AutoResetEvent(false);//the guts mutex
            autoEventFinished = new AutoResetEvent(false);//the finished mutex
            ProcessTempFile = new Thread(new ThreadStart(DoTempFile));
            ProcessTempFile.Name = "file_" + szFileName;//name the thread
            ProcessTempFile.Start();

            if (sizeoffile > 35000)
                _progBar = new Progress(szFileName, (long)sizeoffile, (int)FILEACTION.IncomingFile);

            //create a watchdog timer
            TimeOut = 0;
            _packetTimer = new System.Timers.Timer();
            _packetTimer.Interval = 10000;//10 sec 
            _packetTimer.Elapsed += new System.Timers.ElapsedEventHandler(_packetTimer_Elapsed);
            _packetTimer.Enabled = true;
        }

        public int iFromClientId { get { return _iFromClientId; } }
        public string szFileName { get { return _szFileName; } }
        public int KeyID { get { return _iFileID; } }

        public void ClearList()
        {
            lock (_FileGutsList)
                _FileGutsList.Clear();
        }

        public void AddToFileGuts(FileGuts fg)
        {
            lock (_FileGutsList)
                _FileGutsList.Enqueue(fg);


            if (_progBar != null)
            {
                System.Windows.Forms.Application.DoEvents();

                //_progBar.SetProgressbarValue(++_fileGutCounter * 4096);
                _progBar.SetProgressbarValue(++_fileGutCounter * 1024);
            }

            if (_FileGutsList.Count > 100)
                if (autoEventFileGuts != null)
                    autoEventFileGuts.Set();

            TimeOut = 0;
        }

        private void DoTempFile()
        {
            Console.WriteLine("\n------------------ IN DoTempFile");
            try
            {
                //fin = new FileStream(System.Windows.Forms.Application.StartupPath + "\\" + _iFileID.ToString() + _szFileName + ".TMP", FileMode.Create, FileAccess.Write);
                _tmpFilename = Path.Combine(_appFilePath, string.Format("{0}{1}.TMP", _iFileID, _szFileName));
                fin = new FileStream(_tmpFilename, FileMode.Create, FileAccess.Write);
                if (fin != null)
                    fin.Seek(0, SeekOrigin.Begin);


                while (_fileCreateInProgress)
                {
                    //Console.WriteLine("------------------ BefORE WAIT");
                    autoEventFinished.Set();//signals the TransferCompleted function that we have made it here
                    autoEventFileGuts.WaitOne();//wait at mutex until signal
                    //Console.WriteLine("------------------ AFTER WAIT");

                    if (fin != null)
                    {
                        while (_FileGutsList.Count > 0)
                        {
                            FileGuts fg = null;
                            lock(_FileGutsList)
                                fg = _FileGutsList.Dequeue();

                            fin.Write(fg.guts, 0, fg.iSizeOfGuts);
                        }
                    }
                }

                if (fin != null)
                {
                    fin.Close();
                    fin.Dispose();
                    fin = null;
                }
                else
                {
                    //logMessage.ToFile("Error creating file: " + _szFileName, _appFilePath, "ErrorLog.txt");
                }
            }
            catch (Exception ex)
            {
                string msg = (ex.InnerException != null) ? ex.InnerException.Message : ex.Message;
                System.Diagnostics.Debug.WriteLine("ERROR in DoTempFile()\n" + msg);
            }
            finally
            {
                _stillInThread = false;
            }
        }

        internal string TransferCompleted(string FinalPathAndFileName)
        {
            Console.WriteLine("1 ProcessTempFile.IsAlive = " + ProcessTempFile.IsAlive.ToString());

            //Wait here till we get the 'Set' from the thread. Files that are really small will blow past
            //this function before the threaded 'DoTempFile' funtion ever gets set if we dont wait.
            autoEventFinished.WaitOne();//genius on my part... did I spell genius right?

            _fileCreateInProgress = false;

            if (autoEventFileGuts != null)
                autoEventFileGuts.Set();

            while (_stillInThread)//wait till we know the temp file is closed
            {
                Thread.Sleep(5);
            }

            bool WeNeedAnewFileName = false;
            try
            {
                if (File.Exists(FinalPathAndFileName))//this will only happen for template type files and shedule type files, the
                    File.Delete(FinalPathAndFileName);//File.Move function below will throw an exception if that file already exixts.
            }
            catch(Exception ex)
            {
                //we couldnt remove an existing file....I'd assume it was open so lets chand the name of the file
                string msg = (ex.InnerException != null) ? ex.InnerException.Message : ex.Message;
                System.Diagnostics.Debug.WriteLine(string.Format("\nERROR in TransferCompleted:\n{0}\n", msg));

                WeNeedAnewFileName = true;
            }

            if (WeNeedAnewFileName)
            {
                int c = 1;
                string dir = Path.GetDirectoryName(FinalPathAndFileName);
                string FileName = Path.GetFileNameWithoutExtension(FinalPathAndFileName);
                string ext = Path.GetExtension(FinalPathAndFileName);

                while (File.Exists(FinalPathAndFileName))
                {
                    if (c > 1)
                    {
                        try //try deleting the previous file that was here
                        {
                            if (File.Exists(FinalPathAndFileName))
                                File.Delete(FinalPathAndFileName);
                        }
                        catch { }
                    }

                    string file = string.Format("{0}({1})", FileName, c);
                    string newFileName = string.Format("{0}{1}", file, ext);
                    FinalPathAndFileName = Path.Combine(dir, newFileName);
                    
                    c++;
                }
            }

            
            //string filename = Path.Combine(_appFilePath, string.Format("{0}{1}.TMP", _iFileID, _szFileName));
            if (File.Exists(_tmpFilename))
            {
                File.Move(_tmpFilename, FinalPathAndFileName);
            }
            else
                Console.WriteLine("FILE NOT FOUND: " + _tmpFilename);

            Console.WriteLine("2 ProcessTempFile.IsAlive = " + ProcessTempFile.IsAlive.ToString());

            CleanupClass();

            return FinalPathAndFileName;
        }

        //Every 10 seconds
        private void _packetTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (++TimeOut >= 6)
            {
                TimeOut = 0;

                //No packets within 60 seconds so something happened
                DoEmergencyAbort();
            }

            Console.WriteLine("_packetTimer_Elapsed = " + TimeOut.ToString());
        }

        public void DoEmergencyAbort()
        {
            _fileCreateInProgress = false;

            lock(_FileGutsList)
                _FileGutsList.Clear();

            if (autoEventFileGuts != null)
                autoEventFileGuts.Set();

            Thread.Sleep(100);

            KillProgressBar();

            _stillInThread = false;

            if (ProcessTempFile != null)
            {
                int c = 0;
                while (ProcessTempFile.IsAlive)
                {
                    Thread.Sleep(10);

                    if (c++ > 10)
                        break;
                }

                if (ProcessTempFile.IsAlive)
                    ProcessTempFile.Abort();

            }

            ProcessTempFile = null;

            CleanupClass();
        }

        private void CleanupClass()
        {
            try
            {
                if (_packetTimer != null)
                {
                    _packetTimer.Elapsed -= _packetTimer_Elapsed;
                    _packetTimer.Stop();
                    _packetTimer.Close();
                    _packetTimer.Dispose();
                    _packetTimer = null;
                }

                if (autoEventFileGuts != null)
                {
                    autoEventFileGuts.Close();
                    //autoEventFileGuts.Dispose();
                    autoEventFileGuts = null;
                }

                if (autoEventFinished != null)
                {
                    autoEventFinished.Close();
                    //autoEventFinished.Dispose();
                    autoEventFinished = null;
                }

            }
            catch { }
        }

        public void KillProgressBar()
        {
            if (_progBar != null)
            {
                _progBar.Close();
                _progBar = null;
            }
        }

        public FileGuts PopAGut
        {
            get
            {
                FileGuts fg;
                lock (_FileGutsList)
                    fg = _FileGutsList.Dequeue();
                return fg;
            }
        }

        public int GetGutCount
        {
            get { return _FileGutsList.Count; }
        }

        private string _appFilePath;
        private string _szFileName;
        private string _tmpFilename;
        private UInt32 _iSizOfFile;
        private UInt16 _iFileID;
        private int _iFromClientId;
        private Progress _progBar = null;
        private Queue<FileGuts> _FileGutsList;
        private bool _fileCreateInProgress;
        private bool _stillInThread;

        private FileStream fin = null;
        private AutoResetEvent autoEventFileGuts = null;//mutex 
        private AutoResetEvent autoEventFinished = null;//mutex 
        private Thread ProcessTempFile = null;

        private System.Timers.Timer _packetTimer = null;
        private byte TimeOut;
        private int _fileGutCounter;

    }


    class MotherOfRawPackets
    {
        public MotherOfRawPackets(int List_ClientID)
        {
            _iListClientID = List_ClientID;
            _RawPacketsList = new Queue<RawPackets>();
            _Remainder = new byte[1024];
            _bytesRemaining = 0;
        }

        public int iListClientID { get { return _iListClientID; } }
        public int bytesRemaining { get { return _bytesRemaining; } set { _bytesRemaining = value; } }
        public byte[] Remainder { get { return _Remainder; } set { _Remainder = value; } }


        /***************** List operations ********************************************/
        public void AddToList(byte[] data, int SizeOfChunk)
        {
            lock (_RawPacketsList)
                _RawPacketsList.Enqueue(new RawPackets(_iListClientID, data, SizeOfChunk));
        }
        public void ClearList()
        {
            //_RawPacketsList.TrimExcess();
            lock (_RawPacketsList)
                _RawPacketsList.Clear();
        }

        public RawPackets GetTopItem
        {
            get
            {
                RawPackets rp;
                lock (_RawPacketsList)
                    rp = _RawPacketsList.Dequeue();
                return rp;
            }
        }

        public int GetItemCount
        {
            get { return _RawPacketsList.Count; }
        }

        public void TrimTheFat()//Not sure if this helps anything
        {
            //_RawPacketsList.TrimExcess();
        }
        /******************************************************************************/

        //Private variables
        private int _iListClientID;
        private Queue<RawPackets> _RawPacketsList;

        private int _bytesRemaining;
        private byte[] _Remainder;
    }

    class RawPackets
    {
        public RawPackets(int iClientId, byte[] theChunk, int sizeofchunk)
        {
            _dataChunk = new byte[sizeofchunk]; //create the space
            _dataChunk = theChunk;              //ram it in there
            _iClientId = iClientId;             //save who it came from
            _iChunkLen = sizeofchunk;           //hang onto the space size.. (Length doesn't work)
        }

        public byte[] dataChunk { get { return _dataChunk; } }
        public int iClientId { get { return _iClientId; } }
        public int iChunkLen { get { return _iChunkLen; } }

        private byte[] _dataChunk;
        private int _iClientId;
        private int _iChunkLen;
    }

    class FullPacket
    {
        public FullPacket( int iFromClient, byte[] thePacket)
        {
            _ThePacket = new byte[1024];
            _ThePacket = thePacket;
            _iFromClient = iFromClient;
        }

        public byte[] ThePacket { get { return _ThePacket; } set { _ThePacket = value; } }
        public int iFromClient { get { return _iFromClient; } set { _iFromClient = value; } }
        
        private byte[] _ThePacket;
        private int _iFromClient;
    }
}
