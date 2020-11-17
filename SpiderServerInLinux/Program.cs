using Cowboy.WebSockets;
using HtmlAgilityPack;
using LiteDB;
using Shadowsocks.Controller;
using SocksSharp;
using SocksSharp.Proxy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using xNet;

// netsh winsock reset
namespace SpiderServerInLinux
{
    internal static class Program
    {
        private class Keyword
        {
            public int ID { get; set; }
            public string Title { get; set; }
            public string Link { get; set; }
        }

        private static async Task<int> Main(string[] args)
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // GetOtherT6yyPage();
            //var Download = DownloadGooglePage(4044141);//.Replace("&raquo;", "").Replace("&nbsp;", "").Replace("&copy;", "").Replace("/r", "").Replace("/t", "").Replace("/n", "").Replace("&amp;", "&"); ;
            //AnalyGooglePage(File.ReadAllText("4044141.html"));
            //var RetUrl = AnalyGooglePage(File.ReadAllText("4164642.html"));
            //var RetHtml = client.GetStringAsync($"{ AnalyGooglePage(DownloadGooglePage(PageCount))}").Result;
            //var _Table = db.GetCollection<T66yImgData>("ImgData");
            //var fo = _Table.FindOne(Query.EQ("id", "http://img200.imagexport.com/th/25143/1i1g8gafv3yr.jpg"));
            //var fo = _Table.Find(x => x["Date"] == "2020-09-03").Count();
            //var fo = _Table.Find(x => x.Date == "2020-09-03").Count();
            //var aa = new LiteDB.BsonMapper();
            // var FFF = aa.ToObject<T66yImgData>(fo);
            //Console.WriteLine();
            // HandleT66yPage();
            void HandleT66yPage(string Data)
            {
                var ImgList = new List<T66yImgData>();
                var TempList = new List<string>();
                var Quote = new List<string>();
                var _HtmlDoc = new HtmlDocument();
                //_HtmlDoc.LoadHtml(Data);
                // _HtmlDoc.Load(File.OpenRead($"{PageCount}.html"), Encoding.GetEncoding("gbk"));
                //_HtmlDoc.Save($"{PageCount}.html", Encoding.GetEncoding("gbk"));

                //_HtmlDoc.Load("4177015.html");
                _HtmlDoc.Load("4164637.html", Encoding.GetEncoding("gbk"));
                try
                {
                    //_HtmlDoc.Save($"Html{count++}.html", Encoding.GetEncoding("gbk"));
                    var ParentNode = _HtmlDoc.DocumentNode.SelectSingleNode("//div[@class='tiptop']").ParentNode;
                    var Name = ParentNode.SelectSingleNode("//h4");
                    var MainNode = ParentNode.SelectSingleNode("//div[4]");

                    var Type = ParentNode.SelectSingleNode("//*[@id='main']/div[1]/table[1]/tr[1]/td[1]/b[1]/a[2]");
                    var Time = ParentNode.SelectSingleNode("//*[@id='main']/div[3]/table[1]/tbody[1]/tr[2]/th[1]/div[1]");//.ChildNodes[2].InnerHtml.Replace("\r\n", "").Replace("Posted:", "");
                    if (Time == null)
                        Time = ParentNode.SelectSingleNode("//*[@id='main']/div[3]/table[1]/tr[2]/th[1]/div[1]");//.ChildNodes[2].InnerHtml.Replace("\r\n", "").Replace("Posted:", "");
                    var STime = Time.ChildNodes[2].InnerHtml.Replace("\r\n", "").Replace("Posted:", "");
                    // var Date = DateTime.Parse(Time);
                    /*foreach (var item in CN.SelectNodes("//*[@id='main']/div"))
                    {
                        Console.WriteLine();
                    }*/
                    var Start = false;
                    foreach (var item in MainNode.ChildNodes)
                    {
                        if (Start)
                        {
                            if (item.Name == "blockquote")//TempList.Last().Contains("引用") || TempList.Last().Contains("Quote"))
                            {
                                if (item.ChildNodes.Count != 0)
                                {
                                    SpildChild(item.ChildNodes, ref Quote);
                                }
                            }
                            else if (!string.IsNullOrWhiteSpace(item.InnerHtml)) // !item.InnerHtml.Contains("&nbsp"))
                            {
                                if (item.ChildNodes.Count != 0)
                                    SpildChild(item.ChildNodes, ref TempList);
                                else
                                    TempList.Add(item.InnerHtml);
                            }
                            else if (item.Name == "img")
                            {
                                var img = item.Attributes["ess-data"].Value.Replace(".th.jpg", ".jpg");
                                //SaveImg(img);
                                TempList.Add(img);
                            }
                        }
                        else if (FindFirst(item.InnerHtml, Name.InnerHtml))
                        {
                            Start = true;
                            if (item.ChildNodes.Count != 0)
                                SpildChild(item.ChildNodes, ref TempList);
                            else
                                TempList.Add(item.InnerHtml);
                        }
                    }
                    if (TempList.Count == 0)
                    {
                    }
                    TempList.RemoveAt(TempList.Count - 1);//删除 赞
                    for (int i = TempList.Count - 1; i > 0; i--)
                    {
                        if (TempList[i].ToLower().Contains("quote") || TempList[i].ToLower().Contains("nbsp"))
                            TempList.RemoveAt(i);
                    }
                    var findrmdown = false;
                    if (!findrmdown)
                    {
                        foreach (var item3 in TempList)
                        {
                            if (item3.Contains("rmdown"))
                            {
                                findrmdown = true;
                            }
                        }
                    }
                    if (!findrmdown)
                    {
                    }
                }
                catch (Exception ex)
                {
                }
                void SpildChild(HtmlNodeCollection item, ref List<string> sL)
                {
                    foreach (var item2 in item)
                    {
                        if (item2.ChildNodes.Count == 0)
                        {
                            if (!string.IsNullOrWhiteSpace(item2.InnerHtml) && !item2.InnerHtml.Contains("&nbsp"))
                                sL.Add(item2.InnerHtml);
                            if (item2.Name == "img")
                            {
                                var img = item2.Attributes["ess-data"].Value.Replace(".th.jpg", ".jpg");
                                sL.Add(img);
                                SaveImg(img);
                            }
                        }
                        else
                        {
                            SpildChild(item2.ChildNodes, ref sL);
                        }
                    }
                }
                bool FindFirst(string src, string Title)
                {
                    var SearchChar = new HashSet<string>() { "名称", "名稱", "rmdown", Title };
                    foreach (var item in SearchChar)
                    {
                        if (src.Contains(item))
                        {
                            return true;
                        }
                    }
                    return false;
                }
                void SaveImg(string img)
                {
                    T66yImgData SearchImg = DataBaseCommand.GetDataFromT66y("img", img);
                    if (SearchImg != null)
                    {
                        if (!SearchImg.Status)
                        {
                        }
                    }
                    else
                    {
                    }
                }
            }

