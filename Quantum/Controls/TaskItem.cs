using Quantum.Core.Download;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Common;

namespace Quantum.Controls
{
    public class TaskItem:ContentControl
    {
        public static readonly DependencyProperty TaskNameProperty = DependencyProperty.Register(nameof(TaskName),
            typeof(string), typeof(ContentControl), new PropertyMetadata(""));
        public static readonly DependencyProperty DescProperty = DependencyProperty.Register(nameof(Desc),
            typeof(string), typeof(ContentControl), new PropertyMetadata(""));
        public static readonly DependencyProperty TaskSymbolProperty = DependencyProperty.Register(nameof(TaskSymbol),
            typeof(SymbolRegular), typeof(ContentControl), new PropertyMetadata(SymbolRegular.Info24));
        public static readonly DependencyProperty PercentageProperty = DependencyProperty.Register(nameof(Percentage),
            typeof(double), typeof(ContentControl), new PropertyMetadata(0.0));
        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(nameof(Status),
            typeof(QuantumDownloadStatus), typeof(ContentControl), new PropertyMetadata(QuantumDownloadStatus.Stopped));
        public string TaskName
        {
            get => (string)GetValue(TaskNameProperty);
            set => SetValue(TaskNameProperty, value);
        }
        public string Desc
        {
            get => (string)GetValue(DescProperty);
            set => SetValue(DescProperty, value);
        }
        public SymbolRegular TaskSymbol
        {
            get => (SymbolRegular)GetValue(TaskNameProperty);
            set => SetValue(TaskNameProperty, value);
        }
        public double Percentage
        {
            get => (double)GetValue(PercentageProperty);
            set => SetValue(PercentageProperty, value);
        }
        public QuantumDownloadStatus Status
        {
            get => (QuantumDownloadStatus)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }
    }
}
