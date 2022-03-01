using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace SpiderServerInLinux
{
    internal interface ILoger
    {
        void Warn(object msg);

        void LocalInfo(object msg);

        void Debug(object msg);

        void Error(object msg);
    }

    public class Loger : ILoger
    {
        private static Loger instance;

        private Loger()
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new LogerTraceListener());
        }

        public static Loger Instance
        {
            get
            {
                if (instance == null)
                    instance = new Loger();
                return instance;
            }
        }

        public void Debug(object msg)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Trace.WriteLine(msg, "调试");
        }

        public void Warn(object msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Trace.WriteLine(msg, "警告");
        }

        public void LocalInfo(object msg)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Trace.WriteLine(msg, "信息");
        }

        public void Error(object msg)
        {
            Trace.WriteLine(msg, "错误");
        }

        public void WithTimeStart(object msg, Stopwatch Time)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Time.Start();
            Trace.WriteLine(msg, "信息");
        }

        public void WithTimeRestart(object msg, Stopwatch Time)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Trace.WriteLine($"{msg},用时:{Time.ElapsedMilliseconds}毫秒", "信息");
            Time.Restart();
        }

        public void WithTimeStop(object msg, Stopwatch Time)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Trace.WriteLine($"{msg},用时:{Time.ElapsedMilliseconds}毫秒", "信息");
            Time.Stop();
        }

        public void DateInfo(string msg)
        {
            Trace.Write(msg);
        }

        internal void WaitTime(int i)
        {
            Trace.Write(i);
        }

        internal void ServerInfo(string Host, string Info)
        {
            Trace.Write(Host, Info);
        }

        internal void ServerInfo(string Host, Exception Info)
        {
            Trace.Write(Host, Info.Message);
        }
    }

    public class LogerTraceListener : TraceListener
    {
        private int WindowHeight;

        private int WindowWidth;

        private void Init()
        {
            Console.CursorVisible = false;
            Console.Clear();
            Console.CancelKeyPress += delegate { Console.Clear(); };
            foreach (var item in from X in Enumerable.Range(0, Console.WindowWidth)
                                 from Y in Enumerable.Range(0, Console.WindowHeight)
                                 select new Tuple<int, int>(X, Y))
                if (item.Item1 == 0 && item.Item2 == 0)
                {
                    Console.SetCursorPosition(item.Item1, item.Item2);
                    Console.Write("╭");
                }
                else if (item.Item1 == 0 && item.Item2 == Console.WindowHeight - 1)
                {
                    Console.SetCursorPosition(item.Item1, item.Item2);
                    Console.Write("╰");
                }
                else if (item.Item1 == Console.WindowWidth - 1 && item.Item2 == 0)
                {
                    Console.SetCursorPosition(item.Item1, item.Item2);
                    Console.Write("╮");
                }
                else if (item.Item1 == Console.WindowWidth - 1 && item.Item2 == Console.WindowHeight - 1)
                {
                    Console.SetCursorPosition(item.Item1, item.Item2);
                    Console.Write("╯");
                }
                else if (item.Item1 == 0)
                {
                    Console.SetCursorPosition(item.Item1, item.Item2);
                    Console.Write("│");
                }
                else if (item.Item1 == Console.WindowWidth - 1)
                {
                    Console.SetCursorPosition(item.Item1, item.Item2);
                    Console.Write("┃");
                }
                else if (item.Item2 == Console.WindowHeight - 1)
                {
                    Console.SetCursorPosition(item.Item1, item.Item2);
                    Console.Write("━");
                }
                else if (item.Item2 == 0)
                {
                    Console.SetCursorPosition(item.Item1, item.Item2);
                    Console.Write("─");
                }
                else
                {
                    if (item.Item1 == 1)
                    {
                        Console.SetCursorPosition(1, 1);
                        Console.Write($"当前下载页面:{0}");
                        Console.SetCursorPosition(Console.WindowWidth / 2 + 2, 1);
                        //Console.Write($"倒计时:{0}秒");
                    }
                    else if (item.Item2 == 3)
                    {
                        if (item.Item1 == Console.WindowWidth / 4)
                        {
                            Console.SetCursorPosition(item.Item1, item.Item2);
                            Console.Write("本地信息");
                        }
                        else if (item.Item1 == Console.WindowWidth - Console.WindowWidth / 4)
                        {
                            Console.SetCursorPosition(item.Item1, item.Item2);
                            Console.Write("远程信息");
                        }
                        else if (item.Item1 == Console.WindowWidth / 2)
                        {
                            Console.SetCursorPosition(item.Item1, item.Item2);
                            Console.Write("┆");
                        }
                    }
                    else if (item.Item2 == 2 || item.Item2 == 4)
                    {
                        Console.SetCursorPosition(item.Item1, item.Item2);
                        Console.Write("┅");
                    }
                    else if (item.Item1 == Console.WindowWidth / 2 && item.Item2 != 1)
                    {
                        Console.SetCursorPosition(item.Item1, item.Item2);
                        Console.Write("┆");
                    }
                }
            Console.SetCursorPosition(0, 0);
        }

        private bool Checking = false;

        public void Check()
        {
            if (!Checking)
            {
                Checking = true;
                Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        Thread.Sleep(Setting.LoopTime);
                        _Check();
                    }
                });
            }

            _Check();
            void _Check()
            {
                try
                {
                    if (Console.WindowHeight != WindowHeight || Console.WindowWidth != WindowWidth)
                    {
                        Init();
                        DrawLocal();
                        DrawRemote();
                        WindowHeight = Console.WindowHeight;
                        WindowWidth = Console.WindowWidth;
                    }
                    for (var i = Console.WindowWidth / 2 + Console.WindowWidth / 6 - 1; i < Console.WindowWidth - 1; i++)
                    {
                        Console.SetCursorPosition(i, 1);
                        Console.Write(" ");
                    }
                    try
                    {
                        for (var i = 1; i < Console.WindowWidth / 2 - 1; i++)
                        {
                            Console.SetCursorPosition(i, 1);
                            Console.Write(" ");
                        }
                        Console.SetCursorPosition(1, 1);
                        if (Setting._GlobalSet != null)
                        {
                            var ShowJav = Setting._GlobalSet.JavFin ? $"当前Jav下载页面:{Setting._GlobalSet.JavLastPageIndex}" : $"Jav:{Setting.JavDownLoadNow}";
                            var ShowNyaa = !Setting._GlobalSet.NyaaFin ? $"Nyaa:{Setting.NyaaDownLoadNow}" : $"Nyaa:{Setting.NyaaDownLoadNow}";
                            var ShowMiMi = Setting._GlobalSet.MiMiFin ? $"MiMi:{Setting.MiMiDownLoadNow},{Setting.MiMiDay}" : $"MiMi:{Setting._GlobalSet.MiMiAiPageIndex},{Setting.MiMiDay},{Setting.MiMiDownLoadNow}";
                            var ShowMiMiStory = $"Story:{Setting.MiMiAiStoryDownLoadNow}";
                            var T66yDownLoadNow = $"T66y:{Setting.T66yDownLoadNow}";
                            Console.Write($"|{ShowNyaa}|{ShowJav}|{ShowMiMi}|{ShowMiMiStory}|{T66yDownLoadNow}");
                        }
                    }
                    catch (Exception)
                    {
                    }

                    Console.SetCursorPosition(Console.WindowWidth / 2 + Console.WindowWidth / 6 - 1, 1);
                    Console.Write($"内存使用量:{Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB");
                    if (Setting.SSR != null || true)
                    {
                        Console.SetCursorPosition(Console.WindowWidth / 2 + Console.WindowWidth / 3 - 3, 1);
                        try
                        {
                            if (Setting._GlobalSet != null)
                                Console.Write($"SSR流量:{HumanReadableFilesize((double)Setting._GlobalSet.totalDownloadBytes)}");
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                catch (Exception e)
                {
                    Loger.Instance.ServerInfo("SSR", e);
                    Init();
                    DrawLocal();
                    DrawRemote();
                    WindowHeight = Console.WindowHeight;
                    WindowWidth = Console.WindowWidth;
                }
                try
                {
                    if (Setting.Remote.Count > 1024)
                        Setting.Remote.Clear();
                    if (Setting.LocalInfoC.Count > 1024)
                        Setting.LocalInfoC.Clear();
                }
                catch (Exception e)
                {
                }
                String HumanReadableFilesize(double size)
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

        private void DrawLocal()
        {
            var Top = 5;

            /*  if (Setting.server != null)
              {
                  ThreadPool.QueueUserWorkItem(async (object state) =>
                  {
                      var OnlineCheck = Setting.server.ModuleCatalog.GetModule(typeof(server.OnlineCheck));
                      if (OnlineCheck.SessionCount > 0)
                      {
                          await OnlineCheck.Broadcast(OnlineOpera.Send());
                      }
                  });
              }*/

            foreach (var VARIABLE in Setting.LocalInfoC)
            {
                foreach (var VARIABLE2 in StringSplit(VARIABLE))
                {
                    if (string.IsNullOrEmpty(VARIABLE2)) continue;
                    for (var i = 2; i < Console.WindowWidth / 2; i++)
                    {
                        Console.SetCursorPosition(i, Top);
                        Console.Write(" ");
                    }

                    Console.SetCursorPosition(2, Top);
                    Console.Write(VARIABLE2);

                    Top += 1;
                    if (Top > Console.WindowHeight - 2) break;
                }

                if (Top > Console.WindowHeight - 2) break;
            }
        }

        private void DrawRemote()
        {
            var Top = 5;
            foreach (var VARIABLE in Setting.Remote)
            {
                foreach (var VARIABLE2 in StringSplit(VARIABLE))
                {
                    if (string.IsNullOrEmpty(VARIABLE2)) continue;
                    for (var i = (Console.WindowWidth / 2) + 2; i < Console.WindowWidth - 1; i++)
                    {
                        Console.SetCursorPosition(i, Top);
                        Console.Write(" ");
                    }

                    Console.SetCursorPosition((Console.WindowWidth / 2) + 2, Top);
                    Console.Write(VARIABLE2);

                    Top += 1;
                    if (Top > Console.WindowHeight - 2) break;
                }

                if (Top > Console.WindowHeight - 2) break;
            }
        }

        private List<string> StringSplit(string s)
        {
            var temp = new List<string>();
            var StringSize = Encoding.Default.GetByteCount(s);
            var WindowsSize = (Console.WindowWidth / 2) - 3;
            var CharArray = s.ToArray();
            do
            {
                if (StringSize > WindowsSize)
                {
                    var byteCount = 0;
                    var pos = 0;
                    for (var j = 0; j < CharArray.Length; j++)
                    {
                        byteCount += CharArray[j] > 255 ? 2 : 1;
                        if (byteCount > WindowsSize)
                        {
                            pos = j;
                            break;
                        }

                        if (byteCount == WindowsSize)
                        {
                            pos = j + 1;
                            break;
                        }
                    }

                    if (pos == 0) pos = s.Length;

                    temp.Add(s.Substring(0, pos));
                    s = s.Substring(pos);
                    StringSize = Encoding.Default.GetByteCount(s);
                    CharArray = s.ToArray();
                }
                else
                {
                    if (!string.IsNullOrEmpty(s)) temp.Add(s);

                    return temp;
                }
            } while (true);
        }

        public override void Write(object message)
        {
            /*   Check();
               Console.SetCursorPosition(Console.WindowWidth / 2 + 2, 1);
               Console.Write($"倒计时:{message}秒");*/
        }

        public override void Write(string message)
        {
            Check();
            Console.SetCursorPosition(1, 1);
            Console.Write($"当前Nyaa下载页面:{Setting._GlobalSet.NyaaLastPageIndex}  当前下载日期:{message}");
        }

        public override void WriteLine(string message)
        {
            Check();
            Setting.LocalInfoC.Push($"{DateTime.Now:MM-dd HH:mm:ss}->{message}");
            DrawLocal();
        }

        public override void WriteLine(string message, string category)
        {
            Check();
            Setting.LocalInfoC.Push($"{DateTime.Now:MM-dd HH:mm:ss}->[{category}]{message}");
            DrawLocal();
        }

        public override void Write(string category, string message)
        {
            Check();
            Setting.Remote.Push($"{DateTime.Now:MM-dd HH:mm:ss}->[{category}]{message}");
            DrawRemote();
        }
    }
}