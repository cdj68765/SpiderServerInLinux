using LiteDB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class Form6 : Form
    {
        public Form6()
        {
            InitializeComponent();

            this.FormClosing += delegate
            {
                if (T != null)
                {
                    var Status = T.Status;
                    if (T.Status == TaskStatus.Running)
                    {
                        SISDownloadCancel.Cancel();

                        while (T.Status == TaskStatus.Running)
                        {
                            Thread.Sleep(1000);
                        }
                        T.Dispose();
                        SISDownloadCancel = new CancellationTokenSource();
                    }
                }
            };
        }

        internal class MiMiAiStory
        {
            public int id { get; set; }
            public string Uri { get; set; }
            public string Title { get; set; }
            public string Story { get; set; }
            public byte[] Data { get; set; }
        }

        private List<MiMiAiStory> FindList = new List<MiMiAiStory>();
        private Task T;
        private CancellationTokenSource SISDownloadCancel = new CancellationTokenSource();

        private void button1_Click(object sender, EventArgs e)
        {
            if (T != null)
            {
                var Status = T.Status;
                if (T.Status == TaskStatus.Running)
                {
                    SISDownloadCancel.Cancel();

                    while (T.Status == TaskStatus.Running)
                    {
                        Thread.Sleep(1000);
                    }
                    T.Dispose();
                    SISDownloadCancel = new CancellationTokenSource();
                }
            }
            T = Task.Factory.StartNew(() =>
           {
               using var db = new LiteDatabase(@"Filename=Z:\publish\MiMi.db;Connection=Shared;ReadOnly=True");

               var SISDB = db.GetCollection<MiMiAiStory>("MiMiStory");
               if (!string.IsNullOrEmpty(textBox1.Text))
               {
                   FindList = new List<MiMiAiStory>();
                   this.Invoke(new MethodInvoker(() =>
                   {
                       listBox1.Items.Clear();
                   }));
                   foreach (var item in SISDB.Find(x => x.Story.Contains(textBox1.Text)))
                   {
                       if (SISDownloadCancel.IsCancellationRequested)
                       {
                           break;
                       }
                       this.Invoke(new MethodInvoker(() =>
                       {
                           FindList.Add(item);
                           listBox1.Items.Add($"{item.Title}");
                       }));
                   }
               }
           }, SISDownloadCancel.Token);
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            richTextBox1.Text = FindList[listBox1.SelectedIndex].Story;
            if (!string.IsNullOrEmpty(textBox1.Text))
            {
                string find = textBox1.Text;//find为想要查找的字符串

                int index = richTextBox1.Find(find, RichTextBoxFinds.None);//调用find方法，并设置区分全字匹配
                if (index != -1)
                {
                    int startPos = index;
                    int nextIndex = 0;
                    while (nextIndex != startPos)//循环查找字符串，并用蓝色加粗12号Times New Roman标记之
                    {
                        richTextBox1.SelectionStart = index;
                        richTextBox1.SelectionLength = find.Length;
                        richTextBox1.SelectionColor = Color.Blue;
                        richTextBox1.Focus();
                        nextIndex = richTextBox1.Find(find, index + find.Length, RichTextBoxFinds.None);
                        if (nextIndex == -1)//若查到文件末尾，则充值nextIndex为初始位置的值，使其达到初始位置，顺利结束循环，否则会有异常。
                            nextIndex = startPos;
                        index = nextIndex;
                    }
                }
            }
        }
    }
}