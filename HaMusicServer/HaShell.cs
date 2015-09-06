/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleControl;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Net;
using System.IO;
using HaMusicLib;
using System.Windows.Forms;

namespace HaMusicServer
{
    public class HaShell
    {
        private ConsoleControl.ConsoleControl console;
        private MainForm mainForm;

        public HaShell(MainForm mainForm, ConsoleControl.ConsoleControl console)
        {
            this.console = console;
            this.mainForm = mainForm;

            this.console.OnConsoleInput += Console_OnConsoleInput;
            console.ClearOutput();
            console.IsInputEnabled = true;

            if (console.IsHandleCreated)
                Console_HandleCreated(null, null);
            else
                console.HandleCreated += Console_HandleCreated;
            
        }

        private void Console_HandleCreated(object sender, EventArgs e)
        {
            ConsoleWrite("# ");
        }

        [DllImport("shell32.dll", SetLastError = true)]
        static extern IntPtr CommandLineToArgvW(
        [MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

        public static string[] CommandLineToArgs(string commandLine)
        {
            int argc;
            var argv = CommandLineToArgvW(commandLine, out argc);
            if (argv == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception();
            try
            {
                var args = new string[argc];
                for (var i = 0; i < args.Length; i++)
                {
                    var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i] = Marshal.PtrToStringUni(p);
                }

                return args;
            }
            finally
            {
                Marshal.FreeHGlobal(argv);
            }
        }

        private void Console_OnConsoleInput(object sender, ConsoleControl.ConsoleEventArgs args)
        {
            try
            {
                string[] cmd = CommandLineToArgs(args.Content);
                if (cmd.Length < 0)
                    goto ret;
                string command = cmd[0];
                cmd = Enumerable.Skip(cmd, 1).ToArray();
                MethodInfo mi = null;
                if (command == "help")
                {
                    ConsoleWriteLines(new List<string> {
                        string.Format("HaShell [Version {0}]", ServerDataSource.LocalVersion),
                        "",
                        "\thelp - show this help",
                        "\tclients - print all clients",
                        "\tkick [--ban] <ip> - disconnect client, optionally banning it",
                        "\tunban <ip> - unban client",
                        "\tbanlist - print banlist",
                        "\ttail [n] - print last n lines from the log, default 10",
                        "\tflush [path] - force DataSource flush, optionally into a specific path",
                        "\tload - load DataSource from path",
                        "\texit - exit server"
                    });
                    goto ret;
                }
                try
                {
                    mi = GetType().GetMethod("command_" + command);
                }
                catch { }
                if (mi == null)
                {
                    ConsoleWriteLine(string.Format("Couldn't find command {0}, try `help`", command));
                    goto ret;
                }
                mi.Invoke(this, new object[] { cmd });
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException)
                    e = e.InnerException;
                ConsoleWriteLine("Unhandled exception occurred: " + e.Message + "\r\n" + e.StackTrace);
            }

            ret:
            ConsoleWrite("# ");
        }

        public void command_clients(string[] args)
        {
            List<string> output = new List<string>();
            lock (mainForm.clients)
            {
                foreach (Client c in mainForm.clients)
                {
                    output.Add(((IPEndPoint)c.Socket.RemoteEndPoint).Address.ToString());
                }
            }
            ConsoleWriteLines(output);
        }

        public void command_kick(string[] args)
        {
            bool ban = false;
            IPAddress ipaddr = null;
            foreach (string arg in args)
            {
                if (arg == "--ban")
                    ban = true;
                else
                {
                    IPAddress.TryParse(arg, out ipaddr);
                }
            }
            if (ipaddr == null)
            {
                ConsoleWriteLine("kick: must supply an IP address");
                return;
            }
            lock (mainForm.clients)
            {
                foreach (Client c in mainForm.clients)
                {
                    if (((IPEndPoint)c.Socket.RemoteEndPoint).Address.Equals(ipaddr))
                    {
                        c.Socket.Close();
                    }
                }
            }
            if (ban)
            {
                lock (mainForm.banlist)
                {
                    mainForm.banlist.Add(ipaddr);
                    mainForm.FlushBanlist();
                }
            }
        }

        public void command_unban(string[] args)
        {
            IPAddress addr = null;
            if (args.Length < 1 || !IPAddress.TryParse(args[0], out addr))
            {
                ConsoleWriteLine("unban: must supply an IP address");
                return;
            }
            lock (mainForm.banlist)
            {
                mainForm.banlist.RemoveAll(x => x.Equals(addr));
                mainForm.FlushBanlist();
            }
        }

        public void command_banlist(string[] args)
        {
            List<string> output;
            lock (mainForm.banlist)
            {
                output = mainForm.banlist.Select(x => x.ToString()).ToList();
            }
            ConsoleWriteLines(output);
        }

        public static string[] ReadEndLines(string path, int numberOfTokens)
        {
            byte[] buffer = new byte[1];

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                int tokenCount = 0;

                for (int position = 1; position < fs.Length; position++)
                {
                    fs.Seek(-position, SeekOrigin.End);
                    fs.Read(buffer, 0, buffer.Length);

                    if (buffer[0] == 0x0A)
                    {
                        tokenCount++;
                        if (tokenCount == numberOfTokens)
                        {
                            byte[] returnBuffer = new byte[fs.Length - fs.Position];
                            fs.Read(returnBuffer, 0, returnBuffer.Length);
                            return Encoding.ASCII.GetString(returnBuffer).Split(new string[] { "\r\n" }, StringSplitOptions.None);
                        }
                    }
                }

                // handle case where number of tokens in file is less than numberOfTokens
                fs.Seek(0, SeekOrigin.Begin);
                buffer = new byte[fs.Length];
                fs.Read(buffer, 0, buffer.Length);
                return Encoding.ASCII.GetString(buffer).Split(new string[] { "\r\n" }, StringSplitOptions.None);
            }
        }

        public void command_tail(string[] args)
        {
            int n;
            if (args.Length == 0 || !int.TryParse(args[0], out n))
                n = 10;
            mainForm.FlushLog();
            ConsoleWriteLines(ReadEndLines(MainForm.defaultLogPath, n).ToList());
        }

        public void command_flush(string[] args)
        {
            string path = args.Length > 0 ? args[0] : MainForm.defaultSourcePath;
            mainForm.SaveSourceState(path);
        }

        public void command_load(string[] args)
        {
            string path = args.Length > 0 ? args[0] : MainForm.defaultSourcePath;
            mainForm.LoadSourceState(path);
        }

        public void command_exit(string[] args)
        {
            Application.Exit();
        }

        private void ConsoleWrite(string data)
        {
            console.WriteOutput(data, console.InternalRichTextBox.ForeColor);
        }

        private void ConsoleWriteLine(string line)
        {
            console.WriteOutput(line + "\r\n", console.InternalRichTextBox.ForeColor);
        }

        private void ConsoleWriteLines(List<string> lines)
        {
            lines.ForEach(x => console.WriteOutput(x + "\r\n", console.InternalRichTextBox.ForeColor));
        }
    }
}
