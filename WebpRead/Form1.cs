using LiteDB;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebPWrapper;
using BotBiliBili.Utils;
using System.Threading;

namespace WebpRead
{
    public partial class Form1 : Form
    {
        private string BasrUri = @"Z:\Image\";
        private List<string> DirInfo = new List<string>();
        public CancellationTokenSource Cancel = new CancellationTokenSource();
        private List<string> ImageDate = new List<string>();
        private string DataBase = "";
        private bool Writing = false;

        public Form1()
        {
            InitializeComponent();
            SetStyle(
              ControlStyles.OptimizedDoubleBuffer
              | ControlStyles.ResizeRedraw
              | ControlStyles.Selectable
              | ControlStyles.AllPaintingInWmPaint
              | ControlStyles.UserPaint
              | ControlStyles.SupportsTransparentBackColor
              | ControlStyles.DoubleBuffer, true);
            DirInfo = new DirectoryInfo(BasrUri).GetFiles().Select(x => x.Name).ToList(); DirInfo.Sort();
            if (DirInfo.Count > 0)
            {
                dateTimePicker1.MinDate = DateTime.Parse(DirInfo[1].Replace(".db", ""));
                dateTimePicker1.MaxDate = DateTime.Parse(DirInfo.Last().Replace(".db", ""));
            }
            FormClosing += async delegate
             {
                 DataBase = "";
                 while (Writing)
                 {
                     BeginInvoke(new Action(() =>
                     {
                         label1.Text = "等待退出中";
                     }));
                     await Task.Delay(100);
                 }
             };
            //Tempdb.Dispose();
        }

        internal List<WebpImage> webpImages = new List<WebpImage>();