            Setting._GlobalSet = GlobalSet.Open();
            if (args.Length != 0 && !string.IsNullOrEmpty(args[0]))
            {
                Setting._GlobalSet.ssr_url = args[0].ToString();
                Setting._GlobalSet.SocksCheck = true;
            }

            Setting._GlobalSet.MiMiFin = true;
            await Init();
            return await Setting.ShutdownResetEvent.Task.ConfigureAwait(false);

            await Task.Run(async () =>
             {
                 var config = new AsyncWebSocketClientConfiguration();
                 //var uri = new Uri("ws://163.43.82.141:31424/");
                 var uri = new Uri("ws://127.0.0.1:1200/Set");
                 var _client = new AsyncWebSocketClient(uri,
                                       (client, text) =>
                                       {
                                           return Task.CompletedTask;/*OnServerTextReceived*/
                                       },
                                      (client, data, offset, count) =>
                                      {
                                          return Task.CompletedTask; /*OnServerBinaryReceived*/
                                      },
                                        (client) =>
                                        {
                                            return Task.CompletedTask;/* OnServerConnected*/
                                        },
                                       (client) =>
                                       {
                                           return Task.CompletedTask; /*OnServerDisconnected*/
                                       },
                                        config);

                 await _client.Connect();
             });

            return await Setting.ShutdownResetEvent.Task.ConfigureAwait(false);
            /*  {
                  var HtmlDoc = new HtmlDocument();
                  HtmlDoc.Load(@"C:\Users\cdj68\Desktop\无标题.html");
                  try
                  {
                      foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/div[1]/div"))
                      {
                          var TempData = new JavInfo();
                          var temp = HtmlNode.CreateNode(item.OuterHtml);
                          TempData.ImgUrl = temp.SelectSingleNode("div/div/div[1]/img").Attributes["src"].Value;
                          TempData.ImgUrlError = temp.SelectSingleNode("div/div/div[1]/img").Attributes["onerror"].Value.Split('\'')[1];
                          //TempData.id = temp.SelectSingleNode("div/div/div[2]/div/h5/a").InnerText.Replace("\n", "");
                          TempData.id = temp.SelectSingleNode("div/div/div[2]/div/h5/a").Attributes["href"].Value.Replace(@"/torrent/", "");
                          TempData.Size = temp.SelectSingleNode("div/div/div[2]/div/h5/span").InnerText;
                          TempData.Date = $"{DateTime.Parse(temp.SelectSingleNode("div/div/div[2]/div/p[1]/a").Attributes["href"].Value.Substring(1)):yy-MM-dd}";
                          var tags = new List<string>();
                          try
                          {
                              foreach (var Tags in temp.SelectNodes(@"//div[@class='tags']/a"))
                              {
                                  tags.Add(Tags.InnerText.Replace("\n", "").Replace("\r", ""));
                              }
                          }
                          catch (Exception)
                          {
                          }
                          TempData.Tags = tags.ToArray();
                          TempData.Describe = temp.SelectSingleNode("div/div/div[2]/div/p[2]").InnerText.ReplaceEntities().Replace("\n", ""); ;
                          var Actress = new List<string>();
                          try
                          {
                              foreach (var Tags in temp.SelectNodes(@"//div[@class='panel']/a"))
                              //foreach (var Tags in temp.SelectNodes(@"div/div/div[2]/div/div[2]/a"))
                              {
                                  Actress.Add(Tags.InnerText.Replace("\n", ""));
                              }
                          }
                          catch (System.NullReferenceException e)
                          {
                          }
                          catch (Exception e)
                          {
                          }
                          TempData.Actress = Actress.ToArray();
                          TempData.Magnet = temp.SelectSingleNode("div/div/div[2]/div/a[1]").Attributes["href"].Value;
                      }
                  }
                  catch (Exception e)
                  {
                  }
              }*/
            // HtmlDoc.LoadHtml(Encoding.GetEncoding("GBK").GetString(File.ReadAllBytes("MiMiS")));
            /*try
            {
                var HtmlDoc = new HtmlDocument();
                HtmlDoc.Load("MiMiL");
                foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/center/form/div[1]/div/table"))
                {
                    var TempData = new string[]
                    {
                    item.SelectSingleNode("tr/td[1]/a").Attributes["href"].Value,
                    item.SelectSingleNode("tr/td[3]/a[1]").InnerText,
                    item.SelectSingleNode("tr/td[4]/a").InnerText,
                    item.SelectSingleNode("tr/td[4]/span").InnerText,
                    bool.FalseString
                    };
                    if (TempData[2] == "mimi")
                    {
                        if (TempData[1].Contains("BT合集"))
                        {
                            //DataBaseCommand.SaveToMiMiDataTablet(TempData);
                            DataBaseCommand.SaveToMiMiDataTablet(new[] { TempData[3], bool.TrueString });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }*/
            /* ProxySettings ProxySettings = new ProxySettings { Host = "127.0.0.1", Port = 7070 };
             var client = new HttpClient(new ProxyClientHandler<Socks5>(ProxySettings));

             client.Timeout = new TimeSpan(0, 1, 0);
             var HtmlDoc = new HtmlDocument();
             HtmlDoc.Load(new FileStream("Html", System.IO.FileMode.Open), Encoding.GetEncoding("GBK"));
             foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/div[2]/div[2]/table[1]/tbody[1]/tr"))
             {
                 if (string.IsNullOrEmpty(item.InnerHtml)) continue;
                 if (item.InnerLength < 50)
                     continue;
                 if (item.Attributes["class"].Value == "tr3 t_one tac" && item.SelectSingleNode("td[1]").InnerHtml.Contains(".::"))
                 {
                     var temp = HtmlNode.CreateNode(item.OuterHtml);
                     var Url = temp.SelectSingleNode("td[2]/h3/a").Attributes["href"].Value;
                     var HtmlTempData = Get(ref Url);
                     var TempData = new string[]
                     {
                         Url,//地址
                         temp.SelectSingleNode("td[2]/h3/a").InnerHtml,//标题
                         temp.SelectSingleNode("td[3]/div/span").Attributes["title"].Value.Split(' ')[2],//日期
                         HtmlTempData.Item1,//编号
                         HtmlTempData.Item2,//内容
                     };
                 }
             }
             Tuple<string, string> Get(ref string Url)
             {
                 if (Url.StartsWith("htm"))
                 {
                     var UrlS = Url.Replace("htm_data", "").Replace(".html", "").Split('/');
                     return new Tuple<string, string>($"{UrlS[1]}{UrlS[2]}{UrlS[3]}", client.GetStringAsync($"http://t66y.com/{Url}").Result);
                 }
                 else
                 {
                     // Url = $"htm_data/{year}/{Month}/{Url.Split('=')[1]}.html"; var RetHtml = client.GetStringAsync($"http://t66y.com/{Url}").Result;
                     var TempDoc = new HtmlDocument();
                     TempDoc.Load(client.GetStringAsync($"http://t66y.com/{Url}").Result);
                     Url = TempDoc.DocumentNode.SelectSingleNode("/html/body/center/div/a[2]").Attributes["href"].Value;
                     return Get(ref Url);
                     //File.WriteAllText("Html2", RetHtml);
                 }
             }*/
            //http://www.mmfhd.com/forumdisplay.php?fid=55&page=
            //http://www.mmbuff.com/forumdisplay.php?fid=55&page=
            /* using (var request = new HttpRequest()
              {
                  UserAgent = Http.ChromeUserAgent(),
                  ConnectTimeout = 20000,
                  CharacterSet = Encoding.GetEncoding("GBK")
              })
              {
                  //request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:7070");
                  HttpResponse response = request.Get("http://www.mmbuff.com/viewthread.php?tid=1203210");
                  var Save = response.ToString();
                  File.WriteAllText("MiMiC", Save);
              }
                              var HtmlDoc = new HtmlDocument();
                  HtmlDoc.LoadHtml(Encoding.GetEncoding("GBK").GetString(response.Ret));
                  foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/center/form/div[1]/div/table"))
                  {
                      var CT = item.SelectSingleNode("tr/td[1]/a").Attributes["href"].Value;
                      CT = item.SelectSingleNode("tr/td[3]/a[1]").InnerText;
                      CT = item.SelectSingleNode("tr/td[4]/a").InnerText;
                      CT = item.SelectSingleNode("tr/td[4]/span").InnerText;
                  }
               */

