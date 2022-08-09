using Downloader;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace quantum.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Container : UiWindow
    {
        public string appFolder =
            Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "quantum");

        public Container()
        {
            InitializeComponent();
            refreshList();
            AddTaskDir.Items.Add(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            AddTaskDir.Items.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            //Wpf.Ui.Appearance.Background.Apply(this, Wpf.Ui.Appearance.BackgroundType.Mica);
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            if (!File.Exists(Path.Join(appFolder, "settings")))
            {
                File.Create(Path.Join(appFolder, "settings")).Close();
                ToggleOpenFileFolder.IsChecked = true;
                ToggleDeleteTask.IsChecked = true;
                ChunkNumberBox.Value = 16;
                TogglePlugins.IsChecked = false;
                writeSettings();
            }
            else
            {
                readSettings();
            }

            DirectoryInfo directoryInfo = new DirectoryInfo(appFolder);
            FileSystemInfo[] fileSystemInfos = directoryInfo.GetFileSystemInfos("*.qtask");
            foreach (FileSystemInfo fileSystemInfo in fileSystemInfos)
            {
                if (fileSystemInfo is FileInfo)
                {
                    string[] strTaskInfo = File.ReadAllLines(fileSystemInfo.FullName);
                    if (strTaskInfo.Length == 2)
                    {
                        try
                        {
                            TaskInfo taskInfo = new TaskInfo();
                            taskInfo.Url = strTaskInfo[0];
                            taskInfo.Dir = strTaskInfo[1];
                            QuantumDownload quantumDownload = new QuantumDownload();
                            quantumDownload.shouldDeleteTaak = (bool)ToggleDeleteTask.IsChecked;
                            quantumDownload.shouldOpenFileFolder = (bool)ToggleOpenFileFolder.IsChecked;
                            quantumDownload.taskInfo = taskInfo;
                            quantumDownload.taskInfo.TaskFile = fileSystemInfo.FullName;
                            quantumDownload.Init();
                            resumeTask(quantumDownload);
                            quantumDownload.isDownloading = true;
                            downloadTasks.Add(quantumDownload);
                            addList(taskInfo.Url);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            new Thread(() =>
            {
                for (;;)
                {
                    double totalProgress = 0;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var downloadTask in downloadTasks)
                        {
                            int downloadIndex = 0;
                            Application.Current.Dispatcher.Invoke(() =>
                                downloadIndex = downloadTasks.IndexOf(downloadTask));
                            UIElement element = DownloadList.Children[downloadIndex];
                            if (element is CardExpander)
                            {
                                TextBlock textBlock = new TextBlock();
                                textBlock.Text = downloadTask.taskInfo.Completed + "/" + downloadTask.taskInfo.Size +
                                                 " " + downloadTask.taskInfo.Speed + " " +
                                                 downloadTask.taskInfo.TimeLeft;
                                ((CardExpander)element).ToolTip = textBlock;
                                if (((CardExpander)element).Content is ProgressBar)
                                {
                                    ((ProgressBar)((CardExpander)element).Content).Value =
                                        downloadTask.taskInfo.Percentage;
                                }
                            }
                        }

                        foreach (var download in DownloadList.Children)
                        {
                            if (download is CardExpander)
                            {
                                if (((CardExpander)download).Content is ProgressBar)
                                {
                                    totalProgress += ((ProgressBar)((CardExpander)download).Content).Value;
                                }
                            }
                        }

                        if (DownloadList.Children.Count > 0)
                        {
                            totalProgress /= DownloadList.Children.Count;
                            TotalProgress.Value = totalProgress;
                        }
                    });
                    Application.Current.Dispatcher.Invoke(() => App.notifyIcon.ToolTipText = "quantum\nTotal Progress " + TotalProgress.Value.ToString() + "%");
                    Thread.Sleep(50);
                }
            }).Start();
        }

        public List<QuantumDownload> downloadTasks = new List<QuantumDownload>();
        public int currentDeletingIndex = 0;

        public class TaskInfo
        {
            public string Url { get; set; }
            public string File { get; set; }
            public string Size { get; set; }
            public string Completed { get; set; }
            public double Percentage { get; set; }
            public string Speed { get; set; }
            public string TimeLeft { get; set; }
            public string TaskFile { get; set; }
            public string Dir { get; set; }
            public int ChunkCount { get; set; }
            public string UserAgent { get; set; }
            public DownloadService Download { get; set; }
        }

        public static string CalcMemoryMensurableUnit(double bytes)
        {
            double kb = bytes / 1024;
            double mb = kb / 1024;
            double gb = mb / 1024;
            double tb = gb / 1024;

            string result =
                tb > 1 ? $"{tb:0.##}TB" :
                gb > 1 ? $"{gb:0.##}GB" :
                mb > 1 ? $"{mb:0.##}MB" :
                kb > 1 ? $"{kb:0.##}KB" :
                $"{bytes:0.##}B";

            result = result.Replace("/", ".");
            return result;
        }

        public class QuantumDownload
        {
            public TaskInfo taskInfo { get; set; }
            public bool isDownloading = false;
            public bool shouldDeleteTaak = false;
            public bool shouldOpenFileFolder = false;

            public void Init()
            {
                var downloadOpt = new DownloadConfiguration()
                {
                    ParallelDownload = true
                };
                taskInfo.Download = new DownloadService(downloadOpt);
                taskInfo.Download.DownloadStarted += OnDownloadStarted;
                taskInfo.Download.DownloadProgressChanged += OnDownloadProgressChanged;
                taskInfo.Download.DownloadFileCompleted += OnDownloadFileCompleted;
            }

            public async void startDownload()
            {
                var downloadOpt = new DownloadConfiguration()
                {
                    ChunkCount = taskInfo.ChunkCount,
                    ParallelDownload = true,
                    RequestConfiguration = 
                    {
                        UserAgent = taskInfo.UserAgent
                    }
                };
                taskInfo.Download = new DownloadService(downloadOpt);
                taskInfo.Download.DownloadStarted += OnDownloadStarted;
                taskInfo.Download.DownloadProgressChanged += OnDownloadProgressChanged;
                taskInfo.Download.DownloadFileCompleted += OnDownloadFileCompleted;

                DirectoryInfo path = new DirectoryInfo(taskInfo.Dir);
                await taskInfo.Download.DownloadFileTaskAsync(taskInfo.Url, path);
            }

            public void Stop()
            {
                taskInfo.Download.CancelAsync();
                isDownloading = false;
            }

            public void Resume(DownloadPackage package)
            {
                taskInfo.Download.DownloadFileTaskAsync(package);
                isDownloading = true;
            }

            private void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
            {
                isDownloading = false;
                try
                {
                    if (taskInfo.Percentage >= 100)
                    {
                        if (shouldDeleteTaak)
                        {
                            File.Delete(taskInfo.TaskFile);
                        }
                        if (shouldOpenFileFolder)
                        {
                            Process.Start("explorer.exe", taskInfo.Dir);
                        }
                        App.notifyIcon.ShowBalloonTip("quantum", "Download Complete!", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                    }
                }
                catch
                {
                }
            }

            private void OnDownloadProgressChanged(object sender, Downloader.DownloadProgressChangedEventArgs e)
            {
                double nonZeroSpeed = e.BytesPerSecondSpeed + 0.0001;
                int estimateTime = (int)((e.TotalBytesToReceive - e.ReceivedBytesSize) / nonZeroSpeed);
                bool isMinutes = estimateTime >= 60;
                string timeLeftUnit = "seconds";

                if (isMinutes)
                {
                    timeLeftUnit = "minutes";
                    estimateTime /= 60;
                }

                if (estimateTime < 0)
                {
                    estimateTime = 0;
                    timeLeftUnit = "unknown";
                }

                string speed = CalcMemoryMensurableUnit(e.BytesPerSecondSpeed);
                string bytesReceived = CalcMemoryMensurableUnit(e.ReceivedBytesSize);
                string totalBytesToReceive = CalcMemoryMensurableUnit(e.TotalBytesToReceive);
                taskInfo.Percentage = e.ProgressPercentage;
                taskInfo.Speed = $"{speed}/s";
                taskInfo.TimeLeft = $"{estimateTime} {timeLeftUnit} left";
                taskInfo.Completed = bytesReceived;
                taskInfo.Size = totalBytesToReceive;
            }

            private void OnDownloadStarted(object sender, DownloadStartedEventArgs e)
            {
                taskInfo.File = e.FileName;
                isDownloading = true;
            }
        }

        public void refreshList()
        {
            TotalCount.Text = "Total " + DownloadList.Children.Count +
                              (DownloadList.Children.Count <= 1 ? " Task" : " Tasks");
        }

        public void addList(string fileName)
        {
            ProgressBar progressBar = new ProgressBar();

            CardExpander cardExpander = new CardExpander();

            ColumnDefinition fullColumnDef = new ColumnDefinition();
            fullColumnDef.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition autoColumnDef1 = new ColumnDefinition();
            autoColumnDef1.Width = GridLength.Auto;
            ColumnDefinition autoColumnDef2 = new ColumnDefinition();
            autoColumnDef2.Width = GridLength.Auto;
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(fullColumnDef);
            grid.ColumnDefinitions.Add(autoColumnDef1);
            grid.ColumnDefinitions.Add(autoColumnDef2);

            TextBlock textBlock = new TextBlock();
            textBlock.Text = fileName;
            textBlock.FontSize = 13;
            textBlock.FontWeight = FontWeights.Medium;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            grid.Children.Add(textBlock);
            textBlock.SetValue(Grid.ColumnProperty, 0);

            Wpf.Ui.Controls.Button buttonPause = new Wpf.Ui.Controls.Button();
            buttonPause.Content = "Pause";
            buttonPause.Click += ButtonPause_Click;
            buttonPause.Margin = new Thickness(5, 0, 0, 0);
            grid.Children.Add(buttonPause);
            buttonPause.SetValue(Grid.ColumnProperty, 1);

            Wpf.Ui.Controls.Button buttonDelete = new Wpf.Ui.Controls.Button();
            buttonDelete.Content = "Delete";
            buttonDelete.Click += ButtonDelete_Click;
            buttonPause.Margin = new Thickness(5, 0, 0, 0);
            grid.Children.Add(buttonDelete);
            buttonDelete.SetValue(Grid.ColumnProperty, 2);

            cardExpander.Header = grid;
            cardExpander.Content = progressBar;
            if (fileName.EndsWith(".mp3") || fileName.EndsWith(".mp4") || fileName.EndsWith(".wav"))
            {
                cardExpander.Icon = Wpf.Ui.Common.SymbolRegular.WindowPlay20;
            }
            else if (fileName.EndsWith(".txt") || fileName.EndsWith(".md") || fileName.EndsWith(".doc") ||
                     fileName.EndsWith(".docx"))
            {
                cardExpander.Icon = Wpf.Ui.Common.SymbolRegular.Document20;
            }
            else if (fileName.EndsWith(".exe") || fileName.EndsWith(".apk"))
            {
                cardExpander.Icon = Wpf.Ui.Common.SymbolRegular.Window20;
            }
            else if (fileName.EndsWith(".zip") || fileName.EndsWith(".rar"))
            {
                cardExpander.Icon = Wpf.Ui.Common.SymbolRegular.FolderZip20;
            }
            else
            {
                cardExpander.Icon = Wpf.Ui.Common.SymbolRegular.ArrowDownload20;
            }

            DownloadList.Children.Add(cardExpander);
            refreshList();
        }

        public void deleteTask(int index)
        {
            downloadTasks[index].Stop();
            File.Delete(downloadTasks[index].taskInfo.TaskFile);
            downloadTasks.RemoveAt(index);
            DownloadList.Children.RemoveAt(index);
            refreshList();
        }

        public void saveTask(QuantumDownload downloadTask)
        {
            try
            {
                if (downloadTask.isDownloading)
                {
                    downloadTask.Stop();
                    IFormatter formatter = new BinaryFormatter();
                    Stream serializedStream =
                        new FileStream(downloadTask.taskInfo.TaskFile + ".qdl", FileMode.OpenOrCreate);
                    formatter.Serialize(serializedStream, downloadTask.taskInfo.Download.Package);
                    serializedStream.Close();
                }
            }
            catch
            {
            }
        }

        public void resumeTask(QuantumDownload downloadTask)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream serializedStream =
                new FileStream(downloadTask.taskInfo.TaskFile + ".qdl", FileMode.OpenOrCreate);
            DownloadPackage package = formatter.Deserialize(serializedStream) as DownloadPackage;
            downloadTask.Resume(package);
            serializedStream.Close();
        }

        public void saveAllTasks()
        {
            foreach (QuantumDownload downloadTask in downloadTasks)
            {
                saveTask(downloadTask);
            }
        }

        public void resumeAllTasks()
        {
            foreach (QuantumDownload downloadTask in downloadTasks)
            {
                resumeTask(downloadTask);
            }
        }

        public void readSettings()
        {
            string settingsPath = Path.Join(appFolder, "settings");
            string[] values = File.ReadAllLines(settingsPath);
            ToggleOpenFileFolder.IsChecked = values[0] == "true";
            ToggleDeleteTask.IsChecked = values[1] == "true";
            ChunkNumberBox.Value = Convert.ToInt32(values[2]);
            UserAgentBox.Text = values[3];
            TogglePlugins.IsChecked = values[4] == "true";
        }

        public void writeSettings()
        {
            string settingsPath = Path.Join(appFolder, "settings");
            List<string> values = new List<string>();
            values.Add((bool)ToggleOpenFileFolder.IsChecked ? "true" : "false");
            values.Add((bool)ToggleDeleteTask.IsChecked ? "true" : "false");
            values.Add(ChunkNumberBox.Value.ToString());
            values.Add(UserAgentBox.Text);
            values.Add((bool)TogglePlugins.IsChecked ? "true" : "false");
            File.WriteAllLines(settingsPath, values);
        }

        public List<string> getPluginsList()
        {
            List<string> pluginsList = new List<string>();
            DirectoryInfo directoryInfo = new DirectoryInfo(Path.Join(appFolder, "plugins"));
            FileSystemInfo[] fileSystemInfos = directoryInfo.GetFileSystemInfos("*.dll");
            foreach (FileSystemInfo fileSystemInfo in fileSystemInfos)
            {
                if (fileSystemInfo is FileInfo)
                {
                    pluginsList.Add(fileSystemInfo.FullName);
                }
            }
            return pluginsList;
        }

        public bool shouldPluginConvertUrl(string pluginPath, string Url)
        {
            Assembly assembly = Assembly.LoadFrom(pluginPath);
            Type type = assembly.GetType("QuantumPlugin.Main");
            object obj = Activator.CreateInstance(type);
            MethodInfo shouldConvertUrl = type.GetMethod("shouldConvertUrl");
            return (bool)shouldConvertUrl.Invoke(obj, new object[] {Url});
        }

        public string pluginConvertUrl(string pluginPath, string Url)
        {
            Assembly assembly = Assembly.LoadFrom(pluginPath);
            Type type = assembly.GetType("QuantumPlugin.Main");
            object obj = Activator.CreateInstance(type);
            MethodInfo convertUrl = type.GetMethod("convertUrl");
            return (string)convertUrl.Invoke(obj, new object[] { Url });
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            AddTaskLink.Text = "";
            AddTaskDialog.Show();
        }

        private void PauseAll_Click(object sender, RoutedEventArgs e)
        {
            saveAllTasks();
            foreach (var download in DownloadList.Children)
            {
                if (download is CardExpander)
                {
                    if (((CardExpander)download).Header is Grid)
                    {
                        if (((Grid)((CardExpander)download).Header).Children[1] is Wpf.Ui.Controls.Button)
                        {
                            ((Wpf.Ui.Controls.Button)((Grid)((CardExpander)download).Header).Children[1]).Content =
                                "Resume";
                        }
                    }
                }
            }
        }

        private void StartAll_Click(object sender, RoutedEventArgs e)
        {
            resumeAllTasks();
            foreach (var download in DownloadList.Children)
            {
                if (download is CardExpander)
                {
                    if (((CardExpander)download).Header is Grid)
                    {
                        if (((Grid)((CardExpander)download).Header).Children[1] is Wpf.Ui.Controls.Button)
                        {
                            ((Wpf.Ui.Controls.Button)((Grid)((CardExpander)download).Header).Children[1]).Content =
                                "Pause";
                        }
                    }
                }
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsDialog.Show();
        }

        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            currentDeletingIndex =
                DownloadList.Children.IndexOf((CardExpander)((Grid)((Wpf.Ui.Controls.Button)sender).Parent).Parent);
            ConfirmDeleteDialog.Show();
        }

        private void ButtonPause_Click(object sender, RoutedEventArgs e)
        {
            QuantumDownload downloadTask =
                downloadTasks[
                    DownloadList.Children.IndexOf((CardExpander)((Grid)((Wpf.Ui.Controls.Button)sender).Parent)
                        .Parent)];
            if (downloadTask.isDownloading)
            {
                saveTask(downloadTask);
                ((Wpf.Ui.Controls.Button)sender).Content = "Resume";
            }
            else
            {
                resumeTask(downloadTask);
                ((Wpf.Ui.Controls.Button)sender).Content = "Pause";
            }
        }

        private void AddTaskAdd(object sender, RoutedEventArgs e)
        {
            if (AddTaskLink.Text != "")
            {
                addList(AddTaskLink.Text);
                AddTaskDialog.Hide();
                TaskInfo taskInfo = new TaskInfo();
                if (AddTaskLink.Text.StartsWith("https://") || AddTaskLink.Text.StartsWith("http://"))
                {
                    taskInfo.Url = AddTaskLink.Text;
                }
                else
                {
                    taskInfo.Url = "http://" + AddTaskLink.Text;
                }
                if ((bool)TogglePlugins.IsChecked)
                {
                    foreach (string pluginPath in getPluginsList())
                    {
                        if (shouldPluginConvertUrl(pluginPath, taskInfo.Url))
                        {
                            taskInfo.Url = pluginConvertUrl(pluginPath, taskInfo.Url);
                        }
                    }
                }
                taskInfo.UserAgent = UserAgentBox.Text;

                taskInfo.Dir = AddTaskDir.Text;
                taskInfo.ChunkCount = (int)ChunkNumberBox.Value;
                QuantumDownload quantumDownload = new QuantumDownload();
                quantumDownload.shouldDeleteTaak = (bool)ToggleDeleteTask.IsChecked;
                quantumDownload.shouldOpenFileFolder = (bool)ToggleOpenFileFolder.IsChecked;
                quantumDownload.taskInfo = taskInfo;
                quantumDownload.startDownload();
                quantumDownload.taskInfo.TaskFile = Path.Join(appFolder, Guid.NewGuid().ToString() + ".qtask");
                File.WriteAllLines(quantumDownload.taskInfo.TaskFile,
                    new string[] { quantumDownload.taskInfo.Url, quantumDownload.taskInfo.Dir });
                downloadTasks.Add(quantumDownload);
            }
        }

        private void AddTaskCancel(object sender, RoutedEventArgs e)
        {
            AddTaskDialog.Hide();
        }

        private void SettingsConfirm(object sender, RoutedEventArgs e)
        {
            writeSettings();
            SettingsDialog.Hide();
        }

        private void SettingsCancel(object sender, RoutedEventArgs e)
        {
            readSettings();
            SettingsDialog.Hide();
        }

        private void ConfirmDelete(object sender, RoutedEventArgs e)
        {
            deleteTask(currentDeletingIndex);
            ConfirmDeleteDialog.Hide();
        }

        private void CancelDelete(object sender, RoutedEventArgs e)
        {
            ConfirmDeleteDialog.Hide();
        }

        private void UiWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = Visibility == Visibility.Visible;
            if (!e.Cancel)
            {
                saveAllTasks();
                Environment.Exit(0);
            }
            else
            {
                Visibility = Visibility.Hidden;
            }
        }
    }
}