        private void button1_Click(object sender, EventArgs e)
        {
            webpImages.Clear();
            GC.Collect();
            DataBase = $"{DateTime.Parse(dateTimePicker1.Text):yyyy-MM-dd}.db";
            Task.Factory.StartNew(async () =>
            {
                var CurrectDataBase = DataBase;
                while (Writing)
                {
                    BeginInvoke(new Action(() =>
                    {
                        label1.Text = "等待退出中";
                    }));
                    await Task.Delay(100);
                }
                Invoke(new Action(() =>
                {
                    imageList1.Images.Clear();
                    listView1.Items.Clear();
                    Refresh();
                    label1.Text = "载入中";
                }));
                Writing = true;
                var Tempdb = new LiteDatabase(File.Open($@"{BasrUri}{CurrectDataBase}", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                var WebpData = Tempdb.GetCollection<WebpImage>("WebpData");
                var Count = 0;
                var TotalCount = WebpData.Count();
                var Jpg = 0;
                var WebpJpg = 0;
                var Gif = 0;
                var WebpGif = 0;
                var T66y = 0;
                var SIS = 0;
                foreach (var item in WebpData.FindAll().OrderBy(x => x.FromList.First()))
                {
                    if (!label1.Text.StartsWith("载入中")) break;
                    if (item.From == "T66y") T66y += 1;
                    else SIS += 1;
                    try
                    {
                        if (item.Type == "jpg")
                        {
                            if (GetFileImageTypeFromHeader(item.img) == ImageType.JPEG)
                            {
                                Jpg += 1;
                                var Img = Image.FromStream(new MemoryStream(item.img));
                                if (CurrectDataBase != DataBase) break;
                                Invoke(new Action(() =>
                                {
                                    imageList1.Images.Add(Img);
                                    listView1.Items.Add($"{Count}-{item.FromList.First()}-{item.From}");
                                    listView1.Items[Count].ImageIndex = Count;
                                    ImageDate.Add(item.id);
                                    label1.Text = $"载入中:{Count}/{TotalCount}    |jpg:{Jpg}  |WebpJpg:{WebpJpg}  |Gif:{Gif}  |WebpGif:{WebpGif}  |T66y:{T66y}    |SIS:{SIS}";
                                }));
                                Count += 1;
                            }
                            else
                            {
                                WebpJpg += 1;
                                var Img = new WebP().GetThumbnailFast(item.img, 256, 256);
                                if (CurrectDataBase != DataBase) break;
                                Invoke(new Action(() =>
                                {
                                    imageList1.Images.Add(Img);
                                    listView1.Items.Add($"{Count}-{item.FromList.First()}-{item.From}");
                                    listView1.Items[Count].ImageIndex = Count;
                                    ImageDate.Add(item.id);
                                    label1.Text = $"载入中:{Count}/{TotalCount}    |jpg:{Jpg}  |WebpJpg:{WebpJpg}  |Gif:{Gif}  |WebpGif:{WebpGif}  |T66y:{T66y}    |SIS:{SIS}";
                                }));
                                Count += 1;
                            }
                        }
                        else if (item.Type == "gif")
                        {
                            if (GetFileImageTypeFromHeader(item.img) == ImageType.GIF)
                            {
                                Gif += 1;
                                var Img = Image.FromStream(new MemoryStream(item.img));
                                if (CurrectDataBase != DataBase) break;
                                Invoke(new Action(() =>
                                {
                                    imageList1.Images.Add(Img);
                                    listView1.Items.Add($"{Count}-{item.FromList.First()}-{item.From}");
                                    listView1.Items[Count].ImageIndex = Count;
                                    ImageDate.Add(item.id);
                                    label1.Text = $"载入中:{Count}/{TotalCount}    |jpg:{Jpg}  |WebpJpg:{WebpJpg}  |Gif:{Gif}  |WebpGif:{WebpGif}  |T66y:{T66y}    |SIS:{SIS}";
                                }));
                                Count += 1;
                            }
                            else
                            {
                                WebpGif += 1;
                                var Img = new SimpleAnimDecoder().DecodeFromBytes(item.img).Frames.FirstOrDefault().Image;
                                if (CurrectDataBase != DataBase) break;
                                Invoke(new Action(() =>
                                {
                                    imageList1.Images.Add(Img);
                                    listView1.Items.Add($"{Count}-{item.FromList.First()}-{item.From}");
                                    listView1.Items[Count].ImageIndex = Count;
                                    ImageDate.Add(item.id);
                                    label1.Text = $"载入中:{Count}/{TotalCount}    |jpg:{Jpg}  |WebpJpg:{WebpJpg}  |Gif:{Gif}  |WebpGif:{WebpGif}  |T66y:{T66y}    |SIS:{SIS}";
                                }));
                                Count += 1;
                            }
                        }
                        else
                        {
                            var TT = GetFileImageTypeFromHeader(item.img);
                            if (TT == ImageType.GIF)
                            {
                                Gif += 1;
                                var Img = Image.FromStream(new MemoryStream(item.img));
                                if (CurrectDataBase != DataBase) break;
                                Invoke(new Action(() =>
                                {
                                    imageList1.Images.Add(Img);
                                    listView1.Items.Add($"{Count}-{item.FromList.First()}-{item.From}");
                                    listView1.Items[Count].ImageIndex = Count;
                                    ImageDate.Add(item.id);
                                    label1.Text = $"载入中:{Count}/{TotalCount}    |jpg:{Jpg}  |WebpJpg:{WebpJpg}  |Gif:{Gif}  |WebpGif:{WebpGif}  |T66y:{T66y}    |SIS:{SIS}";
                                }));
                                Count += 1;
                            }
                            else if (TT == ImageType.JPEG)
                            {
                                Jpg += 1;
                                var Img = Image.FromStream(new MemoryStream(item.img));
                                if (CurrectDataBase != DataBase) break;
                                Invoke(new Action(() =>
                                {
                                    imageList1.Images.Add(Img);
                                    listView1.Items.Add($"{Count}-{item.FromList.First()}-{item.From}");
                                    listView1.Items[Count].ImageIndex = Count;
                                    ImageDate.Add(item.id);
                                    label1.Text = $"载入中:{Count}/{TotalCount}    |jpg:{Jpg}  |WebpJpg:{WebpJpg}  |Gif:{Gif}  |WebpGif:{WebpGif}  |T66y:{T66y}    |SIS:{SIS}";
                                }));
                                Count += 1;
                            }
                            else if (TT == ImageType.Unknown)
                            {
                                WebpGif += 1;
                                var Img = new SimpleAnimDecoder().DecodeFromBytes(item.img).Frames.FirstOrDefault().Image;
                                if (CurrectDataBase != DataBase) break;
                                Invoke(new Action(() =>
                                {
                                    imageList1.Images.Add(Img);
                                    listView1.Items.Add($"{Count}-{item.FromList.First()}-{item.From}");
                                    listView1.Items[Count].ImageIndex = Count;
                                    ImageDate.Add(item.id);
                                    label1.Text = $"载入中:{Count}/{TotalCount}    |jpg:{Jpg}  |WebpJpg:{WebpJpg}  |Gif:{Gif}  |WebpGif:{WebpGif}  |T66y:{T66y}    |SIS:{SIS}";
                                }));
                                Count += 1;
                            }
                            else
                            {
                                Jpg += 1;
                                var Img = Image.FromStream(new MemoryStream(item.img));
                                if (CurrectDataBase != DataBase) break;
                                Invoke(new Action(() =>
                                {
                                    imageList1.Images.Add(Img);
                                    listView1.Items.Add($"{Count}-{item.FromList.First()}-{item.From}");
                                    listView1.Items[Count].ImageIndex = Count;
                                    ImageDate.Add(item.id);
                                    label1.Text = $"载入中:{Count}/{TotalCount}    |jpg:{Jpg}  |WebpJpg:{WebpJpg}  |Gif:{Gif}  |WebpGif:{WebpGif}  |T66y:{T66y}    |SIS:{SIS}";
                                }));
                                Count += 1;
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                    webpImages.Add(item);
                }
                Writing = false;
                BeginInvoke(new Action(() =>
                {
                    label1.Text = $"载入完毕:{Count}/{TotalCount}    |jpg:{Jpg}  |WebpJpg:{WebpJpg}  |Gif:{Gif}  |WebpGif:{WebpGif}  |T66y:{T66y}    |SIS:{SIS}";
                }));
            });
        }

        [Serializable]
        internal class WebpImage
        {
            public string id { get; set; }

            public string Date { get; set; }
            public string Hash { get; set; }

            public byte[] img { get; set; }
            public List<int> FromList { get; set; }
            public bool Status { get; set; }
            public string Type { get; set; }
            public string From { get; set; }

            public byte[] Send()
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    BinaryFormatter Formatter = new BinaryFormatter();
                    Formatter.Serialize(stream, this);
                    return stream.ToArray();
                }
            }

            public static WebpImage ToClass(byte[] data)
            {
                var stream = new MemoryStream(data);
                IFormatter Fileformatter = new BinaryFormatter();
                Fileformatter.Binder = new UBinder();
                return Fileformatter.Deserialize(stream) as WebpImage;
            }

            public class UBinder : SerializationBinder
            {
                public override Type BindToType(string assemblyName, string typeName)
                {
                    if (typeName.EndsWith("WebpImage"))
                    {
                        return typeof(WebpImage);
                    }
                    return Assembly.GetExecutingAssembly().GetType(typeName);
                }
            }
        }

        [Serializable]
        public class OriImage
        {
            public string id { get; set; }
            public string From { get; set; }

            public string Date { get; set; }
            public string Hash { get; set; }
            public byte[] img { get; set; }
            public List<int> FromList { get; set; }
            public bool Status { get; set; }

            public byte[] Send()
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    BinaryFormatter Formatter = new BinaryFormatter();
                    Formatter.Serialize(stream, this);
                    return stream.ToArray();
                }
            }

            public static OriImage ToClass(byte[] data)
            {
                var stream = new MemoryStream(data);
                IFormatter Fileformatter = new BinaryFormatter();
                Fileformatter.Binder = new UBinder();
                return Fileformatter.Deserialize(stream) as OriImage;
            }

            public class UBinder : SerializationBinder
            {
                public override Type BindToType(string assemblyName, string typeName)
                {
                    if (typeName.EndsWith("OriImage"))
                    {
                        return typeof(OriImage);
                    }
                    return Assembly.GetExecutingAssembly().GetType(typeName);
                }
            }
        }

        public enum ImageType
        {
            Unknown,
            JPEG,
            PNG,
            GIF,
            BMP,
            TIFF,
        }

        internal ImageType GetFileImageTypeFromHeader(byte[] headerBytes)
        {
            //JPEG:
            if (headerBytes[0] == 0xFF &&//FF D8
                headerBytes[1] == 0xD8 &&
                (
                 (headerBytes[6] == 0x4A &&//'JFIF'
                  headerBytes[7] == 0x46 &&
                  headerBytes[8] == 0x49 &&
                  headerBytes[9] == 0x46)
                  ||
                 (headerBytes[6] == 0x45 &&//'EXIF'
                  headerBytes[7] == 0x78 &&
                  headerBytes[8] == 0x69 &&
                  headerBytes[9] == 0x66)
                ) &&
                headerBytes[10] == 00)
            {
                return ImageType.JPEG;
            }
            //PNG
            if (headerBytes[0] == 0x89 && //89 50 4E 47 0D 0A 1A 0A
                headerBytes[1] == 0x50 &&
                headerBytes[2] == 0x4E &&
                headerBytes[3] == 0x47 &&
                headerBytes[4] == 0x0D &&
                headerBytes[5] == 0x0A &&
                headerBytes[6] == 0x1A &&
                headerBytes[7] == 0x0A)
            {
                return ImageType.PNG;
            }
            //GIF
            if (headerBytes[0] == 0x47 &&//'GIF'
                headerBytes[1] == 0x49 &&
                headerBytes[2] == 0x46)
            {
                return ImageType.GIF;
            }
            //BMP
            if (headerBytes[0] == 0x42 &&//42 4D
                headerBytes[1] == 0x4D)
            {
                return ImageType.BMP;
            }
            //TIFF
            if ((headerBytes[0] == 0x49 &&//49 49 2A 00
                 headerBytes[1] == 0x49 &&
                 headerBytes[2] == 0x2A &&
                 headerBytes[3] == 0x00)
                 ||
                (headerBytes[0] == 0x4D &&//4D 4D 00 2A
                 headerBytes[1] == 0x4D &&
                 headerBytes[2] == 0x00 &&
                 headerBytes[3] == 0x2A))
            {
                return ImageType.TIFF;
            }

            return ImageType.Unknown;
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                var sp = listView1.SelectedItems[0].Text.Split('-');
                if (sp[2] == "T66y")
                    System.Diagnostics.Process.Start($"http://www.t66y.com/read.php?tid={sp[1]}");
                else
                    System.Diagnostics.Process.Start($"https://www.sis001.com/forum/viewthread.php?tid={sp[1]}");
            }
        }

