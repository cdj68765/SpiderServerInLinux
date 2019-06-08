using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;

namespace SpiderServerInLinuxClient
{
    public static class ShowInUI
    {
        public static void ToastButton(string info, bool Error = false)
        {
            var materialToast = new MaterialToast()
            {
                Location = MaterialToastLocationEnum.BottomLeft,
                SlidingDirection = Orientation.Vertical,
                Content = info,
                DisplayTime = TimeSpan.FromMilliseconds(2500),
                HideOnClick = true,
                IsCloseButtonVisible = false,
                CornerRadius = new System.Windows.CornerRadius(10),
            }; materialToast.MaterialAccentBrush = Error ? (Brush)new BrushConverter().ConvertFromString("Tomato") : (Brush)new BrushConverter().ConvertFromString("Aqua");
            materialToast.ShowToast();
        }
    }
}