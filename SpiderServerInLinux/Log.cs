using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            Trace.WriteLine(msg, "Debug");
        }

        public void Warn(object msg)
        {
            Trace.WriteLine(msg, "Warn");
        }

        public void Info(object msg)
        {
            Trace.WriteLine(msg, "Info");
        }

        public void Error(object msg)
        {
            Trace.WriteLine(msg, "Error");
        }
    }
    public class LogerTraceListener : TraceListener
    {
        /// <summary>
        /// FileName
        /// </summary>
        private string m_fileName;

        /// <summary>
        /// Constructor
        /// </summary>
        public LogerTraceListener()
        {
          var Col=new  System.Collections.ObjectModel.ObservableCollection<string>();
            
            string basePath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\";
            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);
            this.m_fileName = basePath +
                string.Format("Log-{0}.txt", DateTime.Now.ToString("yyyyMMdd"));
        }

        /// <summary>
        /// Write
        /// </summary>
        public override void Write(string message)
        {
            message = Format(message, "");
            File.AppendAllText(m_fileName, message);
        }

        /// <summary>
        /// Write
        /// </summary>
        public override void Write(object obj)
        {
            string message = Format(obj, "");
            File.AppendAllText(m_fileName, message);
        }

        /// <summary>
        /// WriteLine
        /// </summary>
        public override void WriteLine(object obj)
        {
            string message = Format(obj, "");
            File.AppendAllText(m_fileName, message);
        }

        /// <summary>
        /// WriteLine
        /// </summary>
        public override void WriteLine(string message)
        {
            message = Format(message, "");
            File.AppendAllText(m_fileName, message);
        }

        /// <summary>
        /// WriteLine
        /// </summary>
        public override void WriteLine(object obj, string category)
        {
            string message = Format(obj, category);
            File.AppendAllText(m_fileName, message);
        }

        /// <summary>
        /// WriteLine
        /// </summary>
        public override void WriteLine(string message, string category)
        {
            message = Format(message, category);
            File.AppendAllText(m_fileName, message);
        }

        /// <summary>
        /// Format
        /// </summary>
        private string Format(object obj, string category)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0} ", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            if (!string.IsNullOrEmpty(category))
                builder.AppendFormat("[{0}] ", category);
            if (obj is Exception)
            {
                var ex = (Exception)obj;
                builder.Append(ex.Message + "\r\n");
                builder.Append(ex.StackTrace + "\r\n");
            }
            else
            {
                builder.Append(obj.ToString() + "\r\n");
            }

            return builder.ToString();
        }
    }
}
