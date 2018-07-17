using System;
using System.Threading.Tasks;

namespace SpiderServerInLinux
{
    internal class DownLoop
    {
        private readonly DownloadHelp DoF = new DownloadHelp();

        internal void DownLoopAsync()
        {
            //DownNewDayAsync();
            DownAsync();
            var PageHandler = new HandlerHtml();
            Task.Factory.StartNew(() =>
            {
                foreach (var Item in DoF.DownloadCollect.GetConsumingEnumerable())
                {
                    Setting.LastPageIndex = Item.Item1;
                    DataBaseCommand.SavePage(Item.Item2);
                    if (PageHandler.HandlerToHtml(Item.Item2) != 75)
                    {
                        Loger.Instance.LocalInfo("当前获得条目小于75条,检查是否完成获取");
                        DoF.DownloadCollect.CompleteAdding();
                        DoF.CancelSign.Cancel();
                    }
                }
            }, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(() =>
            {
                foreach (var Item in PageHandler.DataCollect.GetConsumingEnumerable())
                {
                    var Status = PageInDateStatus(Item.Item1.Item1);
                    if (Status == -1)
                    {
                        DataBaseCommand.SaveToDataBaseRange(Item.Item2, Item.Item1.Item2, true);
                    }
                    else if (Status == 0)
                    {
                        DataBaseCommand.SaveToDataBaseOneByOne(Item.Item2, Item.Item1.Item2, true);
                    }
                    else
                    {
                        Loger.Instance.LocalInfo($"发现{Item.Item1.Item1}已经存在");
                        DataBaseCommand.SaveToDataBaseRange(Item.Item2, Item.Item1.Item2, true);
                        //return true;
                    }
                }

                int PageInDateStatus(string Date)
                {
                    var Status = DataBaseCommand.GetDateInfo(Date);
                    if (Status == null)
                    {
                        return -1;
                    }

                    if (Status.Status)
                    {
                        return 1;
                    }

                    return 0;

                }
            }, TaskCreationOptions.LongRunning);
        }

        void DownNewDayAsync()
        {
            DownloadHelp NDoF = new DownloadHelp();
            var PageHandler = new HandlerHtml();
            var Timer = new System.Timers.Timer();
            Timer.AutoReset = true;
            Timer.Interval = 10000;
            Timer.Elapsed += delegate { };
            Task.Factory.StartNew(async () => { await NDoF.DownloadPageLoop(0); }, TaskCreationOptions.LongRunning);
            foreach (var Item in NDoF.DownloadCollect.GetConsumingEnumerable())
            {
                Setting.LastPageIndex = Item.Item1;
                PageHandler.HandlerToHtml(Item.Item2);
            }
        }

        async void DownAsync()
        {
            await DoF.DownloadPageLoop(Setting.LastPageIndex);
        }
    }
}