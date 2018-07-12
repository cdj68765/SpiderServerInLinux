using System;
using System.Threading.Tasks;

namespace SpiderServerInLinux
{
    internal  class DownLoop
    {
        private readonly DownloadHelp DoF = new DownloadHelp();

        internal  async Task  DownLoopAsync()
        {
          await  DoF.DownloadPageLoop(Setting.LastPageIndex);
            string Date = string.Empty;
            foreach (var Item in DoF.DownloadCollect.GetConsumingEnumerable())
            {
                Loger.Instance.PageInfo(Item.Item1);

            }
        }
        static async void AsyncMethod()
        {

            var result = await MyMethod();

        }

        static async Task<int> MyMethod()
        {
            for (int i = 0; i < 5; i++)
            {
               
                await Task.Delay(1000);
            }
            return 0;
        }
    }
}