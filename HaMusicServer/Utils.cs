/* Copyright (C) 2017 Yuval Deutscher

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace HaMusicServer
{
    public static class Utils
    {
        public static string GetErrorException(Exception e, [CallerMemberName] string name = "Unknown")
        {
            return string.Format("{0}: {1}\r\n\r\n{2}", name, e.Message, e.StackTrace);
        }

        public static void ExecutePacketAndBroadcast(HaProtoImpl.Opcode op, HaProtoImpl.HaProtoPacket packet)
        {
            bool announceIndexChange = packet.ApplyToDatabase(Program.Core.DataSource);
            if (announceIndexChange)
            {
                Program.Core.AnnounceIndexChange();
            }
            Program.Server.BroadcastMessage(op, packet);
        }

        public static void ExecutePacketsAndBroadcast(List<HaProtoImpl.Opcode> ops, List<HaProtoImpl.HaProtoPacket> packets)
        {
            bool announceIndexChange = false;
            foreach (HaProtoImpl.HaProtoPacket packet in packets)
            {
                announceIndexChange |= packet.ApplyToDatabase(Program.Core.DataSource);
            }
            if (announceIndexChange)
            {
                Program.Core.AnnounceIndexChange();
            }
            for (int i = 0; i < packets.Count; i++)
            {
                Program.Server.BroadcastMessage(ops[i], packets[i]);
            }
        }

        public static void BroadcastPosition()
        {
            int pos = Program.Core.Player.Position;
            int max = Program.Core.Player.Maximum;
            if (Program.Core.DataSource.Position == pos && Program.Core.DataSource.Maximum == max)
                return;
            Program.Core.DataSource.Position = pos;
            Program.Core.DataSource.Maximum = max;
            Program.Server.BroadcastMessage(HaProtoImpl.Opcode.SEEK, new HaProtoImpl.SEEK() { pos = pos, max = max });
        }

        public static void SaveSourceStateSafe()
        {
            try
            {
                Program.Core.SaveSourceState(Consts.defaultSourcePath);
            }
            catch (Exception ex)
            {
                Program.Logger.Log(Utils.GetErrorException(ex));
            }
        }
    }
}
