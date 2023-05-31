using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Windows;

namespace Quantum
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static TaskbarIcon notifyIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            notifyIcon.Dispose();
            base.OnExit(e);
        }

        private void ShowWindow_Click(object sender, RoutedEventArgs e)
        {
            Windows[0].Show();
            Windows[0].Focus();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Windows[0].Hide();
            Windows[0].Close();
        }
    }
}