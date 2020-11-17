using Client.Properties;
using Cowboy.WebSockets;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SevenZip;
using System.Drawing.Imaging;

namespace Client
{
    [Serializable]
    internal class T66yImgData
    {
        public string id { get; set; }
        public string Date { get; set; }
        public byte[] img { get; set; }
        public List<int> FromList { get; set; }
        public bool Status => img != null;

        internal byte[] ToByte()
        {
            using var stream = new MemoryStream();
            IFormatter Fileformatter = new BinaryFormatter();
            Fileformatter.Serialize(stream, this);
            return stream.ToArray();
        }

        public static T66yImgData ToClass(byte[] data)
        {
            using var stream = new MemoryStream(data);
            IFormatter Fileformatter = new BinaryFormatter();
            Fileformatter.Binder = new UBinder();
            return Fileformatter.Deserialize(stream) as T66yImgData;
        }

        public class UBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                if (typeName.EndsWith("T66yImgData"))
                {
                    return typeof(T66yImgData);
                }
                return (Assembly.GetExecutingAssembly()).GetType(typeName);
            }
        }
    }

    public partial class Form2 : Form
    {
        public MemoryStream Compress(Image srcBitmap, long level)
        {
            var destStream = new MemoryStream();
            ImageCodecInfo myImageCodecInfo;
            System.Drawing.Imaging.Encoder myEncoder;
            EncoderParameter myEncoderParameter;
            EncoderParameters myEncoderParameters;

            // Get an ImageCodecInfo object that represents the JPEG codec.
            myImageCodecInfo = GetEncoderInfo("image/jpeg");

            // Create an Encoder object based on the GUID

            // for the Quality parameter category.
            myEncoder = System.Drawing.Imaging.Encoder.Quality;

            // Create an EncoderParameters object. An EncoderParameters object has an array of
            // EncoderParameter objects. In this case, there is only one

            // EncoderParameter object in the array.
            myEncoderParameters = new EncoderParameters(1);

            // Save the bitmap as a JPEG file with 给定的 quality level
            myEncoderParameter = new EncoderParameter(myEncoder, level);
            myEncoderParameters.Param[0] = myEncoderParameter;
            srcBitmap.Save(destStream, myImageCodecInfo, myEncoderParameters);
            return destStream;
            ImageCodecInfo GetEncoderInfo(String mimeType)
            {
                int j;
                ImageCodecInfo[] encoders;
                encoders = ImageCodecInfo.GetImageEncoders();
                for (j = 0; j < encoders.Length; ++j)
                {
                    if (encoders[j].MimeType == mimeType)
                        return encoders[j];
                }
                return null;
            }
        }

        public Form2()
        {
            InitializeComponent();
            var Count = 0;
            int NSize = 0;
            int OSize = 0;
            int Size = 0;
            int Size2 = 0;
            int Size3 = 0;

            if (Class1.MainForm.Connect)
            {
                Task.Factory.StartNew(async () =>
                {
                    var uri = new Uri($"ws://{Settings.Default.ip}:{Settings.Default.point}/Data");
                    var client = new AsyncWebSocketClient(uri, null, (c, b, f, e) =>
                      {
                          this.Invoke(new MethodInvoker(() =>
                          {
                              var Img = T66yImgData.ToClass(b);

                              using (var SaveF = new MemoryStream())
                              {
                                  var compressor = new SevenZipCompressor
                                  {
                                      ArchiveFormat = OutArchiveFormat.SevenZip,
                                      CompressionLevel = CompressionLevel.Ultra,
                                  };
                                  using var SaveFD = new MemoryStream();

                                  OSize += Img.img.Length;
                                  var TempImg = Compress(Bitmap.FromStream(new MemoryStream(Img.img)), 75);
                                  Size3 += TempImg.ToArray().Length;
                                  compressor.CompressStream(TempImg, SaveF);
                                  compressor.CompressStream(new MemoryStream(Img.img), SaveFD);
                                  Size += SaveFD.ToArray().Length;
                                  var F = SaveF.ToArray();
                                  NSize += F.Length;
                                  var Extractor = new SevenZipExtractor(SaveF);
                                  using var SaveD = new MemoryStream();
                                  Extractor.ExtractFile(0, SaveD);
                                  var D = SaveD.ToArray();
                                  Size2 += D.Length;
                                  imageList1.Images.Add(Image.FromStream(new MemoryStream(D)));
                              }
                              // imageList1.Images.Add(Image.FromStream(new MemoryStream(Img.img)));
                              listView1.Items.Add(Count.ToString());
                              listView1.Items[Count].ImageIndex = Count;
                              Count += 1;
                              Console.WriteLine($"{OSize}||||||{NSize}|||||{Size}||||||{Size2}|||||||||{Size3}");
                              /*if (Count == 100)
                              {
                                  c.Shutdown();
                              }*/
                          }));
                          return Task.CompletedTask;
                      }
                    , null, null, null);
                    await client.Connect();
                    await client.SendTextAsync($"GetT66y|GetImgFromDate|2020-09-01");
                });
            }
            else
            {
                this.Close();
            }
        }
    }
}