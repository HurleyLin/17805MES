using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Runtime.InteropServices;
//using BenQGuru.eMES.DLLService;

namespace _17805
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32")]//返回取得字符串缓冲区的长度
        private static extern long GetPrivateProfileString(string section, string key,
            string def, StringBuilder retVal, int size, string filePath);

        string ResCode;
        string ISCHECK;
        string User;
        string PassWork;
        string IP;
        string myPort;
        int imyPort;
        bool CheckResult = true;


        public Form1()
        {
            InitializeComponent();
            //关联closing事件
            this.FormClosing += new FormClosingEventHandler(this.Form1_FormClosing);

            string filePath;
            string ErrMessage = "";

            filePath = System.AppDomain.CurrentDomain.BaseDirectory + "MESS.ini";

            StringBuilder temp = new StringBuilder(1024);
            GetPrivateProfileString("MESS", "User", null, temp, 1024, filePath);
            User = temp.ToString();

            GetPrivateProfileString("MESS", "PassWork", null, temp, 1024, filePath);
            PassWork = temp.ToString();

            GetPrivateProfileString("MESS", "ResCode", null, temp, 1024, filePath);
            ResCode = temp.ToString();

            //GetPrivateProfileString("T8300", "WorkOrder", null, temp, 1024, filePath);
            //WorkOrder = temp.ToString();

            GetPrivateProfileString("MESS", "ISCHECK", null, temp, 1024, filePath);
            ISCHECK = temp.ToString();

            GetPrivateProfileString("MESS", "IP", null, temp, 1024, filePath);
            IP = temp.ToString();
            textBox_ip.Text = IP;

            GetPrivateProfileString("MESS", "Port", null, temp, 1024, filePath);
            myPort = temp.ToString();
            imyPort = int.Parse(myPort);
            textBox_port.Text = myPort;

            /*
            BenQGuru.eMES.DLLService.MESHelper login = new BenQGuru.eMES.DLLService.MESHelper();
            if (!login.CheckUserAndResourcePassed(User, ResCode, PassWork, "", out ErrMessage))
            {
                labelTips.Text = "登录失败\n" + ErrMessage;
                labelTips.ForeColor = Color.Red;
            }
            else
            {
                labelTips.Text = "登录成功";
                labelTips.ForeColor = Color.Red;
            }
             * */
        }

        
        private void Form1_Load(object sender, EventArgs e)
        {
            /*
            IPHostEntry ipHost = Dns.Resolve(Dns.GetHostName());
            IPAddress ipAddr = ipHost.AddressList[0];
            string ipAddress = ipAddr.ToString();
            textBox1.Text = ipAddress.ToString();
             * */
        }

        //private static byte[] result = new byte[1024];
        //private static int imyProt = 65500;   //端口
        
        static Socket serverSocket;
        public void server()
        {
            //服务器IP地址 
            //IPAddress ip = IPAddress.Parse("192.168.3.100");
            IPAddress ip = IPAddress.Parse(IP);
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(ip, imyPort));  //绑定IP地址：端口 
            serverSocket.Listen(10);    //设定最多10个排队连接请求
 
            Form1 form1 = new Form1();
           
            textBox2.Text += "启动监听" + serverSocket.LocalEndPoint.ToString() + "成功"+"\r\n";
            //通过Clientsoket发送数据 
            Thread myThread = new Thread(ListenClientConnect);
            myThread.Start();
        }

        /// <summary> 
        /// 监听客户端连接 
        /// </summary> 
        private  void ListenClientConnect()
        {
            while (true)
            {
                Socket clientSocket = serverSocket.Accept();
                clientSocket.Send(Encoding.ASCII.GetBytes("Server Say Hello"));
                Thread receiveThread = new Thread(ReceiveMessage);
                receiveThread.Start(clientSocket);
            }
        }

        /// <summary> 
        /// 接收消息 
        /// </summary> 
        /// <param name="clientSocket"></param> 
         static byte[] result = new byte[1];
        public  void ReceiveMessage(object clientSocket)
        {
            Socket myClientSocket = (Socket)clientSocket;
            while (true)
            {
                try
                {
                    //static byte[] result = new byte[300];
                    //通过clientSocket接收数据 
                    int receiveNumber = myClientSocket.Receive(result);

                    textBox2.Text += Encoding.ASCII.GetString(result);
                    //textBox2.Text += "接收客户端:" + myClientSocket.RemoteEndPoint.ToString() + "消息:" + Encoding.ASCII.GetString(result, 0, receiveNumber) + "\r\n";
                }
                catch (Exception ex)
                {
   
                    textBox2.Text += ex.Message + "\r\n";
                    myClientSocket.Shutdown(SocketShutdown.Both);
                    myClientSocket.Close();
                    break;
                }
            }
        }

        string RemoteEndPoint;     //客户端的网络结点

        Thread threadwatch = null;//负责监听客户端的线程
        Socket socketwatch = null;//负责监听客户端的套接字

        //创建一个和客户端通信的套接字
        Dictionary<string, Socket> dic = new Dictionary<string, Socket> { };

        public void serverstart()
        { 
            //定义一个套接字用于监听客户端发来的消息，包含三个参数（IP4寻址协议，流式连接，Tcp协议）
            socketwatch = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
 
            //服务端发送信息需要一个IP地址和端口号
            //IPAddress ip = IPAddress.Parse(textBox1.Text.Trim());//获取文本框输入的IP地址
            IPAddress ip = IPAddress.Parse("192.168.3.100");
 
            //将IP地址和端口号绑定到网络节点point上
            //IPEndPoint point = new IPEndPoint(ip, int.Parse(textBox2.Text.Trim()));//获取文本框上输入的端口号
            IPEndPoint point = new IPEndPoint(ip, imyPort);

            //此端口专门用来监听的
 
            //监听绑定的网络节点
            socketwatch.Bind(point);
 
            //将套接字的监听队列长度限制为20
            socketwatch.Listen(20);    
 
            //创建一个监听线程
            threadwatch = new Thread(watchconnecting);   
           
            //将窗体线程设置为与后台同步，随着主线程结束而结束
            threadwatch.IsBackground = true;  
 
           //启动线程   
            threadwatch.Start(); 
  
           //启动线程后,文本框显示相应提示
           textBox2.AppendText("开始监听客户端传来的信息!" + "\r\n");
        }

        private void watchconnecting()
        {
            Socket connection = null;
            while (true)  //持续不断监听客户端发来的请求   
            {
                try
                {
                        connection = socketwatch.Accept();                     
                    
                }
                catch (Exception ex)
                {
                    if (socketwatch != null)
                    {
                        MessageBox.Show("重启软件");
                        textBox2.AppendText(ex.Message); //提示套接字监听异常   
                    }          
                    break;
                }
                //获取客户端的IP和端口号
                IPAddress clientIP = (connection.RemoteEndPoint as IPEndPoint).Address;
                int clientPort = (connection.RemoteEndPoint as IPEndPoint).Port;

                //让客户显示"连接成功的"的信息
                string sendmsg = "连接服务端成功！\r\n" + "本地IP:" + clientIP + "，本地端口" + clientPort.ToString();
                byte[] arrSendMsg = Encoding.UTF8.GetBytes(sendmsg);
                connection.Send(arrSendMsg);


                RemoteEndPoint = connection.RemoteEndPoint.ToString(); //客户端网络结点号
                textBox2.AppendText("成功与" + RemoteEndPoint + "客户端建立连接！\t\n");     //显示与客户端连接情况
                dic.Add(RemoteEndPoint, connection);    //添加客户端信息

                //OnlineList_Disp(RemoteEndPoint);    //显示在线客户端

                //IPEndPoint netpoint = new IPEndPoint(clientIP,clientPort);

                IPEndPoint netpoint = connection.RemoteEndPoint as IPEndPoint;

                //创建一个通信线程    
                ParameterizedThreadStart pts = new ParameterizedThreadStart(recv);
                Thread thread = new Thread(pts);
                thread.IsBackground = true;//设置为后台线程，随着主线程退出而退出   
                //启动线程   
                thread.Start(connection);
            }
        }


        string Result = null;
        /// 接收客户端发来的信息     
        ///客户端套接字对象  
        private void recv(object socketclientpara)
        {

            Socket socketServer = socketclientpara as Socket;
            while (true)
            {
                //创建一个内存缓冲区 其大小为1024*1024字节  即1M   
                byte[] arrServerRecMsg = new byte[1];
                //将接收到的信息存入到内存缓冲区,并返回其字节数组的长度  
                try
                {
                    int length = socketServer.Receive(arrServerRecMsg);

                    //将机器接受到的字节数组转换为人可以读懂的字符串   
                    string strSRecMsg = Encoding.ASCII.GetString(arrServerRecMsg, 0, length);
                    Result += strSRecMsg;
                    //textBox2.AppendText(Result);
                    //将发送的字符串信息附加到文本框txtMsg上
                   
                    textBox2.AppendText(strSRecMsg);
                    
                    //textBox2.AppendText("客户端:" + socketServer.RemoteEndPoint + ",time:" + GetCurrentTime() + "\r\n" + strSRecMsg + "\r\n\n");
                }
                catch (Exception ex)
                {
                    textBox2.AppendText("客户端" + socketServer.RemoteEndPoint + "已经中断连接" + "\r\n"); //提示套接字监听异常 
                    //listBoxOnlineList.Items.Remove(socketServer.RemoteEndPoint.ToString());//从listbox中移除断开连接的客户端
                    socketServer.Close();//关闭之前accept出来的和客户端进行通信的套接字
                    break;
                }
            }
        }

        /// 当前时间   
        private DateTime GetCurrentTime()
        {
            DateTime currentTime = new DateTime();
            currentTime = DateTime.Now;
            return currentTime;
        }

        

        private void button1_Click(object sender, EventArgs e)
        {
            serverstart();
           
        }

        private void Form1_FormClosing(object sender, EventArgs e)
        {
            if (socketwatch != null)
            {
                socketwatch.Close();
            }
            this.Dispose();
        }

    }
       
}


