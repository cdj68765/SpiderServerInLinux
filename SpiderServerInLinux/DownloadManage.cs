﻿using HtmlAgilityPack;
using LiteDB;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xNet;

namespace SpiderServerInLinux
{
    public class DownloadManage : IDisposable
    {
        public CancellationTokenSource NyaaDownloadCancel = new CancellationTokenSource();
        public CancellationTokenSource MiMiDownloadCancel = new CancellationTokenSource();
        public System.Timers.Timer GetNyaaNewDataTimer = new System.Timers.Timer();
        public CancellationTokenSource JavDownloadCancel = new CancellationTokenSource();
        public bool JavOldDownloadRunning = false;

        public DownloadManage()
        {
            if (Setting.CheckOnline(Setting._GlobalSet.SocksCheck))
            {
                Loger.Instance.LocalInfo("网络连接正常，正在加载下载进程");
                Load();
            }
            else
            {
                Loger.Instance.LocalInfo("外网访问失败，等待操作");
            }
        }

        private async void Load()
        {
            Loger.Instance.LocalInfo("初始化下载");
            await Task.WhenAll(/*GetJavNewData(), GetNyaaNewData(), */GetOldDate());
        }

        private Task GetNyaaNewData()
        {
            return Task.Run(() =>
            {
                {
                    GetNyaaNewDataTimer = new System.Timers.Timer(10000);
                    GetNyaaNewDataTimer.Elapsed += async delegate
                    {
                        Loger.Instance.LocalInfo("开始获取新Nyaa信息");
                        NyaaDownloadCancel = new CancellationTokenSource();
                        GetNyaaNewDataTimer.Stop();
                        var DownloadCollect = new BlockingCollection<Tuple<int, string>>();
                        //await Task.WhenAll(DownloadLoop(Setting._GlobalSet.NyaaAddress, Setting._GlobalSet.NyaaFin ? 0 : Setting._GlobalSet.NyaaLastPageIndex, DownloadCollect, NyaaNewDownloadCancel), HandlerNyaaHtml(DownloadCollect));
                        await Task.WhenAll(DownloadLoop(Setting._GlobalSet.NyaaAddress, 0, DownloadCollect, NyaaDownloadCancel), HandlerNyaaHtml(DownloadCollect));
                        GetNyaaNewDataTimer.Interval = new Random().Next(2, 12) * 3600 * 1000;
                        Setting.NyaaDownLoadNow = DateTime.Now.AddMilliseconds(GetNyaaNewDataTimer.Interval).ToString("MM-dd|HH:mm");
                        Loger.Instance.LocalInfo($"下次获得新数据为{Setting.NyaaDownLoadNow}");
                        GetNyaaNewDataTimer.Start();
                    };
                    GetNyaaNewDataTimer.Enabled = true;
                }
            });
        }

