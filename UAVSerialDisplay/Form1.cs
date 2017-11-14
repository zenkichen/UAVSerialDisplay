using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.WindowsForms.ToolTips;
using System.Reflection;

namespace UAVSerialDisplay
{
    public partial class Form1 : Form
    {
        double lat, lon;
        double lonStart, latStart;
        double[] pointsLineLat=new double[4];//前一个点纬度
        double[] pointsLineLon = new double[4];//前一个点经度
        //bool serialRun=false;
        private bool Listening = false;//是否没有执行完invoke相关操作   
        private bool Closing = false;//是否正在关闭串口，执行Application.DoEvents，并阻止再次invoke   

        GMapOverlay overlayLeader;
        GMapOverlay overlayFollower1;
        GMapOverlay overlayFollower2;
        GMapOverlay overlayFollower3;
        GMapMarker markerLeader;
        GMapMarker markerFollower1;
        GMapMarker markerFollower2;
        GMapMarker markerFollower3;
        GMapRoute routeLeader;
        GMapRoute routeFollower1;
        GMapRoute routeFollower2;
        GMapRoute routeFollower3;
        List<PointLatLng> pointsLeader = new List<PointLatLng>();
        List<PointLatLng> pointsFollower1 = new List<PointLatLng>();
        List<PointLatLng> pointsFollower2 = new List<PointLatLng>();
        List<PointLatLng> pointsFollower3 = new List<PointLatLng>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            lon = 119.30;
            lat = 26.09;
            lonStart = 119.30;
            latStart = 26.09;

            //获取可用串口
            string[] portList = System.IO.Ports.SerialPort.GetPortNames();
            for (int i = 0; i < portList.Length; ++i)
            {
                string name = portList[i];
                comboBoxCOM.Items.Add(name);
            }
            if (portList.Length > 0)
                comboBoxCOM.SelectedIndex = 0;
            else
                MessageBox.Show("无可用串口！");
            comboBoxRate.SelectedIndex = 2;

            btnCloseCOM.Enabled = false;
            btnOpenCOM.Enabled = true;

            for(int i=0;i<4;i++)
            {
                pointsLineLat[i] = 1000;
                pointsLineLon[i] = 1000;
            }


            /****************/
            InitMapControl();
        }
        private void InitMapControl()
        {
            this.gMapControl1.Manager.Mode = AccessMode.CacheOnly;
            this.gMapControl1.MapProvider = GMap.NET.MapProviders.GoogleChinaMapProvider.Instance;
            this.gMapControl1.Position = new PointLatLng(37.6287, -122.393);
            this.gMapControl1.MaxZoom = 17;
            this.gMapControl1.MinZoom = 10;
            this.gMapControl1.Zoom = 12;


            this.gMapControl1.DragButton = MouseButtons.Left;


            overlayLeader = new GMapOverlay("leader");  //new一个overlays对象
            overlayFollower1 = new GMapOverlay("follow1");  //new一个overlays对象
            overlayFollower2 = new GMapOverlay("follow2");  //new一个overlays对象
            overlayFollower3 = new GMapOverlay("follow3");  //new一个overlays对象
            gMapControl1.Overlays.Add(overlayLeader);
            gMapControl1.Overlays.Add(overlayFollower1);
            gMapControl1.Overlays.Add(overlayFollower2);
            gMapControl1.Overlays.Add(overlayFollower3);
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            double lonCenter, latCenter;
            lon += 0.001;
            lat += 0.001;
            lonCenter = (lonStart + lon) / 2;
            latCenter = (latStart + lat) / 2;
            gMapControl1.Position = new PointLatLng(lat, lon);
            overlayLeader.Markers.Clear();
            markerLeader = new GMarkerGoogle(new PointLatLng(lat, lon), GMarkerGoogleType.arrow);
            overlayLeader.Markers.Add(markerLeader);
            pointsLeader.Add(new PointLatLng(lat, lon));
            routeLeader = new GMapRoute(pointsLeader, "");
            routeLeader.Stroke= new Pen(Color.Red, 1); 
            overlayLeader.Routes.Clear();
            overlayLeader.Routes.Add(routeLeader);
            //webBrowser1.Document.InvokeScript("moveCamera", new object[] { lonCenter.ToString(), latCenter.ToString(), "12" });
            //webBrowser1.Document.InvokeScript("drawPoint", new object[] { lon.ToString(), lat.ToString(), 0});//参数：经度，纬度，飞机index，是否清除
            //webBrowser1.Document.InvokeScript("drawLine", new object[] { 0, 0, lon.ToString(), lat.ToString(), 0 });
            lat += 0.01;
            overlayFollower1.Markers.Clear();
            markerFollower1 = new GMarkerGoogle(new PointLatLng(lat, lon), GMarkerGoogleType.arrow);
            overlayFollower1.Markers.Add(markerFollower1);
            pointsFollower1.Add(new PointLatLng(lat, lon));
            routeFollower1 = new GMapRoute(pointsFollower1, "");
            overlayFollower1.Routes.Clear();
            overlayFollower1.Routes.Add(routeFollower1);

            //webBrowser1.Document.InvokeScript("drawPoint", new object[] { lon.ToString(), lat.ToString(), 1});
            //webBrowser1.Document.InvokeScript("drawLine", new object[] { 0, 0, lon.ToString(), lat.ToString(), 1 });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            timer1.Start();
        }


