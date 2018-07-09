using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SpiderServerInLinux
{
    interface ILoger
    {
        void Warn(object msg);
        void LocalInfo(object msg);
        void Debug(object msg);
        void Error(object msg);
    }

    public class Loger : ILoger
    {
        /// <summary>
        /// Single Instance
        /// </summary>
        private static Loger instance;

        public static Loger Instance
        {
            get
            {
                if (instance == null)
                    instance = new Loger();
                return instance;
            }

        }

        /// <summary>
        /// Constructor
        /// </summary>
        private Loger()
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new LogerTraceListener());
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

        public void Error(object msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Trace.WriteLine(msg, "错误");
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
        void Check()
        {
            if (Console.WindowHeight != WindowHeight || Console.WindowWidth != WindowWidth)
            {
                Init();
                WindowHeight = Console.WindowHeight;
                WindowWidth = Console.WindowWidth;
            }
        }
        public override void Write(object message)
        {
            Check();
            Console.SetCursorPosition((Console.WindowWidth / 2) + 2, 1);
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
            var PushS = $"{DateTime.Now:yyyy-MM-dd mm:ss}->[{category}]{message}";

                LocalInfoC.Push(PushS);
            
            var List = LocalInfoC.ToArray();
            for (int i = 0; i < List.Length; i++)
            {
                Console.SetCursorPosition(2, 5 + i);
                Console.Write(List[i]);
                if (i > Console.WindowHeight - 8)
                {
                    break;
                }
            }

            if (LocalInfoC.Count > 100)
            {
                LocalInfoC.Clear();
                for (int i = 0; i < Console.WindowHeight - 8; i++)
                {
                    LocalInfoC.Push(List[Console.WindowHeight - 9 - i]);
                }
            }
        }

        private int WindowWidth = 0;
        private int WindowHeight = 0;
        private Stack<string> LocalInfoC = new Stack<string>();

        public static void Init()
        {
            Console.CursorVisible = false;
            Console.Clear();
            Console.CancelKeyPress += delegate { Console.Clear(); };
            foreach (var item in from X in Enumerable.Range(0, Console.WindowWidth)
                from Y in Enumerable.Range(0, Console.WindowHeight)
                select new Tuple<int, int>(X, Y))
            {
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
                        Console.SetCursorPosition((Console.WindowWidth/2)+2, 1);
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

            /*     for (int i = 0; i < Console.WindowWidth; i++)
                 {

                     for (int j = 0; j < Console.WindowHeight; j++)
                     {
                         if (i == 0 && j == 0)
                         {
                             Console.SetCursorPosition(i, j);
                             Console.Write("┏");
                         }
                         else if (i == 0 && j == Console.WindowHeight - 1)
                         {
                             Console.SetCursorPosition(i, j);
                             Console.Write("┗");
                         }
                         else if (i == Console.WindowWidth - 1 && j == 0)
                         {
                             Console.SetCursorPosition(i, j);
                             Console.Write("┓");
                         }
                         else if (i == Console.WindowWidth - 1 && j == Console.WindowHeight - 1)
                         {
                             Console.SetCursorPosition(i, j);
                             Console.Write("┛");
                         }
                         else if (i == 0)
                         {
                             Console.SetCursorPosition(i, j);
                             Console.Write("┣");
                         }else if (i == Console.WindowWidth - 1)
                         {
                             Console.SetCursorPosition(i, j);
                             Console.Write("┫");
                         }
                         else if (j == Console.WindowHeight - 1)
                         {
                             Console.SetCursorPosition(i, j);
                             Console.Write("┻");
                         }
                         else
                         {
                             Console.SetCursorPosition(i, 0);
                             Console.Write("┳");
                         }
                         Thread.Sleep(100);
                     }

                     //Console.CursorTop = i;
                     /*  Console.Write("カウンタ：");
                       if (i <= 3)
                       {
                           Console.ForegroundColor = ConsoleColor.Red;
                       }
                       Console.Write("{0:D2}", i);

                       Console.ForegroundColor = ConsoleColor.Gray;
                       Thread.Sleep(1000);*/
            // }
        }
    }


}