        private void 显示网页信息ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                var sp = listView1.SelectedItems[0].Text.Split('-');
                if (sp[2] == "T66y")
                {
                    var db = new LiteDatabase(File.Open(@"Y:\publish\T66y.db", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                    var FI = int.Parse(sp[1]);

                    var item = db.GetCollection<T66yData>("T66yData").FindOne(x => x.id == FI);

                    if (item != null)
                    {
                        var f = new Form2(item.MainList, this, int.Parse(listView1.SelectedItems[0].Text.Split('-')[1]));
                        f.Show();
                    }

                    db.Dispose();
                }
                else
                {
                    var db = new LiteDatabase(File.Open(@"Y:\publish\SIS.db", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

                    var SISDB = db.GetCollection<SISData>("SISData");
                    var FI = int.Parse(sp[1]);
                    var Fitem = SISDB.FindOne(x => x.id == FI);
                    if (Fitem != null)
                    {
                        var f = new Form2(Fitem.MainList, this, int.Parse(listView1.SelectedItems[0].Text.Split('-')[1]));
                        f.Show();
                    }
                    db.Dispose();
                }
            }
        }

        private void 显示同一页信息ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                var sp = listView1.SelectedItems[0].Text.Split('-');
                var f3 = new Form3(this, int.Parse(sp[1]));
                f3.Show();
            }
        }

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

        internal class SISData : T66yData
        {
            public string Type { get; set; }
        }
    }
}