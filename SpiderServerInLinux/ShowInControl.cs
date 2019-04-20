using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpiderServerInLinux
{
    public class ShowInControl
    {
        public readonly BlockingCollection<Task> Mission = new BlockingCollection<Task>();
        private int WindowHeight;
        private int WindowWidth;
        public ShowInControl()
        {
            Mission.Add(ReFlush());
            WindowHeight = Console.WindowHeight;
            WindowWidth = Console.WindowWidth;
            Task.Factory.StartNew( () =>
            {
                foreach (var item in Mission.GetConsumingEnumerable())
                {
                    item.ContinueWith(obj=> { Check(); });
                   
                }
            });
            while (true)
            {
                Mission.Add(ReFlush());
            }
        }
        bool _ReFlush = false;
        private async Task ReFlush()
        {
            await  Task.Factory.StartNew(() =>
            {
                try
                {
                    if(_ReFlush)
                    return;
                    _ReFlush = true;
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
                    Console.SetCursorPosition(0, 0);
                }
                catch (Exception)
                {
                    Console.WriteLine();
                }
                _ReFlush = false;
            });
            
        }
        private void Check()
        {
            if (Console.WindowHeight != WindowHeight || Console.WindowWidth != WindowWidth)
            {
                Mission.Add(ReFlush());
                WindowHeight = Console.WindowHeight;
                WindowWidth = Console.WindowWidth;
            }
            Console.SetCursorPosition(Console.WindowWidth / 2 + Console.WindowWidth / 4 + 2, 1);
            Console.Write($"内存使用量:{Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024}MB");
        }
    }
}
