using BotBiliBili.Utils;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static WebpRead.Form1;
using WebPWrapper;
using System.Drawing.Drawing2D;
using System.Collections;

namespace WebpRead
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        public Form2(List<string> quoteList, Form1 form1, int v)
        {
            InitializeComponent();
            var imageList1 = new List<Tuple<string, string, Image>>();
            Task.Factory.StartNew(() =>
            {
                var Count = 0;
                foreach (var item in from T1 in form1.webpImages from T2 in T1.FromList where T2 == v select T1)
                {
                    try
                    {
                        if (item.Type == "jpg")
                        {
                            if (form1.GetFileImageTypeFromHeader(item.img) == ImageType.JPEG)
                            {
                                var Img = Image.FromStream(new MemoryStream(item.img));
                                imageList1.Add(new Tuple<string, string, Image>(item.id, item.Hash, Img));
                                Count += 1;
                            }
                            else
                            {
                                var Img = new WebP().GetThumbnailFast(item.img, 256, 256);
                                imageList1.Add(new Tuple<string, string, Image>(item.id, item.Hash, Img));
                                Count += 1;
                            }
                        }
                        else if (item.Type == "gif")
                        {
                            if (form1.GetFileImageTypeFromHeader(item.img) == ImageType.GIF)
                            {
                                var Img = Image.FromStream(new MemoryStream(item.img));
                                imageList1.Add(new Tuple<string, string, Image>(item.id, item.Hash, Img));
                                Count += 1;
                            }
                            else
                            {
                                var Img = new SimpleAnimDecoder().DecodeFromBytes(item.img).Frames.FirstOrDefault().Image;
                                imageList1.Add(new Tuple<string, string, Image>(item.id, item.Hash, Img));
                                Count += 1;
                            }
                        }
                        else
                        {
                            var TT = form1.GetFileImageTypeFromHeader(item.img);
                            if (TT == ImageType.GIF)
                            {
                                var Img = Image.FromStream(new MemoryStream(item.img));
                                imageList1.Add(new Tuple<string, string, Image>(item.id, item.Hash, Img));
                                Count += 1;
                            }
                            else if (TT == ImageType.JPEG)
                            {
                                var Img = Image.FromStream(new MemoryStream(item.img));
                                imageList1.Add(new Tuple<string, string, Image>(item.id, item.Hash, Img));
                                Count += 1;
                            }
                            else if (TT == ImageType.Unknown)
                            {
                                var Img = new SimpleAnimDecoder().DecodeFromBytes(item.img).Frames.FirstOrDefault().Image;
                                imageList1.Add(new Tuple<string, string, Image>(item.id, item.Hash, Img));
                                Count += 1;
                            }
                            else
                            {
                                var Img = Image.FromStream(new MemoryStream(item.img));
                                imageList1.Add(new Tuple<string, string, Image>(item.id, item.Hash, Img));
                                Count += 1;
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                if (quoteList != null)
                {
                    var sb = new StringBuilder();
                    var LastNewLine = false;
                    foreach (string quote in quoteList)
                    {
                        try
                        {
                            var Find = imageList1.FirstOrDefault(x => x.Item1 == quote);
                            if (Find != null)
                            {
                                var TempImg = resizeImage(Find.Item3, new Size(256, 256));
                                Invoke(new Action(() =>
                                {
                                    Clipboard.SetDataObject(TempImg, false, 10, 50);
                                    richTextBox1.Paste(DataFormats.GetFormat(DataFormats.Bitmap));
                                    //richTextBox1.AppendText(String.Empty);
                                }));
                                TempImg.Dispose();
                                LastNewLine = true;
                            }
                            else if (quote.StartsWith("SIS-"))
                            {
                                Find = imageList1.FirstOrDefault(x => $"SIS-{x.Item2}" == quote);
                                if (Find != null)
                                {
                                    var TempImg = resizeImage(Find.Item3, new Size(256, 256));
                                    Invoke(new Action(() =>
                                    {
                                        Clipboard.SetDataObject(TempImg, false, 10, 50);
                                        richTextBox1.Paste(DataFormats.GetFormat(DataFormats.Bitmap));
                                    }));
                                    TempImg.Dispose();
                                    LastNewLine = true;
                                }
                            }
                            else
                            {
                                Invoke(new Action(() =>
                                {
                                    if (LastNewLine)
                                    {
                                        richTextBox1.AppendText($"{string.Empty}{Environment.NewLine}");
                                        LastNewLine = false;
                                    }
                                    richTextBox1.AppendText($"{quote}{Environment.NewLine}");
                                }));
                            }
                        }
                        catch (Exception ex)
                        {
                            if (LastNewLine)
                            {
                                richTextBox1.AppendText($"{string.Empty}{Environment.NewLine}");
                                LastNewLine = false;
                            }
                            richTextBox1.AppendText($"{quote}   {ex.Message} {Environment.NewLine}");
                        }
                    }
                    Invoke(new Action(() =>
                    {
                        richTextBox1.Select(0, 1); richTextBox1.DeselectAll();
                    }));
                }
            });
        }

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

        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start($"{e.LinkText}");
        }

        private void 复制ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Copy();
        }
    }
}