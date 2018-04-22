using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SpiderServerInLinux
{
   internal class GlobalSet
    {
        
        public int _id { get; set; }
        public string Item { get; set; }
        public string Value { get; set; }

    }
    internal class Setting
    {
        internal static readonly CancellationTokenSource CancelSign;
        internal static Setting setting;
        internal string Adress;
        internal int LastPage;

    }
}
