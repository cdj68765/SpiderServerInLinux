using Client.Properties;
using Cowboy.WebSockets;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

//netsh winsock reset
namespace Client
{
    public partial class Form1 : Form
    {
        public server _server;
        public bool Connect;

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
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    if (_server == null)
                    {
                        Class1.MainForm.Connecting(false);
                        continue;
                    }
                    try
                    {
                        if (_server._client.State == Cowboy.WebSockets.WebSocketState.Open)
                        {
                            Class1.MainForm.Connecting(true);
                        }
                        else
                        {
                            Class1.MainForm.Connecting(false);
                        }
                    }
                    catch (Exception)
                    {
                        Class1.MainForm.Connecting(false);
                    }
                }
            });
            // button1.PerformClick();
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
            string GetIp(string domain)
            {
                if (!IPAddress.TryParse(domain, out IPAddress ip))
                {
                    try
                    {
                        domain = domain.Replace("http://", "").Replace("https://", "");
                        IPHostEntry hostEntry = Dns.GetHostEntry(domain);
                        IPEndPoint ipEndPoint = new IPEndPoint(hostEntry.AddressList[0], 0);
                        return ipEndPoint.Address.ToString();
                    }
                    catch (Exception)
                    {
                    }
                }
                return domain;
            }
            //textBox1.Text = GetIp(textBox1.Text);
            Settings.Default.ip = textBox1.Text;
            Settings.Default.point = textBox2.Text;
            Settings.Default.Save();
            Task.Factory.StartNew(async () =>
            {
                if (_server != null)
                {
                    await _server._client.Close(Cowboy.WebSockets.WebSocketCloseCode.NormalClosure);
                    _server._client.Dispose();
                    _server = null;
                }
                else
                {
                    _server = new server(textBox1.Text, textBox2.Text);
                }
            });
        }

        public void ShowStatus(string info)
        {
            BeginInvoke(new MethodInvoker(() =>
            {
                listBox1.Items.Add(info);
            }));
        }

        private async void Button2_ClickAsync(object sender, EventArgs e)
        {
            if (Connect)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    BinaryFormatter Formatter = new BinaryFormatter();
                    var Clone = new OnlineOpera()
                    {
                        ConnectPoint = int.Parse(textBox2.Text),
                        SocksPoint = int.Parse(textBox8.Text),
                        NyaaAddress = textBox4.Text,
                        JavAddress = textBox5.Text,
                        MiMiAiAddress = textBox6.Text,
                        ssr_url = textBox7.Text,
                        SocksCheck = checkBox1.Checked,
                        AutoRun = AutoRun.Checked,
                        ssr4Nyaa = textBox9.Text,
                        NyaaSocksCheck = checkBox2.Checked,
                        NyaaSocksPoint = int.Parse(textBox3.Text)
                    };

                    Formatter.Serialize(stream, Clone);
                    await _server._client.SendBinaryAsync(stream.ToArray());
                }
            }
        }

        private void Button3_ClickAsync(object sender, EventArgs e)
        {
            if (Connect)
            {
                _server.Connect2SetAsync("Restart");
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            var F = new Form6();
            F.Show();
            /*   if (Connect)
               {
                   //_server.Connect2SetAsync("CloseMiMiStory");
                   Task.Factory.StartNew(async () =>
                   {
                       var uri = new Uri($"ws://{Settings.Default.ip}:{Settings.Default.point}/Data");
                       var client = new AsyncWebSocketClient(uri, new ServerDataBaseOperation());
                       await client.Connect();
                       await client.SendTextAsync("GetStory|*");
                   });
               }*/
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            var F = new Form6();
            F.Show();
            /*if (Connect)
            {
                Task.Factory.StartNew(async () =>
                {
                    var uri = new Uri($"ws://{Settings.Default.ip}:{Settings.Default.point}/Data");
                    var client = new AsyncWebSocketClient(uri, new ServerDataBaseOperation());
                    await client.Connect();
                    await client.SendTextAsync("GetNullStory");
                });
            }*/
        }

        internal void UpdateUI()
        {
            if (listBox1.Items.Count != Class1.OnlineOpera.LocalInfo.Count)
            {
                listBox1.Items.Clear();
                listBox1.Items.AddRange(Class1.OnlineOpera.LocalInfo.ToArray());
            }

            if (listBox2.Items.Count != Class1.OnlineOpera.RemoteInfo.Count)
            {
                listBox2.Items.Clear();
                listBox2.Items.AddRange(Class1.OnlineOpera.RemoteInfo.ToArray());
            }

            if (double.TryParse(Class1.OnlineOpera.MiMiInterval, out double MiMiInterval))
            {
                var mimi = TimeSpan.FromMilliseconds(MiMiInterval) - Class1.OnlineOpera.MiMiSpan;
                MiMi.Text = $"{mimi:hh\\:mm\\:ss}\n{DateTime.Now.AddMilliseconds(mimi.TotalMilliseconds):MM月dd日HH时mm分}";
            }
            else MiMi.Text = Class1.OnlineOpera.MiMiInterval;
            if (double.TryParse(Class1.OnlineOpera.JavInterval, out double JavInterval))
            {
                var Jav = TimeSpan.FromMilliseconds(JavInterval) - Class1.OnlineOpera.JavSpan;
                JAV.Text = $"{Jav:hh\\:mm\\:ss}\n{DateTime.Now.AddMilliseconds(Jav.TotalMilliseconds):MM月dd日HH时mm分}";
            }
            else JAV.Text = Class1.OnlineOpera.JavInterval;
            if (double.TryParse(Class1.OnlineOpera.NyaaInterval, out double NyaaInterval))
            {
                var Nyaa = TimeSpan.FromMilliseconds(NyaaInterval) - Class1.OnlineOpera.NyaaSpan;
                NYAA.Text = $"{Nyaa:hh\\:mm\\:ss}\n{DateTime.Now.AddMilliseconds(Nyaa.TotalMilliseconds):MM月dd日HH时mm分}";
            }
            else NYAA.Text = Class1.OnlineOpera.NyaaInterval;

            if (double.TryParse(Class1.OnlineOpera.MiMiStoryInterval, out double MiMiStoryInterval))
            {
                var mimistory = TimeSpan.FromMilliseconds(MiMiStoryInterval) - Class1.OnlineOpera.MiMiStorySpan;
                MiMiStory.Text = $"{mimistory:hh\\:mm\\:ss}\n{DateTime.Now.AddMilliseconds(mimistory.TotalMilliseconds):MM月dd日HH时mm分}";
            }
            else MiMiStory.Text = Class1.OnlineOpera.MiMiStoryInterval;

            if (double.TryParse(Class1.OnlineOpera.T66yInterval, out double T66yInterval))
            {
                var t66y = TimeSpan.FromMilliseconds(T66yInterval) - Class1.OnlineOpera.GetT66ySpan;
                T66y.Text = $"{t66y:hh\\:mm\\:ss}\n{DateTime.Now.AddMilliseconds(t66y.TotalMilliseconds):MM月dd日HH时mm分}";
            }
            else T66y.Text = Class1.OnlineOpera.T66yInterval;
            T66yOther.Text = Class1.OnlineOpera.T66yOtherMessage;
            T66yOldOther.Text = Class1.OnlineOpera.T66yOtherOldMessage;
            SIS.Text = Class1.OnlineOpera.SisInterval;
            Memory.Text = Class1.OnlineOpera.Memory;
            Download.Text = HumanReadableFilesize(Class1.OnlineOpera.TotalDownloadBytes);
            label9.Text = Class1.OnlineOpera.SisIndex.ToString();
            label10.Text = Class1.OnlineOpera.DataCount.D0.ToString();
            label11.Text = Class1.OnlineOpera.DataCount.D1.ToString();
            label12.Text = Class1.OnlineOpera.DataCount.D2.ToString();
            label13.Text = Class1.OnlineOpera.DataCount.D3.ToString();
            label14.Text = Class1.OnlineOpera.DataCount.T66y.ToString();

            if (!Class1.OnlineOpera.OnlyList)
            {
                if (textBox2.Text != Class1.OnlineOpera.ConnectPoint.ToString())
                {
                    if (Connect)
                    {
                        _server._client.Shutdown();
                        _server._client.Dispose();
                        _server = new server(textBox1.Text, Class1.OnlineOpera.ConnectPoint.ToString());
                        textBox2.Text = Class1.OnlineOpera.ConnectPoint.ToString();
                    }
                }
                textBox3.Text = Class1.OnlineOpera.NyaaSocksPoint.ToString();
                checkBox2.Checked = Class1.OnlineOpera.NyaaSocksCheck;
                textBox9.Text = Class1.OnlineOpera.ssr4Nyaa;
                textBox4.Text = Class1.OnlineOpera.NyaaAddress;
                textBox5.Text = Class1.OnlineOpera.JavAddress;
                textBox6.Text = Class1.OnlineOpera.MiMiAiAddress;
                textBox7.Text = Class1.OnlineOpera.ssr_url;
                textBox8.Text = Class1.OnlineOpera.SocksPoint.ToString();
                label4.Text = Class1.OnlineOpera.SSRPoint.ToString();
                label5.Text = Class1.OnlineOpera.NyaaSSRPoint.ToString();
                checkBox1.Checked = Class1.OnlineOpera.SocksCheck;
                AutoRun.Checked = Class1.OnlineOpera.AutoRun;
            }
        }

        private string HumanReadableFilesize(double size)
        {
            var units = new[] { "B", "KB", "MB", "GB", "TB", "PB" };
            double mod = 1024.0;
            var DoubleCount = new List<double>();
            while (size >= mod)
            {
                size /= mod;
                DoubleCount.Add(size);
            }
            var Ret = "";
            for (int j = DoubleCount.Count; j > 0; j--)
            {
                if (j == DoubleCount.Count)
                {
                    Ret += $"{Math.Floor(DoubleCount[j - 1])}{units[j]}";
                }
                else
                {
                    Ret += $"{Math.Floor(DoubleCount[j - 1] - (Math.Floor(DoubleCount[j]) * 1024))}{units[j]}";
                }
            }
            return Ret;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (Connect)
            {
                _server.Connect2SetAsync("CloseT66y");
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (Connect)
            {
                _server.Connect2SetAsync("StartT66y");
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            try
            {
                var Show = new Form3(dateTimePicker1.Value.ToString("yyyy-MM-dd"));
                Show.Show();
            }
            catch (Exception)
            {
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (Connect)
            {
                _server.Connect2SetAsync("Close");
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            var Show = new Form2(dateTimePicker1.Value.ToString("yyyy-MM-dd"));
            Show.Show();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (Connect)
            {
                _server.Connect2SetAsync("StartSIS");
            }
        }

        private void 复制ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetDataObject(listBox1.SelectedItem.ToString());
        }

        private void button12_Click(object sender, EventArgs e)
        {
            var Show = new Form4(dateTimePicker1.Value.ToString("yyyy-M-d"));
            Show.Show();
        }

        private void button13_Click(object sender, EventArgs e)
        {
            if (Connect)
            {
                _server.Connect2SetAsync("ReLoad");
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            if (Connect)
            {
                _server.Connect2SetAsync("StartJav");
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            var Show = new Form5(dateTimePicker1.Value.ToString("yy-MM-dd"));
            Show.Show();
        }
    }
}