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
    public partial class Form5 : Form
    {
        private int Count = 0;
        private List<JavInfo> TempInfo = new List<JavInfo>();

        public Form5(string v)
        {
            InitializeComponent();
            Task.Factory.StartNew(() =>
            {
                // using (var stream = new FileStream(@"Z:\publish\SIS.db", System.IO.FileMode.Open,
                // FileAccess.Read, FileShare.ReadWrite))
                {
                    // using (var db = new LiteDatabase(stream)) using (var db = new LiteDatabase(@"Z:\publish\SIS.db"))
                    {
                        using var db = new LiteDatabase(@"Filename=Z:\publish\Jav.db;Connection=Shared;ReadOnly=True");

                        var SISDB = db.GetCollection<JavInfo>("JavDB");
                        var Findd = SISDB.Find(x => x.Date == v);
                        foreach (var Img in Findd)
                        {
                            this.Invoke(new MethodInvoker(() =>
                            {
                                try
                                {
                                    imageList1.Images.Add(Image.FromStream(new MemoryStream(Img.Image)));
                                    listView1.Items.Add(Img.id.ToString());
                                    TempInfo.Add(Img);
                                    listView1.Items[Count].ImageIndex = Count;
                                    Count += 1;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine();
                                }
                            }));
                        }
                    }
                }
            });
        }

        internal class JavInfo
        {
            public string id { get; set; }
            public string Magnet { get; set; }
            public string ImgUrl { get; set; }
            public string ImgUrlError { get; set; }
            public byte[] Image { get; set; }
            public string Describe { get; set; }
            public string Size { get; set; }
            public string Date { get; set; }
            public string[] Tags { get; set; }
            public string[] Actress { get; set; }
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                System.Diagnostics.Process.Start($"https://www.141jav.com/torrent/{listView1.SelectedItems[0].Text}");
            else if (e.Button == MouseButtons.Right)
                Clipboard.SetDataObject(TempInfo.FirstOrDefault(x => x.id == listView1.SelectedItems[0].Text).Magnet);
        }
    }
}