using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

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
        /// <summary>
        ///     Single Instance
        /// </summary>
        private static Loger instance;

        /// <summary>
        ///     Constructor
        /// </summary>
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
            Trace.WriteLine(msg, "信息");
            Time.Stop();
        }

        public void PageInfo(int msg)
        {
            Trace.Write(msg.ToString());
        }

        internal void WaitTime(int i)
        {
            Trace.Write(i);
        }
    }

    public class LogerTraceListener : TraceListener
    {
        private readonly Stack<string> LocalInfoC = new Stack<string>();
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
                        Console.Write($"倒计时:{0}秒");
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
                    else if (item.Item1 == Console.WindowWidth / 2)
                    {
                        Console.SetCursorPosition(item.Item1, item.Item2);
                        Console.Write("┆");
                    }
                }
        }

        private void Check()
        {
            if (Console.WindowHeight != WindowHeight || Console.WindowWidth != WindowWidth)
            {
                try
                {
                    Init();
                    Draw();
                }
                catch (Exception e)
                {
                    Check();
                }

                WindowHeight = Console.WindowHeight;
                WindowWidth = Console.WindowWidth;
            }

            Console.SetCursorPosition(Console.WindowWidth / 4 + 2, 1);
            Console.Write($"内存使用量:{Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB");
        }

        private void Draw()
        {
            var Top = 5;
            var CWidth = Console.WindowWidth / 2;
            foreach (var VARIABLE in LocalInfoC.ToArray())
            {
                foreach (var VARIABLE2 in StringSplit(VARIABLE))
                {
                    if (string.IsNullOrEmpty(VARIABLE2)) continue;
                    for (var i = 2; i < CWidth; i++)
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

                List<string> StringSplit(string s)
                {
                    var temp = new List<string>();
                    var StringSize = Encoding.Default.GetByteCount(s);
                    var WindowsSize = CWidth - 2;
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
            }
        }

        public override void Write(object message)
        {
            Check();
            Console.SetCursorPosition(Console.WindowWidth / 2 + 2, 1);
            Console.Write($"倒计时:{message}秒");
        }

        public override void Write(string message)
        {
            Check();
            Console.SetCursorPosition(1, 1);
            Console.Write($"当前下载页面:{message}");
        }

        public override void WriteLine(string message)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd mm:ss}->{message}");
        }

        public override void WriteLine(string message, string category)
        {
            Check();
            LocalInfoC.Push($"{DateTime.Now:yyyy-MM-dd mm:ss}->[{category}]{message}");
            Draw();
        }
    }
}