using Client.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

//netsh winsock reset
namespace Client
{
    public partial class Form1 : Form
    {
        private server _server;
        private bool Connect;

        public Form1()
        {
            InitializeComponent();
            Class1.MainForm = this;
            button1.Text = "已断开";
            if (!string.IsNullOrEmpty(Settings.Default.ip))
            {
                textBox1.Text = Settings.Default.ip;
                textBox2.Text = Settings.Default.point;
            }
            else
            {
                Settings.Default.ip = textBox1.Text;
                Settings.Default.point = textBox2.Text;
                Settings.Default.Save();
            }
            ThreadPool.QueueUserWorkItem((obj) =>
            {
                _server = new server(Settings.Default.ip, Settings.Default.point);
            });
        }

        internal void Connecting(bool ConnectControl)
        {
            BeginInvoke(new MethodInvoker(() =>
            {
                if (ConnectControl)
                {
                    Settings.Default.ip = textBox1.Text;
                    Settings.Default.point = textBox2.Text;
                    Settings.Default.Save();
                    button1.Text = "已连接";
                    Connect = true;
                }
                else
                {
                    button1.Text = "已断开";
                    Connect = false;
                }
            }));
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (_server != null)
            {
                _server._client.Close(Cowboy.WebSockets.WebSocketCloseCode.NormalClosure);
                _server._client.Dispose();
                _server = null;
            }
            else
            {
                _server = new server(textBox1.Text, textBox2.Text);
            }
        }

        public void ShowStatus(string info)
        {
            BeginInvoke(new MethodInvoker(() =>
            {
                listBox1.Items.Add(info);
            }));
        }

        private GlobalSet globalSet;

        internal void Init(GlobalSet globalSet)
        {
            BeginInvoke(new MethodInvoker(() =>
            {
                this.globalSet = globalSet;
                textBox3.Text = globalSet.Socks5Point.ToString();
                checkBox1.Checked = globalSet.SocksCheck;
                textBox4.Text = globalSet.NyaaAddress;
                textBox5.Text = globalSet.JavAddress;
            }));
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            if (Connect)
            {
                globalSet.Socks5Point = int.Parse(textBox3.Text);
                globalSet.SocksCheck = checkBox1.Checked;
                globalSet.NyaaAddress = textBox4.Text;
                globalSet.JavAddress = textBox5.Text;
                globalSet.Save(_server);
            }
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            if (Connect)
            {
                _server.Connect2Set();
            }
        }
    }
}