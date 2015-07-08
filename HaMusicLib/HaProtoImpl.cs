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

        private static byte[] ReceiveAll(this Socket s, int len)
        {
            int bRead = 0;
            byte[] result = new byte[len];
            while (bRead < len)
            {
                bRead += s.Receive(result, bRead, len - bRead, SocketFlags.None);
            }
            return result;
        }

        private static int ReceiveBlock(Socket s, out string data)
        {
            // Type
            int type = BitConverter.ToInt32(s.ReceiveAll(4), 0);
            
            // Length
            int len = BitConverter.ToInt32(s.ReceiveAll(4), 0);

            // Value
            data = len > 0 ? Encoding.UTF8.GetString(s.ReceiveAll(len)) : "";

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

            s.Send(new List<ArraySegment<byte>> { new ArraySegment<byte>(typeBuf), new ArraySegment<byte>(lenBuf), new ArraySegment<byte>(dataBuf) });
        }

        private static void SafeSendBlock(Socket s, string x, int type)
        {
            try
            {
                SendBlock(s, x, type);
            }
            catch (Exception)
            {
                try
                {
                    s.Close();
                }
                catch { }
            }
        }

        public static void S2CSend(Socket s, string x, ServerToClient type)
        {
            SafeSendBlock(s, x, (int)type);
        }

        public static void C2SSend(Socket s, string x, ClientToServer type)
        {
            SafeSendBlock(s, x, (int)type);
        }
    }
}
