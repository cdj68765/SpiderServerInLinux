using Client.Properties;
using Cowboy.WebSockets;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SevenZip;
using System.Drawing.Imaging;
using LiteDB;

namespace Client
{
    public partial class Form3 : Form
    {
        private AsyncWebSocketClient client = null;
        private List<T66yImgData> Temp = new List<T66yImgData>();
        private int Count = 0;
        private bool ReWrite = false;

        public Form3(string v)
        {
            InitializeComponent();
            int NSize = 0;
            int OSize = 0;
            int Size = 0;
            int Size2 = 0;
            int Size3 = 0;

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
                    {
                        var SISDB = db.GetCollection<T66yImgData>("ImgData");
                        var Findd = SISDB.Find(x => x.Date == v);
                        foreach (var Img in Findd)
                        {
                            this.Invoke(new MethodInvoker(() =>
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
                    await client.SendTextAsync($"ReDownloadT66y|{Temp[item].id}");
                }
            });
        }
    }
}