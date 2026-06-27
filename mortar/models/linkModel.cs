using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace mortar.models
{
    public class documentEntry
    {
        public string path { get; set; }
        public string url { get; set; }
        public string nickname { get; set; }
        public string docType { get; set; }
        public string notes { get; set; }
        public bool isPrimary { get; set; } = false;
        public bool outOfDateDetection { get; set; } = true;
    }

    public class docLink
    {
        public string sourceFile { get; set; }
        public List<documentEntry> documentPaths { get; set; } = new List<documentEntry>();
        public string linkedAt { get; set; }
    }

    public class detailNode
    {
        public string text { get; set; }
    }

    public class documentNode
    {
        public string displayName { get; set; }
        public string fullPath { get; set; }
        public string url { get; set; }
        public string docType { get; set; }
        public string notes { get; set; }
        public bool isPrimary { get; set; }
        public bool isOutOfDate { get; set; }
        public string dotColor { get; set; }
        public string notesVisibility { get; set; }
        public string childrenVisibility { get; set; }
        public List<detailNode> children { get; set; } = new List<detailNode>();
        public bool isEditing { get; set; } = false;
        public string editNickname { get; set; }
        public string editPath { get; set; }
        public string editUrl { get; set; }
        public string editDocType { get; set; }
        public string editNotes { get; set; }
        public bool editPathEnabled => string.IsNullOrEmpty(editUrl);
        public bool editUrlEnabled => string.IsNullOrEmpty(editPath);
    }
    public class sourceFileNode
    {
        public string displayName { get; set; }
        public string fullPath { get; set; }
        public List<documentNode> documents { get; set; } = new List<documentNode>();
        public bool isAddingLink { get; set; } = false;
        public newLinkForm newLink { get; set; } = new newLinkForm();
    }

    public class newLinkForm : System.ComponentModel.INotifyPropertyChanged
    {
        private string _path = "";
        private string _url = "";

        public string path
        {
            get => _path;
            set
            {
                _path = value;
                onPropertyChanged(nameof(path));
                onPropertyChanged(nameof(urlEnabled));
            }
        }

        public string url
        {
            get => _url;
            set
            {
                _url = value;
                onPropertyChanged(nameof(url));
                onPropertyChanged(nameof(pathEnabled));
            }
        }

        public bool pathEnabled => string.IsNullOrEmpty(_url);
        public bool urlEnabled => string.IsNullOrEmpty(_path);

        public string nickname { get; set; } = "";
        public string docType { get; set; } = "";
        public string notes { get; set; } = "";
        public bool isPrimary { get; set; } = false;

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        private void onPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        }
    }

    public class newSourceLinkForm : System.ComponentModel.INotifyPropertyChanged
    {
        private string _path = "";
        private string _url = "";

        public string sourceFile { get; set; } = "";

        public string path
        {
            get => _path;
            set
            {
                _path = value;
                onPropertyChanged(nameof(path));
                onPropertyChanged(nameof(urlEnabled));
            }
        }

        public string url
        {
            get => _url;
            set
            {
                _url = value;
                onPropertyChanged(nameof(url));
                onPropertyChanged(nameof(pathEnabled));
            }
        }

        public bool pathEnabled => string.IsNullOrEmpty(_url);
        public bool urlEnabled => string.IsNullOrEmpty(_path);

        public string nickname { get; set; } = "";
        public string docType { get; set; } = "";
        public string notes { get; set; } = "";
        public bool isPrimary { get; set; } = false;

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        private void onPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        }
    }

    public class docTypeDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string key && mortar.windows.mortarWindowControl.docTypeDisplayNames.ContainsKey(key))
                return mortar.windows.mortarWindowControl.docTypeDisplayNames[key];
            return value ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string display)
            {
                foreach (var kvp in mortar.windows.mortarWindowControl.docTypeDisplayNames)
                    if (kvp.Value == display) return kvp.Key;
            }
            return value ?? "";
        }
    }
}