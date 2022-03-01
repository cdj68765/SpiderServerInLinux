using BotBiliBili.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static WebpRead.Form1;
using WebPWrapper;

namespace WebpRead
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
        }

        public Form3(Form1 form1, int v)
        {
            InitializeComponent();
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
                                Invoke(new Action(() =>
                                {
                                    imageList1.Images.Add(Img);
                                    listView1.Items.Add($"{Count}-{item.FromList.First()}-{item.From}");
                                    listView1.Items[Count].ImageIndex = Count;
                                }));
                                Count += 1;
                            }
                            else
                            {
                                var Img = new WebP().GetThumbnailFast(item.img, 256, 256);
                                Invoke(new Action(() =>
                                {
                                    imageList1.Images.Add(Img);
                                    listView1.Items.Add($"{Count}-{item.FromList.First()}-{item.From}");
                                    listView1.Items[Count].ImageIndex = Count;
                                }));
                                Count += 1;
                            }
                        }
                        else if (item.Type == "gif")
                        {
                            if (form1.GetFileImageTypeFromHeader(item.img) == ImageType.GIF)
                            {
                                var Img = Image.FromStream(new MemoryStream(item.img));
                                Invoke(new Action(() =>
                                {
                                    imageList1.Images.Add(Img);
                                    listView1.Items.Add($"{Count}-{item.FromList.First()}-{item.From}");
                                    listView1.Items[Count].ImageIndex = Count;
                                }));
                                Count += 1;
                            }
                            else
                            {
                                var Img = new SimpleAnimDecoder().DecodeFromBytes(item.img).Frames.FirstOrDefault().Image;
                                Invoke(new Action(() =>
                                {
                                    imageList1.Images.Add(Img);
                                    listView1.Items.Add($"{Count}-{item.FromList.First()}-{item.From}");
                                    listView1.Items[Count].ImageIndex = Count;
                                }));
                                Count += 1;
                            }
                        }
                        else
                        {
                            var TT = form1.GetFileImageTypeFromHeader(item.img);
                            if (TT == ImageType.GIF)
                            {
                                var Img = Image.FromStream(new MemoryStream(item.img));
                                Invoke(new Action(() =>
                                {
                                    imageList1.Images.Add(Img);
                                    listView1.Items.Add($"{Count}-{item.FromList.First()}-{item.From}");
                                    listView1.Items[Count].ImageIndex = Count;
                                }));
                                Count += 1;
                            }
                            else if (TT == ImageType.JPEG)
                            {
                                var Img = Image.FromStream(new MemoryStream(item.img));
                                Invoke(new Action(() =>
                                {
                                    imageList1.Images.Add(Img);
                                    listView1.Items.Add($"{Count}-{item.FromList.First()}-{item.From}");
                                    listView1.Items[Count].ImageIndex = Count;
                                }));
                                Count += 1;
                            }
                            else if (TT == ImageType.Unknown)
                            {
                                var Img = new SimpleAnimDecoder().DecodeFromBytes(item.img).Frames.FirstOrDefault().Image;
                                Invoke(new Action(() =>
                                {
                                    imageList1.Images.Add(Img);
                                    listView1.Items.Add($"{Count}-{item.FromList.First()}-{item.From}");
                                    listView1.Items[Count].ImageIndex = Count;
                                }));
                                Count += 1;
                            }
                            else
                            {
                                var Img = Image.FromStream(new MemoryStream(item.img));
                                Invoke(new Action(() =>
                                {
                                    imageList1.Images.Add(Img);
                                    listView1.Items.Add($"{Count}-{item.FromList.First()}-{item.From}");
                                    listView1.Items[Count].ImageIndex = Count;
                                }));
                                Count += 1;
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            });
        }
    }
}