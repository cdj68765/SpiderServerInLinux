using LiteDB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class Form4 : Form
    {
        private int Count = 0;

        public Form4(string v)
        {
            InitializeComponent();

            Task.Factory.StartNew(() =>
            {
                // using (var stream = new FileStream(@"Z:\publish\SIS.db", System.IO.FileMode.Open,
                // FileAccess.Read, FileShare.ReadWrite))
                {
                    // using (var db = new LiteDatabase(stream)) using (var db = new LiteDatabase(@"Z:\publish\SIS.db"))
                    {
                        using var db = new LiteDatabase(@"Filename=Z:\publish\SIS.db;Connection=Shared;ReadOnly=True");

                        var SISDB = db.GetCollection<SISImgData>("ImgData");
                        var Findd = SISDB.Find(x => x.Date == v);
                        foreach (var Img in Findd)
                        {
                            this.Invoke(new MethodInvoker(() =>
                            {
                                if (Img.Status)
                                {
                                    try
                                    {
                                        imageList1.Images.Add(Image.FromStream(new MemoryStream(Img.img)));
                                        listView1.Items.Add(Img.FromList.First().ToString());
                                        listView1.Items[Count].ImageIndex = Count;
                                        Count += 1;
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine();
                                    }
                                }
                            }));
                        }
                    }
                }
            });
        }

        [Serializable]
        internal class T66yImgData
        {
            public string id { get; set; }
            public string Date { get; set; }
            public string Hash { get; set; }

            public byte[] img { get; set; }
            public List<int> FromList { get; set; }
            public bool Status => img != null;
        }

        [Serializable]
        internal class SISImgData : T66yImgData
        {
            new public byte[] img
            {
                get { return base.img; }
                set
                {
                    base.img = value;
                }
            }
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            System.Diagnostics.Process.Start($"https://www.sis001.com/forum/viewthread.php?tid={listView1.SelectedItems[0].Text}");
        }
    }
}