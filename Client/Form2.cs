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
    public partial class Form2 : Form
    {
        internal class MiMiAiData
        {
            public int id { get; set; }
            public string Title { get; set; }
            public string Date { get; set; }
            public int Index { get; set; }
            public List<BasicData> InfoList { get; set; }
            public bool Status { get; set; }

            internal class BasicData
            {
                public string Type { get; set; }
                public string info { get; set; }
                public byte[] Data { get; set; }
            }
        }

        private List<MiMiAiData> Data = new List<MiMiAiData>();

        public Form2(string v)
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            using (var db = new LiteDatabase(@"Z:\publish\MiMi.db"))
            {
                var MiMiDB = db.GetCollection<MiMiAiData>("MiMiDB");
                foreach (var item in MiMiDB.FindAll())
                {
                    Data.Add(item);
                    if (Data.Count == 100) break;
                }
                // pictureBox1.Refresh();
            }
            var Create = new PictureBox();
            Create.Paint += pictureBox1_Paint;
            Create.Refresh();

            panel1.Controls.Add(Create);
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            var OBJ = sender as PictureBox;
            var Y = 20;
            var SP = 15;
            var g = e.Graphics;
            Font f = new Font(new FontFamily("宋体"), 10f);
            foreach (var item in Data)
            {
                g.DrawString(item.Title, f, Pens.Black.Brush, 10, Y);
                Y += SP;
                foreach (var item2 in item.InfoList)
                {
                    switch (item2.Type)
                    {
                        case "text":
                            {
                                g.DrawString(item2.info, f, Pens.Black.Brush, 10, Y);
                                Y += SP;
                            }
                            break;

                        case "img":
                            {
                                if (item2.Data != null)
                                {
                                    var image = Image.FromStream(new MemoryStream(item2.Data));
                                    g.DrawImage(image, 10, Y);
                                    Y += image.Height + 10;
                                }
                            }
                            break;

                        default:
                            break;
                    }
                }
            }
            OBJ.Size = new Size(1024, Y);
        }
    }
}