            Setting._GlobalSet = GlobalSet.Open();
            if (args.Length != 0 && !string.IsNullOrEmpty(args[0]))
            {
                Setting._GlobalSet.ssr_url = args[0].ToString();
                Setting._GlobalSet.SocksCheck = true;
            }
            Setting._GlobalSet.MiMiFin = true;
            await Init();
            return await Setting.ShutdownResetEvent.Task.ConfigureAwait(false);
        }

        private static void DebugJav()
        {
            var HtmlDoc = new HtmlDocument();

            HtmlDoc.LoadHtml(File.ReadAllText("Error.html"));
            try
            {
                foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/div[1]/div"))
                {
                    var TempData = new JavInfo();
                    var temp = HtmlNode.CreateNode(item.OuterHtml);
                    TempData.ImgUrl = temp.SelectSingleNode("div/div/div[1]/img").Attributes["src"].Value;
                    TempData.ImgUrlError = temp.SelectSingleNode("div/div/div[1]/img").Attributes["onerror"].Value.Split('\'')[1];
                    //TempData.id = temp.SelectSingleNode("div/div/div[2]/div/h5/a").InnerText.Replace("\n", "");
                    TempData.id = temp.SelectSingleNode("div/div/div[2]/div/h5/a").Attributes["href"].Value.Replace(@"/torrent/", "");
                    TempData.Size = temp.SelectSingleNode("div/div/div[2]/div/h5/span").InnerText;
                    TempData.Date = $"{DateTime.Parse(temp.SelectSingleNode("div/div/div[2]/div/p[1]/a").Attributes["href"].Value.Substring(1)):yy-MM-dd}";
                    var tags = new List<string>();
                    try
                    {
                        foreach (var Tags in temp.SelectNodes(@"//div[@class='tags']/a"))
                        {
                            tags.Add(Tags.InnerText.Replace("\n", "").Replace("\r", ""));
                        }
                    }
                    catch (NullReferenceException) { }
                    catch (Exception)
                    {
                    }
                    TempData.Tags = tags.ToArray();
                    TempData.Describe = temp.SelectSingleNode("div/div/div[2]/div/p[2]").InnerText.ReplaceEntities().Replace("\n", ""); ;
                    var Actress = new List<string>();
                    try
                    {
                        foreach (var Tags in temp.SelectNodes(@"//div[@class='panel']/a"))
                        //foreach (var Tags in temp.SelectNodes(@"div/div/div[2]/div/div[2]"))
                        {
                            Actress.Add(Tags.InnerText.Replace("\n", ""));
                        }
                    }
                    catch (NullReferenceException) { }
                    catch (Exception)
                    {
                        Loger.Instance.LocalInfo($"Jav人名解析失败");
                    }
                    TempData.Actress = Actress.ToArray();
                    TempData.Magnet = temp.SelectSingleNode("div/div/div[2]/div/a[1]").Attributes["href"].Value;
                }
            }
            catch (Exception)
            {
            }
        }