        private Task HandlerNyaaHtml(BlockingCollection<Tuple<int, string>> downloadCollect)
        {
            var SaveData = new BlockingCollection<Tuple<int, List<NyaaInfo>>>();
            Task.Factory.StartNew(() =>
            {
                using (var request = new HttpRequest())
                {
                    var _Day = "";
                    foreach (var item in SaveData.GetConsumingEnumerable())
                    {
                        try
                        {
                            if (!DataBaseCommand.SaveToNyaaDataBaseRange(item.Item2))
                            {
                                foreach (var AddTemp in item.Item2)
                                {
                                    if (!DataBaseCommand.SaveToNyaaDataBaseOneObject(AddTemp))
                                    {
                                        Loger.Instance.LocalInfo($"当前保存Nyaa下载日期{item.Item2.First().Day}");
                                        Loger.Instance.LocalInfo($"判断Nyaa下载完成");
                                        NyaaDownloadCancel.Cancel();
                                        downloadCollect.CompleteAdding();
                                        break;
                                    }
                                    else
                                    {
                                        Loger.Instance.LocalInfo($"当前保存Nyaa下载日期{item.Item2.First().Day}");
                                        Loger.Instance.LocalInfo($"判断Nyaa下载完成");
                                        NyaaDownloadCancel.Cancel();
                                        downloadCollect.CompleteAdding();
                                        break;
                                        Loger.Instance.LocalInfo($"检测到时间轴到达上一时间点");
                                        Loger.Instance.LocalInfo($"判断Nyaa下载完成");
                                        Setting._GlobalSet.NyaaFin = true;
                                        NyaaDownloadCancel.Cancel();
                                        downloadCollect.CompleteAdding();
                                        break;
                                    }
                                }
                            }

                            if (item.Item1 > Setting._GlobalSet.NyaaLastPageIndex)
                            {
                                Setting._GlobalSet.NyaaLastPageIndex = item.Item1;
                                if (_Day != item.Item2.First().Day)
                                {
                                    _Day = item.Item2.First().Day;
                                    Loger.Instance.LocalInfo($"当前保存Nyaa下载日期{_Day}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Loger.Instance.LocalInfo($"Nyaa保存出错{ex}");
                            break;
                        }
                    }
                    NyaaDownloadCancel.Cancel();
                    downloadCollect.CompleteAdding();
                }
            });
            return Task.Run(() =>
            {
                var HtmlDoc = new HtmlDocument();
                foreach (var Page in downloadCollect.GetConsumingEnumerable())
                {
                    if (string.IsNullOrEmpty(Page.Item2))
                    {
                        Task.Delay(1000);
                        continue;
                    }
                    HtmlDoc.LoadHtml(Page.Item2);
                    try
                    {
                        var TempSave = new List<NyaaInfo>();
                        foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"//div[@class='table-responsive']/table/tbody/tr"))
                        {
                            var TempData = new NyaaInfo();
                            var temp = HtmlNode.CreateNode(item.OuterHtml);
                            TempData.Class = item.Attributes["class"].Value;
                            TempData.Catagory = temp.SelectSingleNode("td[1]/a").Attributes["title"]
                                .Value;
                            TempData.Title = temp.SelectSingleNode("td[2]/a").Attributes["title"]
                                .Value;
                            TempData.Url = temp.SelectSingleNode("td[2]/a").Attributes["href"]
                                .Value;
                            TempData.Torrent = temp.SelectSingleNode("td[3]/a[1]").Attributes["href"].Value;
                            if (TempData.Torrent.StartsWith("magnet"))
                            {
                                TempData.Magnet = TempData.Torrent;
                                TempData.Torrent = "";
                            }
                            else
                            {
                                TempData.Magnet = temp.SelectSingleNode("td[3]/a[2]").Attributes["href"].Value;
                            }

                            TempData.Size = temp.SelectSingleNode("td[4]").InnerText;
                            TempData.Timestamp = int.Parse(temp.SelectSingleNode("td[5]").Attributes["data-timestamp"].Value);
                            TempData.Date = temp.SelectSingleNode("td[5]").InnerText;
                            TempData.Up = temp.SelectSingleNode("td[6]").InnerText;
                            TempData.Leeches = temp.SelectSingleNode("td[7]").InnerText;
                            TempData.Complete = temp.SelectSingleNode("td[8]").InnerText;
                            TempSave.Add(TempData);
                        }
                        SaveData.Add(new Tuple<int, List<NyaaInfo>>(Page.Item1, TempSave));
                    }
                    catch (Exception e)
                    {
                        File.WriteAllText("BugNyaaPage", Page.Item2);
                        Loger.Instance.LocalInfo($"Nyaa解析失败，失败原因{e}");
                        break;
                    }
                }
                downloadCollect.CompleteAdding();
                SaveData.CompleteAdding();
            });
        }

        public System.Timers.Timer GetJavNewDataTimer = new System.Timers.Timer();

        private Task GetJavNewData()
        {
            return Task.Run(() =>
            {
                {
                    GetJavNewDataTimer = new System.Timers.Timer(10000);
                    GetJavNewDataTimer.Elapsed += async delegate
                    {
                        Loger.Instance.LocalInfo("开始获取新Jav信息");
                        JavDownloadCancel = new CancellationTokenSource();
                        GetJavNewDataTimer.Stop();
                        var DownloadCollect = new BlockingCollection<Tuple<int, string>>();
                        await Task.WhenAll(DownloadLoop(Setting._GlobalSet.JavAddress, 0, DownloadCollect, JavDownloadCancel), HandlerJavHtml(DownloadCollect, true));
                        GetJavNewDataTimer.Interval = new Random().Next(6, 18) * 3600 * 1000;
                        Setting.JavDownLoadNow = DateTime.Now.AddMilliseconds(GetJavNewDataTimer.Interval).ToString("MM-dd|HH:mm");
                        Loger.Instance.LocalInfo($"下次获得新数据为{Setting.JavDownLoadNow}");
                        GetJavNewDataTimer.Start();
                    };
                    GetJavNewDataTimer.Enabled = true;
                }
            });
        }

        private Task GetOldDate()
        {
            return Task.Run(() =>
            {
                if (!Setting._GlobalSet.MiMiFin)
                {
                    var DownloadCollect = new BlockingCollection<Tuple<int, string>>();
                    MiMiDownloadCancel = new CancellationTokenSource();
                    //DownloadLoop(Setting._GlobalSet.MiMiAiAddress, Setting._GlobalSet.MiMiAiPageIndex, DownloadCollect, MiMiDownloadCancel, true);
                    HandlerMiMiHtml(DownloadCollect);
                }
                return;
                if (!Setting._GlobalSet.NyaaFin)
                {
                    var DownloadCollect = new BlockingCollection<Tuple<int, string>>();
                    NyaaDownloadCancel = new CancellationTokenSource();
                    DownloadLoop(Setting.NyaaAddress, Setting._GlobalSet.NyaaLastPageIndex, DownloadCollect, NyaaDownloadCancel);
                    HandlerOldNyaaHtml(DownloadCollect);
                }
                else
                {
                    Loger.Instance.LocalInfo($"Nyaa下载完毕，跳过旧数据获取");
                }
                if (Setting._GlobalSet.JavFin)
                {
                    var DownloadCollect = new BlockingCollection<Tuple<int, string>>();
                    DownloadLoop(Setting._GlobalSet.JavAddress, Setting._GlobalSet.JavLastPageIndex, DownloadCollect, JavDownloadCancel);
                    HandlerJavHtml(DownloadCollect);
                }
                else
                {
                    Loger.Instance.LocalInfo($"JAV下载完毕，跳过旧数据获取");
                }
            }, JavDownloadCancel.Token);
        }

        private Task HandlerMiMiHtml(BlockingCollection<Tuple<int, string>> downloadCollect)
        {
            return Task.WhenAny(Task.Run(() =>
             {
                 var HtmlDoc = new HtmlDocument();
                 foreach (var _Temp in downloadCollect.GetConsumingEnumerable())
                 {
                     try
                     {
                         HtmlDoc.LoadHtml(_Temp.Item2);
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
                                     DataBaseCommand.SaveToMiMiDataTablet(TempData);
                                 }
                             }
                         }
                         Setting._GlobalSet.MiMiAiPageIndex = _Temp.Item1;
                     }
                     catch (Exception ex)
                     {
                         Loger.Instance.LocalInfo($"MiMiAi页解析失败{ex.Message}");
                         if (ex.Message == "Object reference not set to an instance of an object.")
                         {
                             Loger.Instance.LocalInfo($"判断下载完成,退出下载进程");
                             Setting._GlobalSet.MiMiFin = true;
                             MiMiDownloadCancel.Cancel();
                             break;
                         }
                         continue;
                     }
                 }
             }, MiMiDownloadCancel.Token), Task.Run(() =>
              {
                  void HandleMiMiPage(string PageData)
                  {
                      var HtmlDoc = new HtmlDocument();
                      HtmlDoc.LoadHtml(PageData);
                      List<MiMiAiData> ItemList = new List<MiMiAiData>();
                      var Temp = new MiMiAiData();
                      var Index = 0;
                      foreach (var Child in HtmlDoc.DocumentNode.SelectNodes("//div[@class='t_msgfont']")[0].ChildNodes)
                      {
                          if (Temp.InfoList == null) Temp.InfoList = new List<MiMiAiData.BasicData>();
                          switch (Child.Name)
                          {
                              case "a":
                                  {
                                      Temp.InfoList.Add(new MiMiAiData.BasicData() { Type = "torrent", info = Child.Attributes["href"].Value });
                                      Temp.Index = Index;
                                      Temp.Date = Setting.MiMiDay;
                                      ItemList.Add(Temp);
                                      Temp = new MiMiAiData();
                                      Interlocked.Increment(ref Index);
                                  }
                                  break;

                              case "#text":
                                  {
                                      var innerText = Child.InnerText.Replace("\r\n", "").Replace("&nbsp;", "");
                                      if (innerText.StartsWith(" ")) innerText = innerText.Remove(0, 1);
                                      if (string.IsNullOrEmpty(Temp.id) && innerText != "\r\n")
                                      {
                                          Temp.id = innerText;
                                          break;
                                      }
                                      if (!string.IsNullOrEmpty(innerText))
                                          Temp.InfoList.Add(new MiMiAiData.BasicData() { Type = "text", info = innerText });
                                  }
                                  break;

                              case "br":
                                  break;

                              case "img":
                                  Temp.InfoList.Add(new MiMiAiData.BasicData() { Type = "img", info = Child.Attributes["src"].Value });
                                  break;

                              default:
                                  break;
                          }
                      }
                      DataBaseCommand.SaveToMiMiDataUnit(ItemList);
                  }
                  using (var request = new HttpRequest()
                  {
                      UserAgent = Http.ChromeUserAgent(),
                      ConnectTimeout = 20000,
                      CharacterSet = Encoding.GetEncoding("GBK")
                  })
                  {
                      if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Setting.Socks5Point}");
                      var ErrorCount = 0;
                      while (!MiMiDownloadCancel.Token.IsCancellationRequested)
                      {
                          var PageInfo = DataBaseCommand.GetDataFromMiMi("TabletInfo") as BsonDocument;
                          if (PageInfo != null)
                          {
                              try
                              {
                                  Setting.MiMiDay = PageInfo["_id"].AsDateTime.ToString("yyyy-MM-dd");
                                  var downurl = new Uri($"http://{new Uri(Setting._GlobalSet.MiMiAiAddress).Host}/{PageInfo["Uri"].AsString}");
                                  HttpResponse response = request.Get(downurl);
                                  if (response.Address.Authority != downurl.Authority)
                                  {
                                      Loger.Instance.LocalInfo($"MiMiAi网址变更为{response.Address.Authority}");
                                      Setting._GlobalSet.MiMiAiAddress.Replace(downurl.Authority, response.Address.Authority);
                                  }
                                  HandleMiMiPage(response.ToString());
                                  PageInfo["Status"] = bool.TrueString;
                                  DataBaseCommand.SaveToMiMiDataTablet(PageInfo);
                              }
                              catch (Exception ex)
                              {
                                  if (MiMiDownloadCancel.Token.IsCancellationRequested)
                                  {
                                      break;
                                  }
                                  Interlocked.Increment(ref ErrorCount);
                                  Loger.Instance.LocalInfo($"{ex.Message}");
                                  Loger.Instance.LocalInfo($"下载MiMiAi数据失败，计数{ErrorCount}次");
                                  if (ex.Message.StartsWith("Cannot access a disposed object"))
                                  {
                                      Loger.Instance.LocalInfo($"SSR异常，退出全部下载进程");
                                      MiMiDownloadCancel.Cancel();
                                      break;
                                  }
                                  var time = new Random().Next(5000, 10000);
                                  for (var i = time; i > 0; i -= 1000)
                                  {
                                      Loger.Instance.WaitTime(i / 1000);
                                      Thread.Sleep(1000);
                                  }
                                  if (ErrorCount > 5)
                                  {
                                      ErrorCount = 0;
                                      Loger.Instance.LocalInfo($"下载MIMiAi数据错误超过5次，退出下载进程");
                                      if (!Setting.CheckOnline(Setting._GlobalSet.SocksCheck))
                                      {
                                          Loger.Instance.LocalInfo($"检测到网络连接异常，退出全部下载方式");
                                          Setting.CancelSign.Cancel();
                                      }
                                      MiMiDownloadCancel.Cancel();
                                      break;
                                  }
                              }
                              finally
                              {
                                  ErrorCount = 0;
                              }
                          }
                          else
                          {
                              Thread.Sleep(10000);
                          }
                      }
                  }
              }), Task.Run(() =>
              {
                  using (var request = new HttpRequest()
                  {
                      UserAgent = Http.ChromeUserAgent(),
                      ConnectTimeout = 10000,
                      CharacterSet = Encoding.GetEncoding("GBK")
                  })
                  {
                      if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Setting.Socks5Point}");
                      while (!MiMiDownloadCancel.Token.IsCancellationRequested)
                      {
                          var UnitInfo = DataBaseCommand.GetDataFromMiMi("UnitInfo") as MiMiAiData;
                          if (UnitInfo != null)
                          {
                              foreach (var Item in UnitInfo.InfoList)
                              {
                                  switch (Item.Type)
                                  {
                                      case "img":
                                          {
                                              try
                                              {
                                                  var _PostData = request.Get(Item.info);
                                                  if (!string.IsNullOrEmpty(_PostData.ToString()))
                                                  {
                                                      Item.Data = _PostData.Ret;
                                                  }
                                                  else
                                                  {
                                                      throw new Exception("Unknown Error");
                                                  }
                                              }
                                              catch (Exception ex)
                                              {
                                                  DataBaseCommand.SaveToMiMiDataErrorUnit(new[]
                                                    {
                                                      UnitInfo.Date,
                                                      UnitInfo.Index.ToString(),
                                                      UnitInfo.InfoList.IndexOf(Item).ToString(),
                                                      "img",
                                                      Item.info,
                                                      ex.Message,
                                                      bool.FalseString
                                                  });
                                              }
                                          }
                                          break;

                                      case "torrent":
                                          {
                                              var _TempUri = new Uri(Item.info);
                                              try
                                              {
                                                  var _TempDownloadUri = _TempUri.Scheme + Uri.SchemeDelimiter + _TempUri.Authority + "/load.php";
                                                  var _PostData = request.Post(_TempDownloadUri, new RequestParams()
                                              {
                                                  new KeyValuePair<string, string>("ref", _TempUri.Query.Split('=')[1]),
                                                  new KeyValuePair<string, string>("submit ", "点击下载")
                                              }).ToBytes();
                                                  if (_PostData.Length > 1000)
                                                  {
                                                      Item.Data = _PostData;
                                                  }
                                                  else if (Encoding.Default.GetString(_PostData).StartsWith("No such file"))
                                                  {
                                                      throw new Exception("No such file");
                                                  }
                                                  else
                                                  {
                                                      throw new Exception("Unknown Error");
                                                  }
                                              }
                                              catch (Exception ex)
                                              {
                                                  DataBaseCommand.SaveToMiMiDataErrorUnit(new[]
                                                  {
                                                      UnitInfo.Date,
                                                      UnitInfo.Index.ToString(),
                                                      UnitInfo.InfoList.IndexOf(Item).ToString(),
                                                      "torrent",
                                                      Item.info,
                                                      ex.Message,
                                                      bool.FalseString
                                                  });
                                              }
                                          }
                                          break;

                                      default:
                                          break;
                                  }
                              }
                              UnitInfo.Status = true;
                              DataBaseCommand.SaveToMiMiDataUnit(UnitInfo);
                          }
                          else
                          {
                              Thread.Sleep(60000);
                          }
                      }
                  }
              }));
        }

        private Task HandlerOldNyaaHtml(BlockingCollection<Tuple<int, string>> downloadCollect)
        {
            var SaveData = new BlockingCollection<Tuple<int, NyaaInfo>>();
            Task.Factory.StartNew(() =>
            {
                using (var request = new HttpRequest())
                {
                    List<NyaaInfo> Save = new List<NyaaInfo>();
                    foreach (var item in SaveData.GetConsumingEnumerable())
                    {
                        Save.Add(item.Item2);
                        if (item.Item2.Day != Setting.NyaaDay)
                        {
                            Setting.NyaaDay = item.Item2.Day;
                            Loger.Instance.LocalInfo($"当前保存Nyaa下载日期{Setting.NyaaDay}");
                        }
                        if (Save.Count > 25 || SaveData.IsCompleted)
                        {
                            if (!DataBaseCommand.SaveToNyaaDataBaseRange(Save))
                            {
                                foreach (var AddTemp in Save)
                                {
                                    DataBaseCommand.SaveToNyaaDataBaseOneObject(AddTemp, false);
                                }
                            }
                            Setting._GlobalSet.NyaaLastPageIndex = item.Item1;
                            Save.Clear();
                        }
                    }
                    if (Save.Count != 0)
                    {
                        if (!DataBaseCommand.SaveToNyaaDataBaseRange(Save))
                        {
                            foreach (var AddTemp in Save)
                            {
                                DataBaseCommand.SaveToNyaaDataBaseOneObject(AddTemp, false);
                            }
                        }
                        Save.Clear();
                    }
                    Loger.Instance.LocalInfo($"当前保存旧Nyaa线程退出");
                }
            });
            return Task.Run(() =>
            {
                var HtmlDoc = new HtmlDocument();
                foreach (var Page in downloadCollect.GetConsumingEnumerable())
                {
                    if (string.IsNullOrEmpty(Page.Item2))
                    {
                        Task.Delay(1000);
                        continue;
                    }
                    HtmlDoc.LoadHtml(Page.Item2);
                    try
                    {
                        var TempData = new NyaaInfo();
                        var temp = HtmlNode.CreateNode(HtmlDoc.DocumentNode.OuterHtml);
                        TempData.Class = temp.SelectSingleNode("/html/body/div[1]/div[2]").Attributes["class"]
                            .Value.Split('-')[1];
                        TempData.Catagory = $"{temp.SelectSingleNode("/html/body/div[1]/div[2]/div[2]/div[1]/div[2]/a[1]").InnerText} - {temp.SelectSingleNode("/html/body/div[1]/div[2]/div[2]/div[1]/div[2]/a[2]").InnerText}";
                        TempData.Title = temp.SelectSingleNode("/html/body/div[1]/div[2]/div[1]/h3").InnerText.Replace("\r", "").Replace("\t", "").Replace("\n", "");
                        TempData.Url = $"/view/{Page.Item1}";
                        TempData.Torrent = temp.SelectSingleNode("/html/body/div[1]/div[2]/div[3]/a[1]").Attributes["href"].Value;
                        TempData.Magnet = temp.SelectSingleNode("/html/body/div[1]/div[2]/div[3]/a[2]").Attributes["href"].Value;
                        TempData.Size = temp.SelectSingleNode("/html/body/div[1]/div[2]/div[2]/div[4]/div[2]").InnerText;
                        TempData.Timestamp = int.Parse(temp.SelectSingleNode("/html/body/div[1]/div[2]/div[2]/div[1]/div[4]").Attributes["data-timestamp"].Value);
                        TempData.Date = temp.SelectSingleNode("/html/body/div[1]/div[2]/div[2]/div[1]/div[4]").InnerText.Replace(" UTC", "");
                        TempData.Up = temp.SelectSingleNode("/html/body/div[1]/div[2]/div[2]/div[2]/div[4]/span").InnerText;
                        TempData.Leeches = temp.SelectSingleNode("/html/body/div[1]/div[2]/div[2]/div[3]/div[4]/span").InnerText;
                        TempData.Complete = temp.SelectSingleNode("/html/body/div[1]/div[2]/div[2]/div[4]/div[4]").InnerText;
                        SaveData.Add(new Tuple<int, NyaaInfo>(Page.Item1, TempData));
                    }
                    catch (Exception e)
                    {
                        Loger.Instance.LocalInfo($"Nyaa解析失败，失败原因{e}");
                        break;
                    }
                }
                NyaaDownloadCancel.Cancel();
                downloadCollect.CompleteAdding();
                SaveData.CompleteAdding();
            });
        }

        private Task HandlerJavHtml(BlockingCollection<Tuple<int, string>> downloadCollect, bool GetNew = false)
        {
            var SaveData = new BlockingCollection<Tuple<int, JavInfo>>();
            var NewDate = "";
            if (GetNew)
            {
                Task.Factory.StartNew(() =>
                {
                    using (var request = new HttpRequest())
                    {
                        foreach (var item in SaveData.GetConsumingEnumerable())
                        {
                            try
                            {
                                request.UserAgent = Http.ChromeUserAgent();
                                if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Setting.Socks5Point}");
                                try
                                {
                                    HttpResponse response = request.Get(item.Item2.ImgUrl);
                                    if (!string.IsNullOrEmpty(response.ToString()))
                                    {
                                        item.Item2.Image = response.Ret;
                                    }
                                }
                                catch (Exception)
                                {
                                    try
                                    {
                                        HttpResponse response = request.Get(item.Item2.ImgUrlError);
                                        if (!string.IsNullOrEmpty(response.ToString()))
                                        {
                                            item.Item2.Image = response.Ret;
                                        }
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                                if (NewDate != item.Item2.Date)
                                {
                                    Loger.Instance.LocalInfo($"当前保存Jav下载日期{item.Item2.Date}");
                                    NewDate = item.Item2.Date;
                                }
                                if (!DataBaseCommand.SaveToJavDataBaseOneObject(item.Item2))
                                {
                                    Loger.Instance.LocalInfo($"找到重复Jav项退出当前下载项");
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Loger.Instance.LocalInfo(ex);
                            }
                        }
                        JavDownloadCancel.Cancel();
                        downloadCollect.CompleteAdding();
                    }
                });
            }
            else
            {
                Task.Factory.StartNew(() =>
                {
                    int PageIndex = 0;
                    using (var request = new HttpRequest())
                    {
                        List<JavInfo> Save = new List<JavInfo>();
                        foreach (var item in SaveData.GetConsumingEnumerable())
                        {
                            try
                            {
                                if (PageIndex != item.Item1)
                                {
                                    PageIndex = item.Item1;
                                    if (NewDate != item.Item2.Date)
                                    {
                                        Loger.Instance.LocalInfo($"当前保存Jav下载日期{item.Item2.Date}");
                                        NewDate = item.Item2.Date;
                                    }
                                    if (Save.Count != 0)
                                    {
                                        try
                                        {
                                            DataBaseCommand.SaveToJavDataBaseRange(Save);
                                        }
                                        catch (Exception e)
                                        {
                                            Loger.Instance.LocalInfo(e);
                                            DataBaseCommand.SaveToJavDataBaseOneByOne(Save);
                                        }
                                        Setting._GlobalSet.JavLastPageIndex = item.Item1;
                                        Save.Clear();
                                    }
                                }
                                request.UserAgent = Http.ChromeUserAgent();
                                if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Setting.Socks5Point}");
                                try
                                {
                                    HttpResponse response = request.Get(item.Item2.ImgUrl);
                                    if (!string.IsNullOrEmpty(response.ToString()))
                                    {
                                        item.Item2.Image = response.Ret;
                                    }
                                }
                                catch (Exception)
                                {
                                    try
                                    {
                                        HttpResponse response = request.Get(item.Item2.ImgUrlError);
                                        if (!string.IsNullOrEmpty(response.ToString()))
                                        {
                                            item.Item2.Image = response.Ret;
                                        }
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                                Save.Add(item.Item2);
                            }
                            catch (Exception ex)
                            {
                                Loger.Instance.LocalInfo(ex);
                            }
                        }
                        Loger.Instance.LocalInfo($"当前保存Jav解析进程退出");
                        JavOldDownloadRunning = false;
                    }
                });
            }
            return Task.Run(() =>
            {
                var HtmlDoc = new HtmlDocument();
                foreach (var Page in downloadCollect.GetConsumingEnumerable())
                {
                    if (string.IsNullOrEmpty(Page.Item2))
                    {
                        Task.Delay(1000);
                        continue;
                    }
                    HtmlDoc.LoadHtml(Page.Item2);
                    try
                    {
                        foreach (var item in HtmlDoc.DocumentNode.SelectNodes(@"/html/body/div[1]/div"))
                        {
                            var TempData = new JavInfo();
                            var temp = HtmlNode.CreateNode(item.OuterHtml);
                            TempData.ImgUrl = temp.SelectSingleNode("div/div/div[1]/img").Attributes["src"].Value;
                            TempData.ImgUrlError = temp.SelectSingleNode("div/div/div[1]/img").Attributes["onerror"].Value.Split('\'')[1];
                            TempData.id = temp.SelectSingleNode("div/div/div[2]/div/h5/a").InnerText.Replace("\n", "");
                            TempData.Size = temp.SelectSingleNode("div/div/div[2]/div/h5/span").InnerText;
                            TempData.Date = $"{DateTime.Parse(temp.SelectSingleNode("div/div/div[2]/div/p[1]/a").Attributes["href"].Value.Substring(1)):yy-MM-dd}";
                            var tags = new List<string>();
                            try
                            {
                                foreach (var Tags in temp.SelectNodes(@"div/div/div[2]/div/div[1]/a"))
                                {
                                    tags.Add(Tags.InnerText.Replace("\n", ""));
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
                                foreach (var Tags in temp.SelectNodes(@"div/div/div[2]/div/div[2]/a"))
                                {
                                    Actress.Add(Tags.InnerText.Replace("\n", ""));
                                }
                            }
                            catch (Exception)
                            {
                            }
                            TempData.Actress = Actress.ToArray();
                            TempData.Magnet = temp.SelectSingleNode("div/div/div[2]/div/a[1]").Attributes["href"].Value;
                            SaveData.Add(new Tuple<int, JavInfo>(Page.Item1, TempData));
                        }
                    }
                    catch (Exception)
                    {
                        Loger.Instance.LocalInfo($"Jav解析失败，推测下载完毕");
                        Setting._GlobalSet.JavFin = false;
                        downloadCollect.CompleteAdding();
                        SaveData.CompleteAdding();
                        JavDownloadCancel.Cancel();
                    }
                }
                SaveData.CompleteAdding();
            });
        }

        private Task DownloadLoop(string Address, int LastPageIndex, BlockingCollection<Tuple<int, string>> downloadCollect, CancellationTokenSource token, bool CheckMiMiAiAddress = false)
        {
            return Task.Factory.StartNew(() =>
              {
                  using (var request = new HttpRequest()
                  {
                      UserAgent = Http.ChromeUserAgent(),
                      ConnectTimeout = 20000,
                  })
                  {
                      if (CheckMiMiAiAddress)
                      {
                          request.CharacterSet = Encoding.GetEncoding("GBK");
                      }
                      int ErrorCount = 0;
                      if (Setting._GlobalSet.SocksCheck) request.Proxy = Socks5ProxyClient.Parse($"127.0.0.1:{Setting.Socks5Point}");
                      while (!token.Token.IsCancellationRequested)
                      {
                          var downurl = new Uri($"{Address}{LastPageIndex}");
                          try
                          {
                              Thread.Sleep(500);
                              HttpResponse response = request.Get(downurl);
                              downloadCollect.Add(new Tuple<int, string>(LastPageIndex, response.ToString()));
                              Interlocked.Increment(ref LastPageIndex);
                              if (CheckMiMiAiAddress)
                              {
                                  if (response.Address.Authority != downurl.Authority)
                                  {
                                      Loger.Instance.LocalInfo($"MiMiAi网址变更为{response.Address.Authority}");
                                      Setting._GlobalSet.MiMiAiAddress.Replace(downurl.Authority, response.Address.Authority);
                                      Address = Setting._GlobalSet.MiMiAiAddress;
                                  }
                              }

                              ErrorCount = 0;
                          }
                          catch (Exception ex)
                          {
                              if (token.Token.IsCancellationRequested)
                              {
                                  break;
                              }
                              Interlocked.Increment(ref ErrorCount);
                              if (Address == Setting.NyaaAddress)
                              {
                                  if (ex.Message == "NotFound")
                                  {
                                      Interlocked.Increment(ref LastPageIndex);
                                      continue;
                                  }
                              }
                              if (ex.Message.StartsWith("Cannot access a disposed object"))
                              {
                                  Loger.Instance.LocalInfo($"SSR异常，退出全部下载进程");
                                  token.Cancel();
                                  break;
                              }
                              Loger.Instance.LocalInfo($"{ex.Message}");
                              Loger.Instance.LocalInfo($"下载{downurl.ToString()}失败，计数{ErrorCount}次");
                              //Loger.Instance.LocalInfo(ex.Message);
                              var time = new Random().Next(5000, 10000);
                              for (var i = time; i > 0; i -= 1000)
                              {
                                  if (token.Token.IsCancellationRequested)
                                  {
                                      break;
                                  }
                                  Loger.Instance.WaitTime(i / 1000);
                                  Thread.Sleep(1000);
                              }
                              if (ErrorCount > 5)
                              {
                                  ErrorCount = 0;
                                  Loger.Instance.LocalInfo($"下载{downurl.ToString()}失败，退出下载进程");
                                  if (!Setting.CheckOnline(Setting._GlobalSet.SocksCheck))
                                  {
                                      Loger.Instance.LocalInfo($"检测到网络连接异常，退出全部下载方式");
                                      Setting.CancelSign.Cancel();
                                  }
                                  token.Cancel();
                                  break;
                              }
                          }
                      }
                      downloadCollect.CompleteAdding();
                  }
              }).ContinueWith(a =>
              {
                  Loger.Instance.LocalInfo($"接收到退出信号，已经退出{new Uri(Address).Authority}的下载进程");
              });
        }

        public void Dispose()
        {
            Setting.DownloadManage.JavDownloadCancel.Cancel();
            Setting.DownloadManage.NyaaDownloadCancel.Cancel();
            Setting.DownloadManage.MiMiDownloadCancel.Cancel();
            GetJavNewDataTimer.Dispose();
            while (!Setting.DownloadManage.JavOldDownloadRunning && !Setting.DownloadManage.JavDownloadCancel.IsCancellationRequested && !MiMiDownloadCancel.IsCancellationRequested)
            {
                Thread.Sleep(1000);
            }
        }
    }
}