﻿using Shadowsocks.Controller;
using Shadowsocks.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Shadowsocks.Util.SystemProxy
{
    public static class Sysproxy
    {
        private const string _userWininetConfigFile = "user-wininet.json";

        private readonly static string[] _lanIP = {
            "<local>",
            "localhost",
            "127.*",
            "10.*",
            "172.16.*",
            "172.17.*",
            "172.18.*",
            "172.19.*",
            "172.20.*",
            "172.21.*",
            "172.22.*",
            "172.23.*",
            "172.24.*",
            "172.25.*",
            "172.26.*",
            "172.27.*",
            "172.28.*",
            "172.29.*",
            "172.30.*",
            "172.31.*",
            "192.168.*"
            };

        private static string _queryStr;

        private enum RET_ERRORS : int
        {
            RET_NO_ERROR = 0,
            INVALID_FORMAT = 1,
            NO_PERMISSION = 2,
            SYSCALL_FAILED = 3,
            NO_MEMORY = 4,
            INVAILD_OPTION_COUNT = 5,
        };

        static Sysproxy()
        {
        }

        private static void ExecSysproxy(string arguments)
        {
            // using event to avoid hanging when redirect standard output/error
            // ref: https://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
            // and http://blog.csdn.net/zhangweixing0/article/details/7356841
            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {
                using (var process = new Process())
                {
                    // Configure the process using the StartInfo properties.
                    process.StartInfo.FileName = Utils.GetTempPath("sysproxy.exe");
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.WorkingDirectory = Utils.GetTempPath();
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.RedirectStandardOutput = true;

                    // Need to provide encoding info, or output/error strings we got will be wrong.
                    process.StartInfo.StandardOutputEncoding = Encoding.Unicode;
                    process.StartInfo.StandardErrorEncoding = Encoding.Unicode;

                    process.StartInfo.CreateNoWindow = true;

                    StringBuilder output = new StringBuilder();
                    StringBuilder error = new StringBuilder();

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            outputWaitHandle.Set();
                        }
                        else
                        {
                            output.AppendLine(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            errorWaitHandle.Set();
                        }
                        else
                        {
                            error.AppendLine(e.Data);
                        }
                    };
                    try
                    {
                        process.Start();

                        process.BeginErrorReadLine();
                        process.BeginOutputReadLine();

                        process.WaitForExit();
                    }
                    catch (System.ComponentModel.Win32Exception e)
                    {
                        // log the arguments
                        throw new ProxyException(ProxyExceptionType.FailToRun, process.StartInfo.Arguments, e);
                    }
                    var stderr = error.ToString();
                    var stdout = output.ToString();

                    var exitCode = process.ExitCode;
                    if (exitCode != (int)RET_ERRORS.RET_NO_ERROR)
                    {
                        throw new ProxyException(ProxyExceptionType.SysproxyExitError, stderr);
                    }

                    if (arguments == "query")
                    {
                        if (string.IsNullOrWhiteSpace(stdout))
                        {
                            // we cannot get user settings
                            throw new ProxyException(ProxyExceptionType.QueryReturnEmpty);
                        }
                        _queryStr = stdout;
                    }
                }
            }
        }

        private static void Save()
        {
        }

        private static void Read()
        {
        }

        private static void ParseQueryStr(string str)
        {
            string[] userSettingsArr = str.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            // sometimes sysproxy output in utf16le instead of ascii manually translate it
            if (userSettingsArr.Length != 4)
            {
                byte[] strByte = Encoding.ASCII.GetBytes(str);
                str = Encoding.Unicode.GetString(strByte);
                userSettingsArr = str.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                // still fail, throw exception with string hexdump
                if (userSettingsArr.Length != 4)
                {
                    throw new ProxyException(ProxyExceptionType.QueryReturnMalformed, BitConverter.ToString(strByte));
                }
            }
        }
    }
}