        private static async Task Init()
        {
            AppDomain.CurrentDomain.ProcessExit += delegate
            {
                Setting.DownloadManage?.Dispose();
                Console.Clear();
                Console.WriteLine("程序退出");
            };
            AppDomain.CurrentDomain.UnhandledException += delegate
            {
                Console.Clear();
                Console.WriteLine("程序异常");
            };
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Loger.Instance.ServerInfo("主机", "线程异常");
            };
            await InitCoreAsync().ConfigureAwait(false);
        }

        private static async Task InitCoreAsync()
        {
            await Task.WhenAll(
            Task.Run(() => DataBaseCommand.InitDataBase()),
            Task.Run(() =>
            {
                try
                {
                    Setting.SSR = new ShadowsocksController();
                    Setting.Socks5Point = Setting.SSR.SocksPort;
                    Setting.SSR.CheckOnline();
                    Setting.NyaaSSR = new ShadowsocksController(Setting._GlobalSet.ssr4Nyaa);
                    Setting.NyaaSocks5Point = Setting.NyaaSSR.SocksPort;
                }
                catch (Exception ex)
                {
                    Loger.Instance.LocalInfo($"{ex.Message}");
                }
                finally
                {
                    ThreadPool.QueueUserWorkItem(x =>
                    {
                        Setting.NyaaSocks5Point = 1088;
                        if (Setting.NyaaSSR.CheckOnline(@"https://sukebei.nyaa.si/"))
                        {
                            Setting.NyaaSocks5Point = Setting.NyaaSSR.SocksPort;
                        }
                    });
                }
            }),
            Task.Run(() => Setting.server = new server())).ContinueWith(obj
            => Loger.Instance.LocalInfo("数据库初始化完毕")).
            ContinueWith(obj =>
            {
                /*if (Setting.CheckOnline(Setting._GlobalSet.SocksCheck))
                {
                    Loger.Instance.LocalInfo("网络连接正常，正在加载下载进程");
                }
                else
                {
                    Loger.Instance.LocalInfo("外网访问失败，等待操作");
                }*/
                /* while (true)
                 {
                     Thread.Sleep(100);
                     Loger.Instance.LocalInfo("Text");
                     Loger.Instance.ServerInfo("", "Text");
                 }*/
                //Task.Run(() => DataBaseCommand.ChangeJavActress());
                if (Setting._GlobalSet.AutoRun) Setting.DownloadManage = new DownloadManage(); else Loger.Instance.LocalInfo("自动运行关闭，等待命令");

                /*var _controller = new ShadowsocksController();
                _controller.Start();
                using (var request = new HttpRequest())
                {
                    request.UserAgent = Http.ChromeUserAgent();
                    request.Proxy = Socks5ProxyClient.Parse("127.0.0.1:7071");
                    HttpResponse response = request.Get(@"sukebei.nyaa.si");
                    Console.WriteLine(response.ToString());
                    */
                /*    request
                        // Parameters URL-address.
                        .AddUrlParam("data1", "value1")
                        .AddUrlParam("data2", "value2")

                        // Parameters 'x-www-form-urlencoded'.
                        .AddParam("data1", "value1")
                        .AddParam("data2", "value2")
                        .AddParam("data2", "value2")

                        // Multipart data.
                        .AddField("data1", "value1")
                        .AddFile("game_code", @"C:\orion.zip")

                        // HTTP-header.
                        .AddHeader("X-Apocalypse", "21.12.12");

                    // These parameters are sent in this request.
                    request.Post("/").None();

                    // But in this request they will be gone.
                    request.Post("/").None();*/
                //}

                /*  Task.Factory.StartNew(() =>
                  {
                      while (!Setting.ShutdownResetEvent.Task.IsCompleted)
                      {
                          Thread.Sleep(5000);
                          Setting.StatusByte = Setting._GlobalSet.Send();
                      }
                  }, TaskCreationOptions.LongRunning);*/
                //Task.Factory.StartNew(() => new DownNyaaLoop().DownLoopAsync(), TaskCreationOptions.LongRunning);
                //Task.Factory.StartNew(() => { }, TaskCreationOptions.LongRunning);
            });
        }

        //private static readonly Stopwatch Time = new Stopwatch();

        #region MyRegion

        private static void Main2(string[] args)
        {
            /*  var DownloadCollect = new BlockingCollection<TorrentInfo2>();
              var DateRecordC = new BlockingCollection<DateRecord>();
              var CounT = 0;
              Task.Factory.StartNew(() =>
              {
                  using (var db = new LiteDatabase(@"Nyaa.db"))
                  {
                      var DateRecord = db.GetCollection<TorrentInfo>("NyaaDB");
                      CounT = DateRecord.Count();
                      Parallel.ForEach(DateRecord.FindAll(), VARIABLE =>
                      {
                          if (!string.IsNullOrEmpty(VARIABLE.Torrent))
                          {
                              DownloadCollect.TryAdd(new TorrentInfo2()
                              {
                                  Catagory = VARIABLE.Catagory,
                                  Timestamp = VARIABLE.id,
                                  Class = VARIABLE.Class,
                                  Title = VARIABLE.Title,
                                  Torrent = VARIABLE.Torrent,
                                  Magnet = VARIABLE.Magnet,
                                  Size = VARIABLE.Size,
                                  Date = VARIABLE.Date,
                                  Up = VARIABLE.Up,
                                  Leeches = VARIABLE.Leeches,
                                  Complete = VARIABLE.Complete
                              });
                          }
                      });
                      DownloadCollect.CompleteAdding();
                      var Record = db.GetCollection<DateRecord>("DateRecord");

                      Parallel.ForEach(Record.FindAll(), VARIABLE => { DateRecordC.TryAdd(VARIABLE); });
                      DateRecordC.CompleteAdding();
                      CounT = Record.Count();
                  }
              });
              var NowC = 0;
              Task.Factory.StartNew(() =>
              {
                  using (var db = new LiteDatabase(@"Nyaa2.db"))
                  {
                      var SettingData = db.GetCollection<GlobalSet>("Setting");
                      SettingData.Upsert(new GlobalSet {_id = "Address", Value = "https://sukebei.nyaa.si/"});
                      SettingData.Upsert(new GlobalSet {_id = "LastCount", Value = "2790"});
                      var DateRecord = db.GetCollection<DateRecord>("DateRecord");
                      DateRecord.EnsureIndex(X => X._id);
                      var NyaaDB = db.GetCollection<TorrentInfo2>("NyaaDB");
                      NyaaDB.EnsureIndex(x => x.Catagory);
                      NyaaDB.EnsureIndex(x => x.Date);
                      NyaaDB.EnsureIndex(x => x.id);

                      Parallel.ForEach(DownloadCollect.GetConsumingEnumerable(),
                          VARIABLE =>
                          {
                              NyaaDB.Insert(VARIABLE);
                              Interlocked.Increment(ref NowC);
                          });
                      NowC = 0;
                      Parallel.ForEach(DateRecordC.GetConsumingEnumerable(),
                          VARIABLE =>
                          {
                              DateRecord.Insert(VARIABLE);
                              Interlocked.Increment(ref NowC);
                          });
                  }
              });

              while (true)
              {
                  Console.SetCursorPosition(0, 0);
                  Console.Write("                                  ");
                  Console.SetCursorPosition(0, 0);
                  Console.Write($"{NowC}/{CounT}");
                  Thread.Sleep(500);
              }

              */

            /*   do
               {
                      var ST = string.Empty;

                   for (int i = 0; i < new Random().Next(10, 30); i++)
                   {
                       ST += $"启动{i}";
                   }
                   Loger.Instance.LocalInfo(ST);
                   Thread.Sleep(1);
               } while (true);*/

            /* Loger.Instance.LocalInfo($"启动");
             DataBaseCommand.Init();
             Loger.Instance.LocalInfo("数据库初始化完毕");
             Task.Factory.StartNew(() =>
             {
                 Loger.Instance.LocalInfo("主线程启动");
                 new DownLoop().DownLoopAsync();
             }, TaskCreationOptions.LongRunning);
             Console.CancelKeyPress += delegate { DataBaseCommand.SaveLastCountStatus(); };*/

            //new DownWork();
            /*   var Web= new  WebClient();
                  try
                  {
                      Web.DownloadString("https://sukebei.nyaa.si/?p=1");
                  }
                  catch (Exception ex)
                  {
                      while (ex != null)
                      {
                          Console.WriteLine(ex.Message);
                          ex = ex.InnerException;
                      }
                  }*/

            // Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd"));

            // new DownWork();
            /*  Task.Factory.StartNew(() =>
              {
                  Loger.Instance.Info("开始获取网页数据");
                 new DownloadHelp(Setting.LastPageIndex);
              }, Setting.CancelSign.Token);
              Task.Factory.StartNew(() =>
              {
                  Loger.Instance.Info("启动网页数据分析和保存");
                  //管道剩余待处理数据
                  //剩余处理时间
                  //处理完毕且管道已经清空
                  foreach (var item in DownloadHelp.DownloadCollect.GetConsumingEnumerable())
                  {
                  }
              }, Setting.CancelSign.Token);*/

            //test
            //new WebPageGet(@"https://sukebei.nyaa.si/?p=500000");
            //GetDataFromDataBase();
            // var ret = new HandlerHtml(File.ReadAllText("save.txt"));
            // SaveToDataBaseFormList(ret.AnalysisData.Values);
            //SaveToDataBaseOneByOne(ret.AnalysisData.Values);
            //var TCPCmd = TCPCommand.Init(1000);
            //TCPCmd.StartListener();
        }

        #endregion MyRegion
    }
}