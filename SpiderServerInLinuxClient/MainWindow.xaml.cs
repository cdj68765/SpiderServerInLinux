using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;

namespace SpiderServerInLinuxClient
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public BaseCommand ChangeFrameSourceCommand
        {
            get
            {
                return new BaseCommand(ChangeFrameSourceCommand_Executed);
            }
        }

        public void ChangeFrameSourceCommand_Executed(object para)
        {
            materialHamburger.IsChecked = false;

            switch (para)
            {
                case "MainPage":
                    materialFrame.Source = new Uri("/PageMain.xaml", UriKind.Relative); break;
                case "141javPage":
                    materialFrame.Source = new Uri("/JavPage.xaml", UriKind.Relative); break;
            }
        }

        public class BaseCommand : ICommand
        {
            public Action<object> ExecuteAction;

            public BaseCommand(Action<object> action)
            {
                ExecuteAction = action;
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged;

            public void Execute(object parameter)
            {
                ExecuteAction(parameter);
            }
        }

        public MainWindow()
        {
            Licenser.LicenseKey = "WTK35-CMGPF-7AGTY-0BZA";

            InitializeComponent();

            DataContext = this;
            /*   Task.Factory.StartNew(() =>
               {
                   materialFrame.Source = new Uri("/PageMain.xaml", UriKind.Relative);
               }, new System.Threading.CancellationToken(), TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());*/
        }
    }
}