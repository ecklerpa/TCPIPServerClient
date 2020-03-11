using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonClassLibs
{
    public class MotherOfRawPackets
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

    public class RawPackets
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

    public class FullPacket
    {
        public FullPacket(int iFromClient, byte[] thePacket)
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
