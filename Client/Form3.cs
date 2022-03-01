using Client.Properties;
using Cowboy.WebSockets;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class Form3 : Form
    {
        private AsyncWebSocketClient client = null;
        private List<T66yImgData> Temp = new List<T66yImgData>();
        private int Count = 0;
        private bool ReWrite = false;
        string date;
        public Form3(string v)
        {
            InitializeComponent();
            date = v;
        }

        [Serializable]
        internal class T66yImgData
        {
            public string id { get; set; }
            public string Date { get; set; }
            public byte[] img { get; set; }
            public List<int> FromList { get; set; }
            public bool Status => img != null;

            internal byte[] ToByte()
            {
                using var stream = new MemoryStream();
                IFormatter Fileformatter = new BinaryFormatter();
                Fileformatter.Serialize(stream, this);
                return stream.ToArray();
            }

            public static T66yImgData ToClass(byte[] data)
            {
                using var stream = new MemoryStream(data);
                IFormatter Fileformatter = new BinaryFormatter();
                Fileformatter.Binder = new UBinder();
                return Fileformatter.Deserialize(stream) as T66yImgData;
            }

            public class UBinder : SerializationBinder
            {
                public override Type BindToType(string assemblyName, string typeName)
                {
                    if (typeName.EndsWith("T66yImgData"))
                    {
                        return typeof(T66yImgData);
                    }
                    return (Assembly.GetExecutingAssembly()).GetType(typeName);
                }
            }
        }

        [Serializable]
        internal class T66yData
        {
            public int id { get; set; }
            public string Title { get; set; }
            public string Uri { get; set; }
            public string Date { get; set; }
            public string HtmlData { get; set; }
            public List<string> MainList { get; set; }
            public List<string> QuoteList { get; set; }
            public bool Status { get; set; }
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            System.Diagnostics.Process.Start($"http://www.t66y.com/read.php?tid={listView1.SelectedItems[0].Text}");
        }

        private void 重新下载该图片ToolStripMenuItem_ClickAsync(object sender, EventArgs e)
        {
            var SelectList = new List<int>();
            foreach (int item in listView1.SelectedIndices)
            {
                SelectList.Add(item);
            }
            Task.Factory.StartNew(async () =>
            {
                ReWrite = true;
                foreach (int item in SelectList)
                {
                    var Find = Temp[item];
                    //File.WriteAllBytes("Error.txt", Find.img);
                    // await client.SendTextAsync($"ReDownloadT66y|{Find.id}");
                }
            });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int NSize = 0;
            int OSize = 0;
            int Size = 0;
            int Size2 = 0;
            int Size3 = 0;
            listView1.Dock = DockStyle.Fill;
            button1.Visible = false;
            button2.Visible = false;
            textBox1.Visible = false;
            if (Class1.MainForm.Connect)
            {
                Task.Factory.StartNew(async () =>
                {
                    var uri = new Uri($"ws://{Settings.Default.ip}:{Settings.Default.point}/Data");
                    client = new AsyncWebSocketClient(uri, null, (c, b, f, e) =>
                    {
                        this.Invoke(new MethodInvoker(() =>
                        {
                            var Img = T66yImgData.ToClass(b);

                            if (!ReWrite)
                            {
                                Temp.Add(Img);
                                if (Img.Status)
                                {
                                    imageList1.Images.Add(Image.FromStream(new MemoryStream(Img.img)));
                                    listView1.Items.Add(Img.FromList.First().ToString());
                                    listView1.Items[Count].ImageIndex = Count;
                                }
                                else
                                {
                                    var Bit = new Bitmap(256, 256);
                                    for (int i = 0; i < 256; i++)
                                    {
                                        Bit.SetPixel(i, i, Color.Black);
                                    }
                                    imageList1.Images.Add(Bit);
                                    listView1.Items.Add(Img.FromList.First().ToString());
                                    listView1.Items[Count].ImageIndex = Count;
                                }
                                Count += 1;
                            }
                            else
                            {
                                var Find = Temp.FindIndex(x => x.id == Img.id);
                                imageList1.Images.Add(Image.FromStream(new MemoryStream(Img.img)));
                                listView1.Items[Find].ImageIndex = imageList1.Images.Count - 1;
                            }

                            /*if (Count == 100)
                            {
                                c.Shutdown();
                            }*/
                        }));
                        return Task.CompletedTask;
                    }
                   , null, null, null);
                    await client.Connect();
                    using var db = new LiteDatabase(@"Filename=Z:\publish\T66y.db;Connection=Shared;ReadOnly=True");
                    //using var db = new LiteDatabase(@"Filename=D:\T66y.db;Connection=Shared;ReadOnly=True");
                    {
                        var SISDB = db.GetCollection<T66yImgData>("ImgData");
                        var Findd = SISDB.Find(x => x.Date == date).ToArray();
                        foreach (var Img in Findd)
                        {
                            this.Invoke(new MethodInvoker(() =>
                            {
                                Temp.Add(Img);
                                if (Img.Status && Img.img.Length > 1024)
                                {
                                    imageList1.Images.Add(Image.FromStream(new MemoryStream(Img.img)));
                                    listView1.Items.Add(Img.FromList.First().ToString());
                                    listView1.Items[Count].ImageIndex = Count;
                                }
                                else
                                {
                                    var Bit = new Bitmap(256, 256);

                                    for (int i = 0; i < 256; i++)
                                    {
                                        Bit.SetPixel(i, i, Color.Black);
                                    }
                                    if (Img.img != null)
                                    {
                                        var g = Graphics.FromImage(Bit);
                                        g.DrawString(Encoding.UTF8.GetString(Img.img), this.Font, Brushes.Black, 10, 10);
                                    }
                                    imageList1.Images.Add(Bit);
                                    listView1.Items.Add(Img.FromList.First().ToString());
                                    listView1.Items[Count].ImageIndex = Count;
                                }
                                Count += 1;
                            }));
                        }
                    }
                    //await client.SendTextAsync($"GetT66y|GetImgFromDate|{v}");
                });
            }
            else
            {
                this.Close();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            listView1.Dock = DockStyle.Fill;
            button1.Visible = false;
            button2.Visible = false;
            textBox1.Visible = false;

            if (Class1.MainForm.Connect)
            {
                Task.Factory.StartNew(async () =>
                {
                    var uri = new Uri($"ws://{Settings.Default.ip}:{Settings.Default.point}/Data");
                    client = new AsyncWebSocketClient(uri, null, (c, b, f, e) =>
                    {
                        this.Invoke(new MethodInvoker(() =>
                        {
                            var Img = T66yImgData.ToClass(b);

                            if (!ReWrite)
                            {
                                Temp.Add(Img);
                                if (Img.Status)
                                {
                                    imageList1.Images.Add(Image.FromStream(new MemoryStream(Img.img)));
                                    listView1.Items.Add(Img.FromList.First().ToString());
                                    listView1.Items[Count].ImageIndex = Count;
                                }
                                else
                                {
                                    var Bit = new Bitmap(256, 256);
                                    for (int i = 0; i < 256; i++)
                                    {
                                        Bit.SetPixel(i, i, Color.Black);
                                    }
                                    imageList1.Images.Add(Bit);
                                    listView1.Items.Add(Img.FromList.First().ToString());
                                    listView1.Items[Count].ImageIndex = Count;
                                }
                                Count += 1;
                            }
                            else
                            {
                                var Find = Temp.FindIndex(x => x.id == Img.id);
                                imageList1.Images.Add(Image.FromStream(new MemoryStream(Img.img)));
                                listView1.Items[Find].ImageIndex = imageList1.Images.Count - 1;
                            }

                            /*if (Count == 100)
                            {
                                c.Shutdown();
                            }*/
                        }));
                        return Task.CompletedTask;
                    }
                   , null, null, null);
                    await client.Connect();
                    using var db = new LiteDatabase(@"Filename=Z:\publish\T66y.db;Connection=Shared;ReadOnly=True");
                    //using var db = new LiteDatabase(@"Filename=D:\T66y.db;Connection=Shared;ReadOnly=True");
                    {
                        var SISMain = db.GetCollection<T66yData>("T66yData");
                        var SISDB = db.GetCollection<T66yImgData>("ImgData");

                        foreach (var item in SISMain.Find(x => x.Title.Contains(textBox1.Text)))
                        {
                            foreach (var item2 in from D1 in item.MainList where D1.ToLower().StartsWith("http") select D1)
                            {
                                foreach (var Img in SISDB.Find(x => x.id == item2))
                                {
                                    this.Invoke(new MethodInvoker(() =>
                                    {
                                        Temp.Add(Img);
                                        if (Img.Status && Img.img.Length > 1024)
                                        {
                                            imageList1.Images.Add(Image.FromStream(new MemoryStream(Img.img)));
                                            listView1.Items.Add(Img.FromList.First().ToString());
                                            listView1.Items[Count].ImageIndex = Count;
                                        }
                                        else
                                        {
                                            var Bit = new Bitmap(256, 256);

                                            for (int i = 0; i < 256; i++)
                                            {
                                                Bit.SetPixel(i, i, Color.Black);
                                            }
                                            if (Img.img != null)
                                            {
                                                var g = Graphics.FromImage(Bit);
                                                g.DrawString(Encoding.UTF8.GetString(Img.img), this.Font, Brushes.Black, 10, 10);
                                            }
                                            imageList1.Images.Add(Bit);
                                            listView1.Items.Add(Img.FromList.First().ToString());
                                            listView1.Items[Count].ImageIndex = Count;
                                        }
                                        Count += 1;
                                    }));
                                }
                            }

                        }
                    }
                    //await client.SendTextAsync($"GetT66y|GetImgFromDate|{v}");
                });
            }
            else
            {
                this.Close();
            }
        }
    }
}