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
using BenQGuru.eMES.DLLService;

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
            textBox_IP.Text = IP;

            GetPrivateProfileString("MESS", "Port", null, temp, 1024, filePath);
            myPort = temp.ToString();
            imyPort = int.Parse(myPort);
            textBox_Port.Text = myPort;

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
                //labelTips.ForeCllor = Color.FromArgb(250, 250, 0, 0);
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
           textBox_Result.AppendText("开始监听客户端传来的信息!" + "\r\n");
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
                        textBox_Result.AppendText(ex.Message); //提示套接字监听异常   
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
                textBox_Result.AppendText("成功与" + RemoteEndPoint + "客户端建立连接！\t\n");     //显示与客户端连接情况
                dic.Add(RemoteEndPoint, connection);    //添加客户端信息

                //OnlineList_Disp(RemoteEndPoint);    //显示在线客户端

                //IPEndPoint netpoint = new IPEndPoint(clientIP,clientPort);

                IPEndPoint netpoint = connection.RemoteEndPoint as IPEndPoint;

                //创建一个通信线程    
                ParameterizedThreadStart pts = new ParameterizedThreadStart(recv);
                Thread thread = new Thread(pts);
                //Thread thread = new Thread(recv);
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
                   
                    textBox_Result.AppendText(strSRecMsg);
                    
                    //textBox2.AppendText("客户端:" + socketServer.RemoteEndPoint + ",time:" + GetCurrentTime() + "\r\n" + strSRecMsg + "\r\n\n");
                }
                catch (Exception ex)
                {
                    textBox_Result.AppendText("客户端" + socketServer.RemoteEndPoint + "已经中断连接" + "\r\n"); //提示套接字监听异常 
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

        static string resulttest;
        //获取结果
        private unsafe void parse_params(string str, string key, string val)
        {
            //定位key的位置
            int temp = str.LastIndexOf(key);

            string tempstr;
            if (temp > 0)
            {
                tempstr = str.Remove(0, temp - 1);
            }
            else
            {
                tempstr = str;
            }
            

            //分割字符串
            string[] s = tempstr.Split(new char[1]{'\n'});
            
            //获取所需字符串
            string resultstr = s[1];

            resulttest += s[1] + "\r\n";

            //返回结果
            val = resultstr.Remove(0, key.Length + 1);
            //labelTips.Text = resultstr;
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

        string temperature = null;
        string codec1 = null;
        string gsensorX = null;
        string gsensorY = null;
        string gsensorZ = null;
        string SD = null;
        string udisk = null;
        string MES_lock = null;
        string alarm_led = null;
        string rec_led = null;
        string sd1_led = null;
        string sd2_led = null;
        string SIM_status = null;
        string dial = null;
        string gps = null;
        string wifi = null;
        string camera1 = null;
        string camera2 = null;
        string camera3 = null;
        string camera4 = null;
        string camera5 = null;
        string camera6 = null;
        string IO_backoff = null;
        string IO_turnleft = null;
        string IO_turnright = null;
        string IO_alarmIn1 = null;
        string IO_alarmIn2 = null;
        string IO_alarmIn4 = null;

        public void setnull()
        {
            temperature = "";
            codec1 = "";
            gsensorX = "";
            gsensorY = "";
            gsensorZ = "";
            SD = "";
            udisk = "";
            MES_lock = "";
            alarm_led = "";
            rec_led = "";
            sd1_led = "";
            sd2_led = "";
            SIM_status = "";
            dial = "";
            gps = "";
            wifi = "";
            camera1 = "";
            camera2 = "";
            camera3 = "";
            camera4 = "";
            camera5 = "";
            camera6 = "";
            IO_backoff = "";
            IO_turnleft = "";
            IO_turnright = "";
            IO_alarmIn1 = "";
            IO_alarmIn2 = "";
            IO_alarmIn4 = "";
        }
        //获取测试结果
        public bool getresult()
        {
            string result = "测试结果：\r\n"+textBox_Result.Text;
            if (result == "")
            {
                labelTips.Text = "请先进行测试!";
                return false;
            }

            parse_params(result, "temperature", temperature);
            parse_params(result, "codec1", codec1);
            parse_params(result, "gsensorX", gsensorX);
            parse_params(result, "gsensorY", gsensorY);
            parse_params(result, "gsensorZ", gsensorZ);
            parse_params(result, "SD", SD);
            parse_params(result, "udisk", udisk);
            parse_params(result, "lock", MES_lock);
            parse_params(result, "alarm_led", alarm_led);
            parse_params(result, "rec_led", rec_led);
            parse_params(result, "sd1_led", sd1_led);
            parse_params(result, "sd2_led", sd2_led);
            parse_params(result, "SIM_status", SIM_status);
            parse_params(result, "dial", dial);
            parse_params(result, "gps", gps);
            parse_params(result, "wifi", wifi);
            parse_params(result, "camera1", camera1);
            parse_params(result, "camera2", camera2);
            parse_params(result, "camera3", camera3);
            parse_params(result, "camera4", camera4);
            parse_params(result, "camera5", camera5);
            parse_params(result, "camera6", camera6);
            parse_params(result, "IO_backoff", IO_backoff);
            parse_params(result, "IO_turnleft", IO_turnleft);
            parse_params(result, "IO_turnright", IO_turnright);
            parse_params(result, "IO_alarmIn1", IO_alarmIn1);
            parse_params(result, "IO_alarmIn2", IO_alarmIn2);
            parse_params(result, "IO_alarmIn4", IO_alarmIn4);

            return true;
        }

        public void Collecting_Errors(out string ErrCode)
        {
            ErrCode = "";
            if (temperature == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "temperature";
                }
                else
                {
                    ErrCode += ",temperature";
                }
            }
            if (codec1 == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "codec1";
                }
                else
                {
                    ErrCode += ",codec1";
                }
            }
            if (gsensorX == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "gsensorX";
                }
                else
                {
                    ErrCode += ",gsensorX";
                }
            }
            if (gsensorY == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "gsensorY";
                }
                else
                {
                    ErrCode += ",gsensorY";
                }
            }
            if (gsensorZ == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "gsensorZ";
                }
                else
                {
                    ErrCode += ",gsensorZ";
                }
            }
            if (SD == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "SD";
                }
                else
                {
                    ErrCode += ",SD";
                }
            }
            if (udisk == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "udisk";
                }
                else
                {
                    ErrCode += ",udisk";
                }
            }
            if (MES_lock == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "MES_lock";
                }
                else
                {
                    ErrCode += ",MES_lock";
                }
            }
            if (alarm_led == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "alarm_led";
                }
                else
                {
                    ErrCode += ",alarm_led";
                }
            }
            if (rec_led == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "rec_led";
                }
                else
                {
                    ErrCode += ",rec_led";
                }
            }
            if (sd1_led == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "sd1_led";
                }
                else
                {
                    ErrCode += ",sd1_led";
                }
            }
            if (sd2_led == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "sd2_led";
                }
                else
                {
                    ErrCode += ",sd2_led";
                }
            }
            if (SIM_status == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "SIM_status";
                }
                else
                {
                    ErrCode += ",SIM_status";
                }
            }
            if (dial == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "dial";
                }
                else
                {
                    ErrCode += ",dial";
                }
            }
            if (gps == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "gps";
                }
                else
                {
                    ErrCode += ",gps";
                }
            }
            if (wifi == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "wifi";
                }
                else
                {
                    ErrCode += ",wifi";
                }
            }
            if (camera1 == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "camera1";
                }
                else
                {
                    ErrCode += ",camera1";
                }
            }
            if (camera2 == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "camera2";
                }
                else
                {
                    ErrCode += ",camera2";
                }
            }
            if (camera3 == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "camera3";
                }
                else
                {
                    ErrCode += ",camera3";
                }
            }
            if (camera4 == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "camera4";
                }
                else
                {
                    ErrCode += ",camera4";
                }
            }
            if (camera5 == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "camera5";
                }
                else
                {
                    ErrCode += ",camera5";
                }
            }
            if (camera6 == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "camera6";
                }
                else
                {
                    ErrCode += ",camera6";
                }
            }
            if (IO_backoff == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "IO_backoff";
                }
                else
                {
                    ErrCode += ",IO_backoff";
                }
            }
            if (IO_turnleft == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "IO_turnleft";
                }
                else
                {
                    ErrCode += ",IO_turnleft";
                }
            }
            if (IO_turnright == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "IO_turnright";
                }
                else
                {
                    ErrCode += ",IO_turnright";
                }
            }
            if (IO_alarmIn1 == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "IO_alarmIn1";
                }
                else
                {
                    ErrCode += ",IO_alarmIn1";
                }
            }
            if (IO_alarmIn2 == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "IO_alarmIn2";
                }
                else
                {
                    ErrCode += ",IO_alarmIn2";
                }
            }
            if (IO_alarmIn4 == "FAIL")
            {
                if (ErrCode == "")
                {
                    ErrCode = "IO_alarmIn4";
                }
                else
                {
                    ErrCode += ",IO_alarmIn4";
                }
            }
            
        }
        private void button_upload_Click(object sender, EventArgs e)
        {
            resulttest = "测试结果：\r\n";

            labelTips.Text = "";
            string ErrMessage;
            string SN;
            int time;
            string Result;
            string ErrCode = "";

            //将变量设置为空
            setnull();

            bool b = getresult();
            if(!b)
            {
                return;
            }
            

            BenQGuru.eMES.DLLService.MESHelper temp = new BenQGuru.eMES.DLLService.MESHelper();

            SN = textBox_Sn.Text;
            if (SN == "")
            {
                labelTips.Text = "请扫描SN!";
                return;
            }

            //收集错误码
            Collecting_Errors(out ErrCode);
            if (ErrCode == "")
            {
                Result = "OK";
            }
            else
            {
                Result = "NG";
            }
            textBox_Result.Text = resulttest;
            /*
            if (ISCHECK == "TRUE")
            {
                bool Res = temp.CheckRoutePassed(SN, ResCode, out ErrMessage, out time);
                if (!Res)
                {
                    labelTips.Text = "该序列号不属于当前工序\n" + ErrMessage;
                    textBox_Sn.Text = "";
                    textBox_Sn.Focus();
                    return;
                }
                else
                {
                    if (!temp.SetMobileData(SN, ResCode, User, Result, ErrCode, out ErrMessage))
                    {
                        labelTips.Text = "上传失败，请重试!\n" + ErrMessage;
                    }
                    else
                    {
                        textBox_Result.Text = "";
                        textBox_Sn.Text = "";
                        labelTips.Text = "上传成功";
                        textBox_Sn.Focus();
                    }
                }
            }
            else
            {
                if (!temp.SetMobileData(SN, ResCode, User, Result, ErrCode, out ErrMessage))
                {
                    labelTips.Text = "上传失败，请重试!\n" + ErrMessage;
                }
                else
                {

                    textBox_Result.Text = "";
                    textBox_Sn.Text = "";
                    labelTips.Text = "上传成功";
                    textBox_Sn.Focus();
                }
            }
             * */

        }

    }
       
}


