﻿using Downloader;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Quantum;
using Quantum.Core.Data;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using Clipboard = System.Windows.Clipboard;

namespace Quantum
{
    public class LanguageManager
    {
        public static List<string> GetAllLang = new List<string>{"English.xaml", "中文.xaml"};
        public static int CurrentLang = 0;
        public static void UseLang(string langFile)
        {
            ResourceDictionary ResDict = new ResourceDictionary();
            ResDict.Source = new Uri(@"Resources\Language\" + langFile, UriKind.Relative);
            Application.Current.Resources.MergedDictionaries[2] = ResDict;
        }
    }
    
    public partial class MainWindow : UiWindow
    {
        public string appFolder =
            Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "quantum");

        public List<Core.Download.QuantumDownload> Tasks = new List<Core.Download.QuantumDownload>();
        public MainWindow()
        {
            InitializeComponent();
            for (int k = 0; k < LanguageManager.GetAllLang.Count; k++)
            {
                LanguageComboBox.Items.Add(LanguageManager.GetAllLang[k]);
            }
            
            AddTaskDir.Items.Add(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            AddTaskDir.Items.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            //Wpf.Ui.Appearance.Background.Apply(this, Wpf.Ui.Appearance.BackgroundType.Mica);
            /*if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            if (!Directory.Exists(Path.Join(appFolder, "plugins")))
            {
                Directory.CreateDirectory(Path.Join(appFolder, "plugins"));
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
            }*/
            
            LoadConfigToUi();

            TaskListBox.ItemsSource = Tasks;

            new Thread(() =>
            {
                for (;;)
                {
                    try
                    {
                        double totalProg = 0;
                        if(Tasks.Count > 0)
                        {
                            foreach (var dl in Tasks)
                            {
                                totalProg += dl.Percentage;
                            }
                            totalProg /= Tasks.Count;
                        }
                        Dispatcher.Invoke(new Action(delegate
                        {
                            TaskListBox.Items.Refresh();
                            TotalProgress.Value = totalProg;
                        }));
                    }catch(Exception){}
                    Thread.Sleep(500);
                }
            }).Start();

            /*DirectoryInfo directoryInfo = new DirectoryInfo(appFolder);
            FileSystemInfo[] fileSystemInfoList = directoryInfo.GetFileSystemInfos("*.qtask");
            foreach (FileSystemInfo fileSystemInfo in fileSystemInfoList)
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
                            if (File.Exists(fileSystemInfo.FullName + ".qdl"))
                            {
                                resumeTask(quantumDownload);
                            }
                            else
                            {
                                quantumDownload.startDownload();
                            }

                            quantumDownload.isDownloading = true;
                            downloadTasks.Add(quantumDownload);
                            addList(taskInfo.Url);
                        }
                        catch
                        {
                        }
                    }
                }
            }*/

            List<string> pluginsList = getPluginsList(true, true);
            foreach (string pluginPath in pluginsList)
            {
                if (File.Exists(pluginPath + ".shouldDelete"))
                {
                    File.Delete(pluginPath);
                    File.Delete(pluginPath + ".shouldDelete");
                    if (File.Exists(pluginPath + ".disabled"))
                    {
                        File.Delete(pluginPath + ".disabled");
                    }
                }
            }

            /*new Thread(() =>
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
                            UIElement element = TaskStackPanel.Children[downloadIndex];
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

                        foreach (var download in TaskStackPanel.Children)
                        {
                            if (download is CardExpander)
                            {
                                if (((CardExpander)download).Content is ProgressBar)
                                {
                                    totalProgress += ((ProgressBar)((CardExpander)download).Content).Value;
                                }
                            }
                        }

                        if (TaskStackPanel.Children.Count > 0)
                        {
                            totalProgress /= TaskStackPanel.Children.Count;
                            TotalProgress.Value = totalProgress;
                        }
                    });
                    Application.Current.Dispatcher.Invoke(() =>
                        App.notifyIcon.ToolTipText = "quantum\nTotal Progress " + TotalProgress.Value.ToString() + "%");
                    Thread.Sleep(50);
                }
            }).Start();

            if (ThemeComboBox.SelectedIndex == 0)
            {
                Wpf.Ui.Appearance.Theme.Apply(Wpf.Ui.Appearance.ThemeType.Light, Wpf.Ui.Appearance.BackgroundType.Mica,
                    true);
            }
            else
            {
                Wpf.Ui.Appearance.Theme.Apply(Wpf.Ui.Appearance.ThemeType.Dark, Wpf.Ui.Appearance.BackgroundType.Mica,
                    true);
            }*/
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

                    App.notifyIcon.ShowBalloonTip("quantum", "Download Complete!",
                        Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
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

                string speed = "u";
                string bytesReceived = "u5";
                string totalBytesToReceive = "vwo50";
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
            buttonDelete.Margin = new Thickness(5, 0, 0, 0);
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

            TaskStackPanel.Children.Add(cardExpander);
            
        }

        public void deleteTask(int index)
        {
            downloadTasks[index].Stop();
            File.Delete(downloadTasks[index].taskInfo.TaskFile);
            downloadTasks.RemoveAt(index);
            TaskStackPanel.Children.RemoveAt(index);
            
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
            try
            {
                IFormatter formatter = new BinaryFormatter();
                Stream serializedStream =
                    new FileStream(downloadTask.taskInfo.TaskFile + ".qdl", FileMode.OpenOrCreate);
                DownloadPackage package = formatter.Deserialize(serializedStream) as DownloadPackage;
                downloadTask.Resume(package);
                serializedStream.Close();
            }
            catch
            {
            }
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
            ThemeComboBox.SelectedIndex = Convert.ToInt32(values[5]);
        }

        public void LoadConfigToUi()
        {
            Data.ReadConfig();
            ChunkNumberBox.Value = Data.CurrentQuantumConfig.Chunks;
            UserAgentBox.Text = Data.CurrentQuantumConfig.UserAgent;
            ThemeComboBox.SelectedIndex = Data.CurrentQuantumConfig.Theme;
        }
        
        public void SaveConfigFromUi()
        {
            Data.CurrentQuantumConfig.Chunks = Convert.ToInt32(ChunkNumberBox.Value);
            Data.CurrentQuantumConfig.UserAgent = UserAgentBox.Text;
            Data.CurrentQuantumConfig.Theme = ThemeComboBox.SelectedIndex;
            Data.WriteConfig();
        }

        /*public void writeSettings()
        {
            string settingsPath = Path.Join(appFolder, "settings");
            List<string> values = new List<string>();
            values.Add((bool)ToggleOpenFileFolder.IsChecked ? "true" : "false");
            values.Add((bool)ToggleDeleteTask.IsChecked ? "true" : "false");
            values.Add(ChunkNumberBox.Value.ToString());
            values.Add(UserAgentBox.Text);
            values.Add((bool)TogglePlugins.IsChecked ? "true" : "false");
            values.Add(ThemeComboBox.SelectedIndex.ToString());
            File.WriteAllLines(settingsPath, values);
        }*/

        public List<string> getPluginsList(bool includeDisabled = false, bool getFullName = true)
        {
            List<string> pluginsList = new List<string>();
            DirectoryInfo directoryInfo = new DirectoryInfo(Path.Join(appFolder, "plugins"));
            FileSystemInfo[] fileSystemInfos = directoryInfo.GetFileSystemInfos("*.dll");
            foreach (FileSystemInfo fileSystemInfo in fileSystemInfos)
            {
                if (fileSystemInfo is FileInfo && fileSystemInfo.Name.StartsWith("plugin") &&
                    !File.Exists(fileSystemInfo.FullName + ".disabled"))
                {
                    if (!File.Exists(fileSystemInfo.FullName + ".shouldDelete") || getFullName)
                    {
                        pluginsList.Add(getFullName ? fileSystemInfo.FullName : fileSystemInfo.Name);
                    }
                }

                if (fileSystemInfo is FileInfo && File.Exists(fileSystemInfo.FullName + ".disabled") && includeDisabled)
                {
                    if (!File.Exists(fileSystemInfo.FullName + ".shouldDelete") || getFullName)
                    {
                        pluginsList.Add(getFullName ? fileSystemInfo.FullName : fileSystemInfo.Name);
                    }
                }
            }

            return pluginsList;
        }

        public void refreshPluginsList()
        {
            List<string> pluginsList = getPluginsList(true, false);
            List<UIElement> uiElements = new List<UIElement>();
            foreach (string pluginPath in pluginsList)
            {
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
                textBlock.Text = pluginPath;
                textBlock.FontSize = 13;
                textBlock.FontWeight = FontWeights.Medium;
                textBlock.VerticalAlignment = VerticalAlignment.Center;
                grid.Children.Add(textBlock);
                textBlock.SetValue(Grid.ColumnProperty, 0);

                Wpf.Ui.Controls.Button buttonEnable = new Wpf.Ui.Controls.Button();
                buttonEnable.Content = File.Exists(Path.Join(Path.Join(appFolder, "plugins"), pluginPath) + ".disabled")
                    ? "Enable"
                    : "Disable";
                buttonEnable.Click += ButtonEnable_Click;
                buttonEnable.Margin = new Thickness(5, 0, 0, 0);
                grid.Children.Add(buttonEnable);
                buttonEnable.SetValue(Grid.ColumnProperty, 1);

                Wpf.Ui.Controls.Button buttonDelete = new Wpf.Ui.Controls.Button();
                buttonDelete.Content = "Delete";
                buttonDelete.Click += ButtonDeletePlugin_Click;
                buttonDelete.Margin = new Thickness(5, 0, 0, 0);
                grid.Children.Add(buttonDelete);
                buttonDelete.SetValue(Grid.ColumnProperty, 2);

                uiElements.Add(grid);
            }

            PluginsListBox.ItemsSource = uiElements;
        }

        public bool shouldPluginConvertUrl(string pluginPath, string Url)
        {
            Assembly assembly = Assembly.LoadFrom(pluginPath);
            Type type = assembly.GetType("QuantumPlugin.Main");
            object obj = Activator.CreateInstance(type);
            MethodInfo shouldConvertUrl = type.GetMethod("shouldConvertUrl");
            return (bool)shouldConvertUrl.Invoke(obj, new object[] { Url });
        }

        public string pluginConvertUrl(string pluginPath, string Url)
        {
            Assembly assembly = Assembly.LoadFrom(pluginPath);
            Type type = assembly.GetType("QuantumPlugin.Main");
            object obj = Activator.CreateInstance(type);
            MethodInfo convertUrl = type.GetMethod("convertUrl");
            return (string)convertUrl.Invoke(obj, new object[] { Url });
        }

        #region Menu Events
        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            AddTaskLink.Text= string.Empty;
            string clipboardText = Clipboard.GetText(TextDataFormat.Text);
            AddTaskLink.Text = clipboardText.StartsWith("http://") || clipboardText.StartsWith("https://")
                ? clipboardText
                : "";
            AddTaskDialog.Show();
        }

        private void OnPauseAllClick(object sender, RoutedEventArgs e)
        {
            saveAllTasks();
            foreach (var download in TaskStackPanel.Children)
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

        private void OnStartAllClick(object sender, RoutedEventArgs e)
        {
            resumeAllTasks();
            foreach (var download in TaskStackPanel.Children)
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

        private void OnDeleteAllClick(object sender, RoutedEventArgs e)
        {
            for(int i = 0;i < Tasks.Count; i++)
            {
                if (Tasks[i].Status == Core.Download.QuantumDownloadStatus.Downloading)
                {
                    Tasks[i].Stop();
                }
            }
            Tasks.Clear();
        }

        private void OnConfigClick(object sender, RoutedEventArgs e)
        {
            SettingsDialog.Show();
        }
        #endregion

        #region Utils
        private Dialog CreateAskDialog(string title, string content)
        {
            Dialog dialog = new Dialog
            {
                Title= title,
                Content= content,
            };
            Grid.SetRow(dialog, 1);
            Grid.SetRowSpan(dialog, 3);
            return dialog;
        }
        #endregion

        #region Dialog Events

        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            currentDeletingIndex =
                TaskStackPanel.Children.IndexOf((CardExpander)((Grid)((Wpf.Ui.Controls.Button)sender).Parent).Parent);
            ConfirmDeleteDialog.Show();
        }

        private void ButtonPause_Click(object sender, RoutedEventArgs e)
        {
            QuantumDownload downloadTask =
                downloadTasks[
                    TaskStackPanel.Children.IndexOf((CardExpander)((Grid)((Wpf.Ui.Controls.Button)sender).Parent)
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

        private void ButtonDeletePlugin_Click(object sender, RoutedEventArgs e)
        {
            string pluginPath = Path.Join(Path.Join(appFolder, "plugins"),
                ((TextBlock)((Grid)((Wpf.Ui.Controls.Button)sender).Parent).Children[0]).Text);
            File.Create(pluginPath + ".shouldDelete").Close();
            refreshPluginsList();
        }

        private void ButtonEnable_Click(object sender, RoutedEventArgs e)
        {
            string pluginPath = Path.Join(Path.Join(appFolder, "plugins"),
                ((TextBlock)((Grid)((Wpf.Ui.Controls.Button)sender).Parent).Children[0]).Text);
            if (File.Exists(pluginPath + ".disabled"))
            {
                File.Delete(pluginPath + ".disabled");
                ((Wpf.Ui.Controls.Button)sender).Content = "Disable";
            }
            else
            {
                File.Create(pluginPath + ".disabled").Close();
                ((Wpf.Ui.Controls.Button)sender).Content = "Enable";
            }
        }

        private void AddTaskAdd(object sender, RoutedEventArgs e)
        {
            if (AddTaskLink.Text != "")
            {
                //addList(AddTaskLink.Text);
                AddTaskDialog.Hide();
                //TaskInfo taskInfo = new TaskInfo();
                Core.Download.QuantumDownload task = new Core.Download.QuantumDownload(AddTaskLink.Text.StartsWith("https://") || AddTaskLink.Text.StartsWith("http://") ? AddTaskLink.Text : "http://" + AddTaskLink.Text, AddTaskDir.Text);

                /*if ((bool)TogglePlugins.IsChecked)
                {
                    foreach (string pluginPath in getPluginsList())
                    {
                        if (shouldPluginConvertUrl(pluginPath, taskInfo.Url))
                        {
                            taskInfo.Url = pluginConvertUrl(pluginPath, taskInfo.Url);
                        }
                    }
                }*/

                task.UserAgent = UserAgentBox.Text;
                task.Chunks = Core.Data.Data.CurrentQuantumConfig.Chunks;
                if (AddTaskLink.Text.EndsWith(".mp3") || AddTaskLink.Text.EndsWith(".mp4") || AddTaskLink.Text.EndsWith(".avi") || AddTaskLink.Text.EndsWith(".webm") || AddTaskLink.Text.EndsWith(".mov"))
                {
                    task.Symbol = SymbolRegular.WindowPlay20;
                }

                //taskInfo.ChunkCount = (int)ChunkNumberBox.Value;
                //QuantumDownload quantumDownload = new QuantumDownload();
                //quantumDownload.shouldDeleteTaak = (bool)ToggleDeleteTask.IsChecked;
                //quantumDownload.shouldOpenFileFolder = (bool)ToggleOpenFileFolder.IsChecked;
                //quantumDownload.taskInfo = taskInfo;
                //quantumDownload.startDownload();
                //quantumDownload.taskInfo.TaskFile = Path.Join(appFolder, Guid.NewGuid().ToString() + ".qtask");
                /*File.WriteAllLines(quantumDownload.taskInfo.TaskFile,
                    new string[] { quantumDownload.taskInfo.Url, quantumDownload.taskInfo.Dir });
                downloadTasks.Add(quantumDownload);*/
                Tasks.Add(task);
                Tasks[Tasks.Count - 1].Start();
            }
        }

        #endregion

        private void AddTaskCancel(object sender, RoutedEventArgs e)
        {
            AddTaskDialog.Hide();
        }

        private void SettingsConfirm(object sender, RoutedEventArgs e)
        {
            //writeSettings();
            SaveConfigFromUi();
            SettingsDialog.Hide();
        }

        private void SettingsCancel(object sender, RoutedEventArgs e)
        {
            //readSettings();
            LoadConfigToUi();
            SettingsDialog.Hide();
        }

        private void OpenPluginsMan_Click(object sender, RoutedEventArgs e)
        {
            refreshPluginsList();
            PluginsManagerDialog.Show();
        }

        private void PluginsManOK(object sender, RoutedEventArgs e)
        {
            PluginsManagerDialog.Hide();
        }

        private void AddPlugin_Click(object sender, RoutedEventArgs e)
        {
            string fileName = "";
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Plugin File(*.dll)|*.dll";
            ofd.ValidateNames = true;
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            if ((bool)ofd.ShowDialog())
            {
                fileName = ofd.FileName;
                if (Path.GetFileName(fileName).StartsWith("plugin"))
                {
                    File.Copy(fileName, Path.Join(Path.Join(appFolder, "plugins"), Path.GetFileName(fileName)));
                }
                else
                {
                    File.Copy(fileName,
                        Path.Join(Path.Join(appFolder, "plugins"), "plugin" + Path.GetFileName(fileName)));
                }

                refreshPluginsList();
            }
        }

        private void OpenPluginsFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", Path.Join(appFolder, "plugins"));
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

        private void onThemeChange(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedIndex == 0)
            {
                Wpf.Ui.Appearance.Theme.Apply(Wpf.Ui.Appearance.ThemeType.Light, Wpf.Ui.Appearance.BackgroundType.Mica,
                    true);
            }
            else if (ThemeComboBox.SelectedIndex == 1)
            {
                Wpf.Ui.Appearance.Theme.Apply(Wpf.Ui.Appearance.ThemeType.Dark, Wpf.Ui.Appearance.BackgroundType.Mica,
                    true);
            }
            else
            {
                Wpf.Ui.Appearance.Watcher.Watch(this, Wpf.Ui.Appearance.BackgroundType.Mica, true);
            }
        }

        private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            LanguageManager.UseLang(LanguageManager.GetAllLang[LanguageComboBox.SelectedIndex]);
        }
    }
}