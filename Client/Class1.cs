using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    internal class Class1
    {
        internal static Form1 MainForm;
        internal static OnlineOpera OnlineOpera;
        internal static int _OperaCount = 0;

        internal static int OperaCount
        {
            get { return _OperaCount; }
            set
            {
                _OperaCount = value; MainForm.Invoke(new MethodInvoker(() => { MainForm.T66yOther.Text = _OperaCount.ToString(); }));
            }
        }
    }
}