using LiteDB;
using SevenZip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace DBTest
{
    internal class Program
    {
        static void Main(string[] args)
        {

            var die=new DirectoryInfo(".//");
           
            foreach (var item in die.EnumerateFiles("*.gif"))
            {
                var ms = new MemoryStream(File.ReadAllBytes(item.FullName));

                Image img = Image.FromStream(ms);
                if (img.RawFormat.Equals(ImageFormat.Gif))
                {

                    Process p = new Process();
                    p.StartInfo.FileName = "gif2webp.exe";
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.Arguments = $"{item.FullName} -o {Path.GetFileNameWithoutExtension(item.FullName)}.webp -mixed -min_size -mt -m 6 -v";
                    p.Start();
                    p.WaitForExit();
                 


                }
            }
           var db = new LiteDatabase(@"Filename=I:\SIS.db;Connection=Shared;ReadOnly=True");

           var SISDB = db.GetCollection<SISImgData>("ImgData");

            long Long1 = 0;
            long Long2 = 0;
            long Long3 = 0;
            double Long4 = 0;
            long Long5 = 0;
            long Long6 = 0; 
            long Long7 = 0;

            foreach (var item in SISDB.FindAll())
            {
                if (item.img != null)
                    if (item.img.Length > 1024)
                    {
                         var ms = new MemoryStream(item.img);

                        Image img = Image.FromStream(ms);
                        if (img.RawFormat.Equals(ImageFormat.Gif))
                        {
                            Long3 += 1;
                               Long1 += item.img.Length;
                            File.WriteAllBytes("temp.gif", item.img);
                            //Stopwatch sw = Stopwatch.StartNew();
                            //Process p=new Process();
                            //p.StartInfo.FileName = "gif2webp.exe";
                            //p.StartInfo.CreateNoWindow = true;
                            //p.StartInfo.UseShellExecute = false;
                            //p.StartInfo.RedirectStandardOutput = true;
                            //p.StartInfo.Arguments = $"temp.gif -o {Long3}.webp -mixed -min_size -mt -m 6 -v";
                            //p.Start();
                            //p.WaitForExit();
                            //var rr = File.ReadAllBytes($"{Long3}.webp");
                            //Long2 += rr.Length;
                            //Console.Clear();
                            //Console.WriteLine($"原始            {HumanReadableFilesize(Long1)}");
                            //Console.WriteLine($"换成WEBP        {HumanReadableFilesize(Long2)}");
                            //Console.WriteLine($"等待             {sw.Elapsed.TotalSeconds}s");
                            //Long4 += sw.Elapsed.TotalSeconds;

                            Console.WriteLine($"等待             {Long4}s");

                        }
                        //     Long1 += item.img.Length;
                        //    StringBuilder stringBuilder = new StringBuilder();
                        // foreach (var X2 in item.img)
                        // {
                        //     stringBuilder.Append(X2.ToString("X2"));
                        // }
                        // var SB = Encoding.UTF8.GetBytes(stringBuilder.ToString());

                        // Long2 += rr.Length;
                        // var GzipSB = Compress(SB);
                        // Long3 += GzipSB.Length;
                        // var ZSB = CompressDeflater(SB);
                        // Long4 += ZSB.Length;
                        // var Base64 = Encoding.ASCII.GetBytes(Convert.ToBase64String(item.img));
                        // Long5 += Base64.Length;
                        // var GzipBase = Compress(Base64);
                        // Long6 += GzipBase.Length;
                        // var ZBase = CompressDeflater(Base64);
                        //Long7 += ZBase.Length;
                        // Console.Clear();
                        // Console.WriteLine($"原始            {HumanReadableFilesize(Long1)}");
                        // Console.WriteLine($"换成十六进制    {HumanReadableFilesize(Long2)}");
                        // Console.WriteLine($"十六进制GZIP压缩{HumanReadableFilesize(Long3)}");
                        // Console.WriteLine($"十六进制7z压缩  {HumanReadableFilesize(Long4)}");
                        // Console.WriteLine($"换成BASE64      {HumanReadableFilesize(Long5)}");
                        // Console.WriteLine($"换成BASE64 GZIP {HumanReadableFilesize(Long6)}");
                        //Console.WriteLine($"换成BASE64 7z   {HumanReadableFilesize(Long5)}");

                    }
            }
             byte[] CompressDeflater(byte[] pBytes)
            {
                MemoryStream mMemory = new MemoryStream();
                Deflater mDeflater = new Deflater(Deflater.BEST_COMPRESSION);
                using (DeflaterOutputStream mStream = new DeflaterOutputStream(mMemory, mDeflater, 131072))
                {
                    mStream.Write(pBytes, 0, pBytes.Length);
                }

                return mMemory.ToArray();
            }
            MemoryStream CompressZip(MemoryStream input)
            {
                MemoryStream output=new MemoryStream();
                var zip = new SevenZipCompressor();
                zip.CompressionLevel = CompressionLevel.High;
                zip.CompressStream(input, output);
                return output;
            }
            byte[] Compress(byte[] rawData)
            {
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                System.IO.Compression.GZipStream compressedzipStream = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Compress, true);
                compressedzipStream.Write(rawData, 0, rawData.Length);
                compressedzipStream.Close();
                return ms.ToArray();
            }
            string HumanReadableFilesize(double size)
            {
                var units = new[] { "B", "KB", "MB", "GB", "TB", "PB" };
                double mod = 1024.0;
                var DoubleCount = new List<double>();
                while (size >= mod)
                {
                    size /= mod;
                    DoubleCount.Add(size);
                }
                var Ret = "";
                for (int j = DoubleCount.Count; j > 0; j--)
                {
                    if (j == DoubleCount.Count)
                    {
                        Ret += $"{Math.Floor(DoubleCount[j - 1])}{units[j]}";
                    }
                    else
                    {
                        Ret += $"{Math.Floor(DoubleCount[j - 1] - (Math.Floor(DoubleCount[j]) * 1024))}{units[j]}";
                    }
                }
                return Ret;
            }
        }
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
}
