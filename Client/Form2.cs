using LiteDB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FileMode = System.IO.FileMode;

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

        private List<MiMiAiData.BasicData> basicDatas = new List<MiMiAiData.BasicData>();

        //private List<MiMiAiData> Data = new List<MiMiAiData>();
        private static Image resizeImage(Image imgToResize, Size size)
        {
            //Get the image current width
            int sourceWidth = imgToResize.Width;
            //Get the image current height
            int sourceHeight = imgToResize.Height;
            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;
            //Calulate  width with new desired size
            nPercentW = ((float)size.Width / (float)sourceWidth);
            //Calculate height with new desired size
            nPercentH = ((float)size.Height / (float)sourceHeight);
            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;
            //New Width
            int destWidth = (int)(sourceWidth * nPercent);
            //New Height
            int destHeight = (int)(sourceHeight * nPercent);
            Bitmap b = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage((System.Drawing.Image)b);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            // Draw image with new width and height
            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();
            return (Image)b;
        }

        public Form2(string v)
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
            //using (var db = new LiteDatabase(File.Open(@"Y:\publish\MiMi.db", FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            var db = new LiteDatabase(File.Open(@"Y:\publish\MiMi.db", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            Task.Factory.StartNew(() =>
        {
            {
                var MiMiDB = db.GetCollection<MiMiAiData>("MiMiDB");
                foreach (var item in MiMiDB.Find(x => x.Date == v))
                {
                    var LastNewLine = false;

                    //Data.Add(item);
                    foreach (var quote in item.InfoList)
                    {
                        switch (quote.Type)
                        {
                            case "text":
                                {
                                    Invoke(new Action(() =>
                                    {
                                        if (LastNewLine)
                                        {
                                            richTextBox1.AppendText($"{string.Empty}{Environment.NewLine}");
                                            LastNewLine = false;
                                        }
                                        richTextBox1.AppendText($"{quote.info}{Environment.NewLine}");
                                    }));
                                }
                                break;

                            case "img":
                                {
                                    try
                                    {
                                        if (quote.Data != null)
                                        {
                                            MemoryStream img = new MemoryStream(quote.Data);
                                            var TempIm = Image.FromStream(img);
                                            // var TempImg = resizeImage(TempIm, new Size(256, 256));
                                            Invoke(new Action(() =>
                                        {
                                            try
                                            {
                                                Clipboard.SetDataObject(TempIm);
                                                richTextBox1.Paste(DataFormats.GetFormat(DataFormats.Bitmap));
                                                // TempImg.Dispose();
                                                TempIm.Dispose();
                                                img.Dispose();
                                            }
                                            catch (Exception)
                                            {
                                            }
                                        }));

                                            LastNewLine = true;
                                        }
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                                break;

                            case "torrent":
                                {
                                    basicDatas.Add(quote);
                                    Invoke(new Action(() =>
                                    {
                                        if (LastNewLine)
                                        {
                                            richTextBox1.AppendText($"{string.Empty}{Environment.NewLine}");
                                            LastNewLine = false;
                                        }
                                        richTextBox1.AppendText($"{quote.info}{Environment.NewLine}");
                                    }));
                                }
                                break;

                            default:
                                break;
                        }
                    }
                }
                Invoke(new Action(() =>
                {
                    richTextBox1.Select(0, 1); richTextBox1.DeselectAll();
                }));
                db.Dispose();
            }
        });
            this.FormClosing += delegate
            {
                try
                {
                    richTextBox1.Clear();
                    richTextBox1.Dispose();
                    db.Dispose();
                }
                catch (Exception)
                {
                }
                GC.Collect();
            };
        }

        private void 复制选中ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Copy();
        }

        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            switch (MessageBox.Show("是否打开，或者另存为", "打开方式", MessageBoxButtons.YesNoCancel))
            {
                case DialogResult.Yes:
                    {
                        System.Diagnostics.Process.Start($"{e.LinkText}");
                    }
                    break;

                case DialogResult.No:
                    {
                        var Find = basicDatas.FirstOrDefault(x => x.info == e.LinkText);
                        if (Find != null)
                            if (Find.Data != null)
                            {
                                File.WriteAllBytes($"{ Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}{Path.DirectorySeparatorChar}Temp.torrent", Find.Data);
                                switch (MessageBox.Show("打开Torrent？", "打开方式", MessageBoxButtons.YesNo))
                                {
                                    case DialogResult.Yes:
                                        {
                                            System.Diagnostics.Process.Start($"{ Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}{Path.DirectorySeparatorChar}Temp.torrent");
                                        }
                                        break;

                                    default:
                                        break;
                                }
                            }
                    }
                    break;

                default:
                    break;
            }
        }

        //private void pictureBox1_Paint(object sender, PaintEventArgs e)
        //{
        //    var OBJ = sender as PictureBox;
        //    var Y = 20;
        //    var SP = 15;
        //    var g = e.Graphics;
        //    Font f = new Font(new FontFamily("宋体"), 10f);
        //    foreach (var item in Data)
        //    {
        //        g.DrawString(item.Title, f, Pens.Black.Brush, 10, Y);
        //        Y += SP;
        //        foreach (var item2 in item.InfoList)
        //        {
        //            switch (item2.Type)
        //            {
        //                case "text":
        //                    {
        //                        g.DrawString(item2.info, f, Pens.Black.Brush, 10, Y);
        //                        Y += SP;
        //                    }
        //                    break;

        //                case "img":
        //                    {
        //                        if (item2.Data != null)
        //                        {
        //                            var image = Image.FromStream(new MemoryStream(item2.Data));
        //                            g.DrawImage(image, 10, Y);
        //                            Y += image.Height + 10;
        //                        }
        //                    }
        //                    break;

        //                default:
        //                    break;
        //            }
        //        }
        //    }
        //    OBJ.Size = new Size(1024, Y);
        //}
    }
}