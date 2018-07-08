using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace SpiderServerInLinux
{
    interface ILoger
    {
        void Warn(object msg);
        void Info(object msg);
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

        public void Info(object msg)
        {

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Trace.WriteLine(msg, "信息");
        }
        public void WithTimeStart(object msg, Stopwatch Time)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Time.Start();
            Trace.WriteLine(msg, "信息");
        }
        public void WithTimeRestart(object msg, Stopwatch Time)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Trace.WriteLine($"{msg},用时:{Time.ElapsedMilliseconds}毫秒", "信息");
            Time.Restart();
        }
        public void WithTimeStop(object msg, Stopwatch Time)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Trace.WriteLine(msg, "信息");
            Time.Stop();
        }
        public void Error(object msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Trace.WriteLine(msg, "错误");
        }
    }
    public class LogerTraceListener : TraceListener
    {

        /// <summary>
        /// Write
        /// </summary>
        public override void Write(string message)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd mm:ss}->{message}");
        }

        /// <summary>
        /// Write
        /// </summary>
        public override void Write(object obj)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd mm:ss}->{obj}");
        }

        /// <summary>
        /// WriteLine
        /// </summary>
        public override void WriteLine(object obj)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd mm:ss}->{obj}");
        }

        /// <summary>
        /// WriteLine
        /// </summary>
        public override void WriteLine(string message)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd mm:ss}->{message}");
        }

        /// <summary>
        /// WriteLine
        /// </summary>
        public override void WriteLine(object obj, string category)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd mm:ss}->[{category}]{obj}");
        }

        /// <summary>
        /// WriteLine
        /// </summary>
        public override void WriteLine(string message, string category)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd mm:ss}->[{category}]{message}");
        }
     
    }
}
