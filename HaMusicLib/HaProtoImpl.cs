/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HaMusicLib
{
    public static class HaProtoImpl
    {
        public enum ServerToClient
        {
            PL_INFO,
            IDX_INFO,
            VOL_INFO,
            MEDIA_SEEK_INFO,
            PLAY_PAUSE_INFO
        }

        public enum ClientToServer
        {
            ADD,
            REMOVE,
            CLEAR,
            GETPL,
            GETIDX,
            SETIDX,
            GETVOL,
            SETVOL,
            UP,
            DOWN,
            SEEK,
            GETPOS,
            PAUSE,
            PLAY,
            GETSTATE
        }

        private static int ReceiveBlock(Socket s, out string data)
        {
            // Type
            byte[] typeBuf = new byte[4];
            int i = s.Receive(typeBuf);
            if (i == 0)
                throw new SocketException();
            int type = BitConverter.ToInt32(typeBuf, 0);
            
            // Length
            byte[] lenBuf = new byte[4];
            i = s.Receive(lenBuf);
            if (i == 0)
                throw new SocketException();
            int len = BitConverter.ToInt32(lenBuf, 0);

            // Value
            if (len != 0)
            {
                byte[] databuf = new byte[len];
                i = s.Receive(databuf);
                if (i == 0)
                    throw new SocketException();
                data = Encoding.UTF8.GetString(databuf);
            }
            else
            {
                data = "";
            }
            return type;
        }

        public static ServerToClient S2CReceive(Socket s, out string data)
        {
            return (ServerToClient)ReceiveBlock(s, out data);
        }

        public static ClientToServer C2SReceive(Socket s, out string data)
        {
            return (ClientToServer)ReceiveBlock(s, out data);
        }

        private static void SendBlock(Socket s, string x, int type)
        {
            byte[] typeBuf = BitConverter.GetBytes(type);
            byte[] dataBuf = Encoding.UTF8.GetBytes(x.ToCharArray());
            byte[] lenBuf = BitConverter.GetBytes(dataBuf.Length);
            s.Send(typeBuf);
            s.Send(lenBuf);
            s.Send(dataBuf);
        }

        public static void S2CSend(Socket s, string x, ServerToClient type)
        {
            SendBlock(s, x, (int)type);
        }

        public static void C2SSend(Socket s, string x, ClientToServer type)
        {
            SendBlock(s, x, (int)type);
        }
    }
}
