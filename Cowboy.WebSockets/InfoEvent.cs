using System;
using System.Collections.Generic;
using System.Text;

namespace Cowboy.WebSockets
{
    public class InfoEvent
    {
        public static InfoEvent CowbotEvent = new InfoEvent();
        //定义事件委托
        //public delegate void ChangedEventHandler(object sender, EventArgs e);

        //定义一个委托类型事件
        public event System.ComponentModel.PropertyChangedEventHandler MessageChanged;

        //用于触发Changed事件
        protected virtual void OnChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.MessageChanged?.Invoke(this, e);
        }

        private string _Message = string.Empty;

        public string Message
        {
            get { return this._Message; }
            set
            {
                this._Message = value.Replace("\r", "").Replace("\n", "");
                this.OnChanged(new System.ComponentModel.PropertyChangedEventArgs("Message"));
            }
        }
    }
}