using CanetisRadar.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CanetisRadar
{
    public class OverlayNotifyIcon
    {
        private NotifyIcon _notifyIcon;
        public OverlayNotifyIcon() {
        }

        public void Init()
        {
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = Resources.OverlayNotifyIcon;
            _notifyIcon.Text = "Overlay";
            _notifyIcon.Visible = true;

            ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
            contextMenuStrip.Items.Add("退出").Click += OnExit_Click;
            _notifyIcon.ContextMenuStrip = contextMenuStrip;
        }

        private void OnExit_Click(object sender, EventArgs e)
        {
            _notifyIcon.Dispose();
            //Application.Exit();
            Environment.Exit(-1);
        }
    }
}