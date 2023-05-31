using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Downloader;
using Microsoft.VisualBasic.Logging;
using Wpf.Ui.Common;

namespace Quantum.Core.Download;

public class DownloadEvent : EventArgs
{
    public string Msg { get; }
    private DownloadEvent(string msg)
    {
        Msg= msg;
    }
    internal static DownloadEvent Create(string msg) => new(msg);
}

public enum QuantumDownloadStatus
{
    Stopped,
    Downloading,
    Completed,
    Error
}

public class QuantumDownload
{
    #region Basic Information
    public string Name { get; set; } = "unknown";
    public string Source { get; set; }
    public string Destination { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    public SymbolRegular Symbol { get; set; } = SymbolRegular.ArrowDownload24;
    #endregion
    #region Download Statuses
    public string Describe { get; set; } = "Finding resource...";
    public QuantumDownloadStatus Status { get; set; } = QuantumDownloadStatus.Stopped;
    public double Percentage { get; set; } = 0;
    public string Speed { get; set; }
    #endregion
    #region Download Options
    public string UserAgent { get; set; }
    public int Chunks { get; set; }
    #endregion
    #region Event
    public delegate void QuantumDownloadEvent<in TArgs>(QuantumDownload dl, TArgs args);
    public event QuantumDownloadEvent<DownloadEvent> OnDownloadError;
    private Dictionary<Type, Action<EventArgs>> _dict;
    internal void InitializeHandlers()
    {
        _dict = new()
        {
            {typeof(DownloadEvent), e => OnDownloadError?.Invoke(this, (DownloadEvent) e)}
        };
    }
    #endregion
    public DownloadService DlService { get; set; }

    public void Init()
    {
        DlService = new DownloadService(new DownloadConfiguration()
        {
            ParallelDownload = true,
            ChunkCount = Chunks,
            RequestConfiguration= new RequestConfiguration()
            {
                UserAgent = UserAgent,
            }
        });
        DlService.DownloadStarted += OnDownloadStarted;
        DlService.DownloadProgressChanged += OnDownloadProgressChanged;
        DlService.DownloadFileCompleted += OnDownloadCompleted;
    }

    private void OnDownloadStarted(object? sender, DownloadStartedEventArgs e)
    {
        Status = QuantumDownloadStatus.Downloading;
        Describe = "Download started...";
        Percentage = 0;
    }
    
    private void OnDownloadProgressChanged(object? sender, DownloadProgressChangedEventArgs e)
    {
        Describe = String.Format("{0}%, {1}/{2}, {3} {4}", Convert.ToInt32(e.ProgressPercentage).ToString(), CalcByteFormat(DlService.Package.ReceivedBytesSize), CalcByteFormat(DlService.Package.TotalFileSize), Chunks.ToString(), Chunks > 1 ? "Chunks" : "Chunk");
        Percentage = e.ProgressPercentage;
    }
    
    private void OnDownloadCompleted(object? sender, AsyncCompletedEventArgs e)
    {
        
        if(e.Error == null)
        {
            Status = QuantumDownloadStatus.Completed;
            Describe = "Completed!";
        }
        else
        {
            Status = QuantumDownloadStatus.Error;
            Describe = e.Error.Message;
            OnDownloadError?.Invoke(this, DownloadEvent.Create(e.Error.Message));
        }
    }

    private string CalcByteFormat(double bytes)
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

    public async void Start()
    {
        Init();
        DirectoryInfo dest = new DirectoryInfo(Destination);
        if (!dest.Exists)
        {
            try
            {
                Directory.CreateDirectory(Destination);
            }catch(Exception ex)
            {
                OnDownloadError?.Invoke(this, DownloadEvent.Create("无法创建文件hhhhhhhhhhhh"));
                return;
            }
        }
        Status = QuantumDownloadStatus.Downloading;
            //try
            //{
            //OnDownloadError?.Invoke(this, DownloadEvent.Create("下载失败辣辣辣辣"));
            await DlService.DownloadFileTaskAsync(Source, dest);
            //}catch(Exception ex)
            //{
            //    Downloading = false;
            //    OnDownloadError?.Invoke(this, DownloadEvent.Create("下载失败辣辣辣辣"));
            //}
    }

    public void Stop()
    {
        DlService.CancelAsync();
        Status = QuantumDownloadStatus.Stopped;
    }

    public QuantumDownload(string name, SymbolRegular symbol, string source, string destination, string ua)
    {
        Name = name;
        Symbol = symbol;
        Source = source;
        Destination = destination;
        UserAgent = ua;
        InitializeHandlers();
    }
    public QuantumDownload(string name, string source, string destination)
    {
        Name = name;
        Source = source;
        Destination = destination;
        InitializeHandlers();
    }
    public QuantumDownload(string source, string destination)
    {
        Name = source;
        Source = source;
        Destination = destination;
        InitializeHandlers();
    }
    public QuantumDownload(string source)
    {
        Name = source;
        Source = source;
        InitializeHandlers();
    }
    public QuantumDownload()
    {
        InitializeHandlers();
    }
}