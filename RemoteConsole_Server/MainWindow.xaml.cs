using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RemoteConsole_Server
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private NetworkHelper helper;
        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            Init();
        }

        private void Init()
        {
            if (helper != null) helper._cancelToken.Cancel();
            helper = new NetworkHelper()
            {
                Ip_Addr = "192.168.0.107",
                //Ip_Addr = "127.0.0.1",
                _handle = MessageHandle,
                _cancelToken = new CancellationTokenSource()
            };
            while (!helper.Try_to_Conect())
            {
                Thread.Sleep(500);
            }
        }
        private void MessageHandle(string msg)
        {
            Dispatcher.Invoke(() =>
            {
                rch_msg.AppendText($"{msg}{Environment.NewLine}");
                rch_msg.ScrollToEnd();
            });
        }

        private void ClearRichBox(object sender, RoutedEventArgs e)
        {
            rch_msg.Document.Blocks.Clear();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            helper._cancelToken.Cancel();
        }

        private void Txt_cmd_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                helper.Execute(txt_cmd.Text);
            }
        }

        private void StartNewConsole(object sender, RoutedEventArgs e)
        {
            Init();
        }
    }
    class Globle
    {
        public static int Online_Number = 0;               //上线主机数量
        public static int Port = 9999;                     //默认上线端口
        public static int UDP_Port = 9999;                 //默认上线端口
        public static int ClientPort = 6666;               //默认上线端口
        public static bool _IsListen_Port = true;          //是否监听端口
        public static bool _IsResvice_Message = true;      //是否接收消息
    }
    public class NetworkHelper
    {
        public string Ip_Addr;
        TcpClient Client;
        NetworkStream Ns;
        String Welcome_Message;                  //欢迎信息
        String Result_Message;                   //被控端执行命令后的返回信息
        bool IsLis2Result = true;                //循环监听标志位
        public Action<string> _handle;
        public CancellationTokenSource _cancelToken;

        /// <summary>
        /// 此方法尝试连接被控端
        /// 如果连接上则返回对于控制端的流句柄
        /// </summary>
        public bool Try_to_Conect()
        {
            try
            {
                Client = new TcpClient();
                Client.Connect(this.Ip_Addr, Globle.ClientPort);
                if (Client.Connected)
                {
                    Ns = Client.GetStream();  //返回流控制句柄
                    //this.Send_Order(Ns);
                    ThreadPool.QueueUserWorkItem((state) =>
                    {
                        var cancel = (CancellationToken)state;
                        while (this.IsLis2Result && !cancel.IsCancellationRequested)
                        {
                            try
                            {
                                byte[] bb = new byte[102400];
                                int Len = this.Ns.Read(bb, 0, bb.Length);
                                string res = Encoding.Default.GetString(bb, 0, Len);
                                _handle(res);
                            }
                            catch (Exception ex)
                            { };
                        }
                    }, _cancelToken.Token);
                }

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        /// <summary>
        /// 此方法负责发送请求列举所有进程命令
        /// </summary>
        public void Execute(string cmd)
        {
            Ns.Write(Encoding.Default.GetBytes(cmd), 0, Encoding.Default.GetBytes(cmd).Length);
            Ns.Flush();
        }

    }
}
