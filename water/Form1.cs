using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Data.SqlClient;
using System.Configuration;
using ZedGraph;
using System.Threading.Tasks;
namespace water
{
    public partial class Form1 : Form
    {
        Thread th;
        Socket socketWatch;
        Socket socketSend;
        String temp;
        String hum;
        String ph;
        bool result, result2;
        double temp1, hum1, ph1;
        Dictionary<string, Socket> dicSocket = new Dictionary<string, Socket>();
        public Form1()
        {
            InitializeComponent();
            TextBox.CheckForIllegalCrossThreadCalls = false;

        }
        //加载托盘事件函数


        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                //当点击监听是，在服务器端创建一个负责监听IP地址和端口号的Socket
                socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress IP = IPAddress.Any;// 获取文本框中的IP地址
                IPEndPoint point = new IPEndPoint(IP, int.Parse(textBox2.Text.Trim()));
                socketWatch.Bind(point);
                ShowMsg("启动成功");
                socketWatch.Listen(20);
                th = new Thread(Listen);
                th.IsBackground = true;
                th.Start();

            }
            catch (Exception)
            { }
        }
        void Listen(object o)
        {
            socketSend = o as Socket;
            ShowMsg("服务端开始监听");
            //等待客户端连接
            while (true)
            {
                try
                {
                    //用于与客户端通信的socket
                    socketSend = socketWatch.Accept();
                    ShowMsg(socketSend.RemoteEndPoint.ToString() + "连接成功");
                    comboBox1.Items.Add(socketSend.RemoteEndPoint.ToString());
                    dicSocket.Add(socketSend.RemoteEndPoint.ToString(), socketSend);
                    Thread the = new Thread(Receive);
                    the.IsBackground = true;
                    the.Start(socketSend);
                }
                catch (Exception)
                {
                }

                //获取客户端的ip和服务号
                IPAddress clientIP = (socketSend.RemoteEndPoint as IPEndPoint).Address;
                int clientPort = (socketSend.RemoteEndPoint as IPEndPoint).Port;
                ShowMsg("获取到客户端ip" + clientIP + "获取端口号" + clientPort);
                //将远程连接的客户端的IP地址和Socket存入集合总

                //将远程连接客户端的IP地址和端口号储存在下拉框中

                //开启新的线程，用于接受客户端发来的消息

            }
        }


        void Receive(object o)
        {
            socketSend = o as Socket;
            while (true)
            {
                try
                {
                    //客户端连接成功后，服务器接受客户端发送的消息
                    byte[] buffer = new byte[1024 * 1024 * 2];
                    //实际接受的有效字节数
                    int r = socketSend.Receive(buffer);
                    if (r == 0)
                    {
                        break;
                    }
                    string str = Encoding.UTF8.GetString(buffer, 0, r);
                    ShowMsg(socketSend.RemoteEndPoint + "," + str);
                    if (str.Contains('T'.ToString()) && str.Contains('H'.ToString()) && str.Contains("PH"))//转换接收数据
                    {
                        char[] delimiter = { ' ' };
                        string[] sArray =str.Split(delimiter);
                        temp = sArray[0].Substring(sArray[0].IndexOf(":") + 1, sArray[0].IndexOf("#") - sArray[0].IndexOf(":") - 1);
                        hum = sArray[1].Substring(sArray[1].IndexOf(":") + 1, sArray[1].IndexOf("#") - sArray[1].IndexOf(":") - 1);
                        ph = sArray[2].Substring(sArray[2].IndexOf(":") + 1, sArray[2].IndexOf("#") - sArray[2].IndexOf(":") - 1);
                        temp1 = Convert.ToDouble(temp);
                        hum1 = Convert.ToDouble(hum);
                        ph1 = Convert.ToDouble(ph);
                        ShowMsg("接受并保存数据：" + temp1 + "," + hum1 + "," + ph1 + " " + result2.ToString() + "\r\n");
                        result2 = Savedata(temp1, hum1, ph1);
                        gpsdata gpsdata2 = getgps();
                 
                        
                    }
                    else
                    {
                        ShowMsg("传输语句有问题");
                    }
                }
                catch
                { }
            }
        }
        void ShowMsg(string str)
        {
            txtLog.AppendText(str + "\r\n");
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            try
            {
                th.Interrupt();
                socketWatch.Close();
                socketSend.Close();
                ShowMsg("服务已停止，客户端已断开");
                btnStart.Enabled = true;
                btnStop.Enabled = false;
            }
            catch
            { }
        }

        private void btnsend_Click(object sender, EventArgs e)
        {
            try
            {
                string str = txtMsg.Text.Trim();
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(str);
                string ip = comboBox1.SelectedItem.ToString();
                dicSocket[ip].Send(buffer);
                txtMsg.Clear();
            }
            catch
            { }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;
        }
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }
        public bool Savedata(double temp,double hum,double ph)//上传传感器数据
        {
            string connstr =@"Data Source=NWTVOP0OWTDS4XE; Initial Catalog=WaterServer;Integrated Security=true;";
            String time = DateTime.Now.ToString();

            string sql = "insert into AllData(time,temp,hum,ph) values('" + time + "','" + temp + "','" + hum + "','" + ph + "')";
            using (SqlConnection conn = new SqlConnection(connstr))
            {
                 conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    int i = 0;
                     i = cmd.ExecuteNonQuery();
                    conn.Close();
                   
                if(i>0)
                {
                    ShowMsg("数据已存入数据库中");
                    result= true;
                }
                else
                {
                    ShowMsg("数据未存入数据库中");
                    result=false;
                }
                
            }
            }
            return result;

            }   
        //获取GPS数据
        private gpsdata getgps()
        {
            string constr = @"Data Source=NWTVOP0OWTDS4XE; Initial Catalog=WaterServer;Integrated Security=true;";
            SqlConnection sqlcon;
            gpsdata gpsdata1 = new gpsdata();
            try
            {
                sqlcon = new SqlConnection();
                sqlcon.ConnectionString = constr;
                sqlcon.Open();
                string sql = "select top 1 * from GpsData order by id desc";//在数据库中读取
                SqlCommand cmd = new SqlCommand(sql, sqlcon);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    gpsdata1.Lng = Convert.ToDouble(reader[1]);
                    gpsdata1.Lat = Convert.ToDouble(reader[2]);
                }
                sqlcon.Close();
                reader.Close();
                cmd.Dispose();
                ShowMsg("获取的gps数据：" + gpsdata1.Lng + ","+gpsdata1.Lat);
            }
            catch
            {
                ShowMsg("gps出现异常");

            }
            return gpsdata1;
        }


            
    }
}
