using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static SpiderServerInLinux.Setting;

namespace SpiderServerInLinux
{
    internal class GetHtml
    {
        public GetHtml()
        {

            var s = "https://sukebei.nyaa.si";
            var c1 = new WebClientEx.WebClientEx();
            var sss = c1.DownloadString(s);
            Console.WriteLine(sss);
        }
    }
}