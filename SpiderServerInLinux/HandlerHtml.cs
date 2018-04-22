using CloudFlareUtilities;
using System.Net.Http;
using System.Threading.Tasks;
using static SpiderServerInLinux.Setting;

namespace SpiderServerInLinux
{
    internal class HandlerHtml
    {
        public HandlerHtml()
        {
            Task.Factory.StartNew(async ()=> 
            {
                HttpClient client = new HttpClient(new ClearanceHandler());
               var content = await client.GetStringAsync($"{setting.Adress}?p={setting.LastPage}");
                System.Console.WriteLine(content);
            },TaskCreationOptions.LongRunning);
        }
    }
}