        private void btnOpenCOM_Click(object sender, EventArgs e)
        {
            int baudRate = 57600;
            baudRate = int.Parse(comboBoxRate.Text);
            if (comboBoxCOM.Text == "")
            {
                MessageBox.Show("串口设置错误");
                return;
            }
            try
            {
                serialPort1 = new SerialPort(comboBoxCOM.Text, baudRate, Parity.None, 8, StopBits.One);
                //关键 为 serialPort1绑定事件句柄
                serialPort1.DataReceived += new SerialDataReceivedEventHandler(serialPort1_DataReceived);

                serialPort1.Open();
                btnOpenCOM.Enabled = false;
                btnCloseCOM.Enabled = true;
                //textBox7.Text = "串口已打开";
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        //显示接收的数据
        private void SetText(byte[] s)
        {
            int dataLen = 0;
            int index = 0;
            double lat=0, lon=0;
            //MessageBox.Show(Encoding.UTF8.GetString(s));
            if((s[0]==0x7E)&&(s[1]==0x00))//帧头
            {
                dataLen = s[2];
                lat = (double)((s[13] << 24) + (s[12] << 16) + (s[11] << 8) + (s[10])) / 10000000;
                textBox1.Text = lat.ToString();
                lon = (double)((s[17] << 24) + (s[16] << 16) + (s[15] << 8) + (s[14])) / 10000000;
                textBox2.Text = lon.ToString();
                //richTextBox1.Text = HexToString(s);
                if ((Math.Abs(lon) < 0.01) && (Math.Abs(lat)<0.01))
                {
                    //richTextBox1.Text = HexToString(s);
                }
                else
                {

                    if ((s[4] == 0x22) && (s[5] == 0x22))//主机地址
                    {
                        gMapControl1.Position = new PointLatLng(lat, lon);
                        overlayLeader.Markers.Clear();
                        markerLeader = new GMarkerGoogle(new PointLatLng(lat, lon), GMarkerGoogleType.red);
                        overlayLeader.Markers.Add(markerLeader);
                        pointsLeader.Add(new PointLatLng(lat, lon));
                        routeLeader = new GMapRoute(pointsLeader, "");
                        routeLeader.Stroke = new Pen(Color.Red, 2);
                        overlayLeader.Routes.Clear();
                        overlayLeader.Routes.Add(routeLeader);
                    }
                    else if ((s[4] == 0x33) && (s[5] == 0x33))//跟随着1
                    {
                        overlayFollower1.Markers.Clear();
                        markerFollower1 = new GMarkerGoogle(new PointLatLng(lat, lon), GMarkerGoogleType.green_small);
                        overlayFollower1.Markers.Add(markerFollower1);
                        pointsFollower1.Add(new PointLatLng(lat, lon));
                        routeFollower1 = new GMapRoute(pointsFollower1, "");
                        routeFollower1.Stroke = new Pen(Color.Green, 2);
                        overlayFollower1.Routes.Clear();
                        overlayFollower1.Routes.Add(routeFollower1);
                    }
                    else if ((s[4] == 0x44) && (s[5] == 0x44))//跟随着2
                    {
                        overlayFollower2.Markers.Clear();
                        markerFollower2 = new GMarkerGoogle(new PointLatLng(lat, lon), GMarkerGoogleType.blue_small);
                        overlayFollower2.Markers.Add(markerFollower2);
                        pointsFollower2.Add(new PointLatLng(lat, lon));
                        routeFollower2 = new GMapRoute(pointsFollower2, "");
                        routeFollower2.Stroke = new Pen(Color.Blue, 2);
                        overlayFollower2.Routes.Clear();
                        overlayFollower2.Routes.Add(routeFollower2);
                    }
                    else if ((s[4] == 0x55) && (s[5] == 0x55))//跟随着3
                    {
                        overlayFollower3.Markers.Clear();
                        markerFollower3 = new GMarkerGoogle(new PointLatLng(lat, lon), GMarkerGoogleType.gray_small);
                        overlayFollower3.Markers.Add(markerFollower3);
                        pointsFollower3.Add(new PointLatLng(lat, lon));
                        routeFollower3 = new GMapRoute(pointsFollower3, "");
                        routeFollower3.Stroke = new Pen(Color.Gray, 2);
                        overlayFollower3.Routes.Clear();
                        overlayFollower3.Routes.Add(routeFollower3);
                    }
                }
            }
        }


        //数据接收使用的代理
        private delegate void myDelegate(byte[] s);
        //串口数据到达时的事件
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (Closing) return;//如果正在关闭，忽略操作，直接返回，尽快的完成串口监听线程的一次循环   
            //关键 代理
            myDelegate md = new myDelegate(SetText);
            //MessageBox.Show("received");

            try
            {
                Listening = true;//设置标记，说明我已经开始处理数据，一会儿要使用系统UI的。 
                if (serialPort1.IsOpen == true)
                {
                    byte[] readBuffer = new byte[serialPort1.ReadBufferSize];
                    serialPort1.Read(readBuffer, 0, readBuffer.Length);
                    //string readstr = Encoding.UTF8.GetString(readBuffer);

                    Invoke(md, readBuffer);
                }
            }
            finally
            {
                Listening = false;//我用完了，ui可以关闭串口了。   
            }
        }

        private void btnCloseCOM_Click(object sender, EventArgs e)
        {
            Closing = true;  
            try
            {
                while (Listening) Application.DoEvents();   
                serialPort1.Close();
                if (serialPort1.IsOpen == false)
                {
                    //串口已关闭
                    Closing = false;   
                    btnCloseCOM.Enabled = false;
                    btnOpenCOM.Enabled = true;
                }
            }
            catch (Exception err)
            {
                throw err;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int i = 0;
            int distance;
            byte check_sum = 0x00;
            byte tmp = 0xFF;
            byte[] sendData = new byte[] { 0x7E, 0x00, 0x15, 0x01, 0x01, 0x22, 0x22, 0x00, 0xAF, 0xFB, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0xAA };
            distance = Int32.Parse(textBox4.Text);
            byte[] distanceByte = System.BitConverter.GetBytes(distance);
            sendData[10] = (byte)distanceByte[0];
            sendData[11] = (byte)distanceByte[1];
            sendData[12] = (byte)distanceByte[2];
            sendData[13] = (byte)distanceByte[3];
            for (i = 3; i < 24; i++)
            {
                check_sum += sendData[i];
            }
            sendData[24] = (byte)(0xFF - check_sum);
            /*
            check_sum = 0x00;
            for (i = 10; i < 22;i++ )
            {
                check_sum += sendData[i];
            }
            sendData[23] = (byte)(0xFF - check_sum+0xAA);
            */
            richTextBox1.Text = HexToString(sendData);
            serialPort1.Write(sendData, 0, sendData.Length);
        }

        //Hex to String
        public static string HexToString(byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("X2") + " ";
                }
            }
            return returnStr;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            int i = 0;
            int distance;
            byte check_sum = 0x00;
            byte tmp = 0xFF;
            byte[] sendData = new byte[] { 0x7E, 0x00, 0x15, 0x01, 0x01, 0x22, 0x22, 0x00, 0xAF, 0xFB, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0xAA };
            distance = Int32.Parse(textBox4.Text);
            byte[] distanceByte=System.BitConverter.GetBytes(distance);
            sendData[10] = (byte)distanceByte[0];
            sendData[11] = (byte)distanceByte[1];
            sendData[12] = (byte)distanceByte[2];
            sendData[13] = (byte)distanceByte[3];
            for (i = 3; i < 24; i++)
            {
                check_sum += sendData[i];
            }
            sendData[24] = (byte)(tmp - check_sum);
            richTextBox1.Text = HexToString(sendData);
            serialPort1.Write(sendData, 0, sendData.Length);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            int i = 0;
            int distance;
            byte check_sum = 0x00;
            byte tmp = 0xFF;
            byte[] sendData = new byte[] { 0x7E, 0x00, 0x15, 0x01, 0x01, 0x22, 0x22, 0x00, 0xAF, 0xFB, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0xAA };
            distance = Int32.Parse(textBox4.Text);
            byte[] distanceByte = System.BitConverter.GetBytes(distance);
            sendData[10] = (byte)distanceByte[0];
            sendData[11] = (byte)distanceByte[1];
            sendData[12] = (byte)distanceByte[2];
            sendData[13] = (byte)distanceByte[3];
            for (i = 3; i < 24; i++)
            {
                check_sum += sendData[i];
            }
            sendData[24] = (byte)(tmp - check_sum);
            richTextBox1.Text = HexToString(sendData);
            serialPort1.Write(sendData, 0, sendData.Length);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            this.gMapControl1.MapProvider = GMap.NET.MapProviders.GoogleChinaMapProvider.Instance;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            this.gMapControl1.MapProvider = GMap.NET.MapProviders.GoogleChinaSatelliteMapProvider .Instance;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

            try
            {
                Closing = true;
                while (Listening) Application.DoEvents();
                //打开时点击，则关闭串口   
                serialPort1.Close();
                Closing = false;
            }
            catch
            {

            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            int i = 0;
            int distance;
            byte check_sum = 0x00;
            byte tmp = 0xFF;
            byte[] sendData = new byte[] { 0x7E, 0x00, 0x15, 0x01, 0x01, 0x22, 0x22, 0x00, 0xAF, 0xFB, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xAA };
            distance = Int32.Parse(textBox4.Text);
            byte[] distanceByte = System.BitConverter.GetBytes(distance);
            sendData[10] = (byte)distanceByte[0];
            sendData[11] = (byte)distanceByte[1];
            sendData[12] = (byte)distanceByte[2];
            sendData[13] = (byte)distanceByte[3];
            for (i = 3; i < 24; i++)
            {
                check_sum += sendData[i];
            }
            sendData[24] = (byte)(tmp - check_sum);
            richTextBox1.Text = HexToString(sendData);
            serialPort1.Write(sendData, 0, sendData.Length);
        }
    }
}
