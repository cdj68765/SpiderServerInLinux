using DataBaseRead.Properties;
using LiteDB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MetroFramework;
namespace DataBaseRead
{
    public partial class Form1 : MetroFramework.Forms.MetroForm
    {
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

        public Form1()
        {
            InitializeComponent();
            label1.Text = Settings.Default.Address;
            if (!string.IsNullOrEmpty(Settings.Default.Date))
                dateTimePicker1.Value = DateTime.Parse(Settings.Default.Date);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var O = new OpenFileDialog();
            O.ShowDialog();
            Settings.Default.Address = O.FileName;
            Settings.Default.Save();
            label1.Text = Settings.Default.Address;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var D = dateTimePicker1.Value.ToString("yy-MM-dd");
            bool CompareChar(string c)
            {
                if (char.Parse(c) >= 'A' && char.Parse(c) <= 'Z')
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            using (var db = new LiteDatabase(label1.Text))
            {
                var JavDB = db.GetCollection<JavInfo>("JavDB");
                userListBox1.Items.Clear();
                foreach (var item in JavDB.Find(x => x.Date == D))
                {
                    userListBox1.Items.Add(new ListBoxItem() { Id = new Guid(), Image = Image.FromStream(new System.IO.MemoryStream(item.Image)), Name = item.id });
                    if (item.Actress.Length == 1 && item.Actress[0] != null)
                    {
                        var SaveS = new List<string>();
                        var TempS = new StringBuilder();
                        foreach (var item1 in item.Actress[0].Replace(" ", "").Select(x => x.ToString()))
                        {
                            if (CompareChar(item1))
                            {
                                if (TempS.Length != 0)
                                    SaveS.Add(TempS.ToString());
                                TempS.Clear();
                                TempS.Append(item1);
                            }
                            else
                            {
                                TempS.Append(item1);
                            }
                        }
                        SaveS.Add(TempS.ToString());
                    }
                }
            }
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            Settings.Default.Date = dateTimePicker1.Value.ToString("yy-MM-dd");
            Settings.Default.Save();
        }
    }
}