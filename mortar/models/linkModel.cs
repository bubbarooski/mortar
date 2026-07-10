using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace mortar.models
{
    // ── Storage models — serialized to/from doclinks.mor ─────────────────────

    public class documentEntry
    {
        public string? path { get; set; }
        public string? url { get; set; }
        public string? nickname { get; set; }
        public string? docType { get; set; }
        public string? notes { get; set; }
        public bool isPrimary { get; set; } = false;
        public bool outOfDateDetection { get; set; } = true;
    }

    public class docLink
    {
        public string? sourceFile { get; set; }
        public List<documentEntry> documentPaths { get; set; } = new List<documentEntry>();
        public string? linkedAt { get; set; }
    }

    // ── UI tree nodes — built from storage models for display ─────────────────

    public class detailNode
    {
        public string? text { get; set; }
    }

    public class documentNode
    {
        public string? displayName { get; set; }
        public string? fullPath { get; set; }
        public string? url { get; set; }
        public string? docType { get; set; }
        public string? notes { get; set; }
        public bool isPrimary { get; set; }
        public bool isOutOfDate { get; set; }
        public string? dotColor { get; set; }
        public string? notesVisibility { get; set; }
        public string? childrenVisibility { get; set; }
        public List<detailNode> children { get; set; } = new List<detailNode>();
        public bool isEditing { get; set; } = false;
        public string? editNickname { get; set; }
        public string? editPath { get; set; }
        public string? editUrl { get; set; }
        public string? editDocType { get; set; }
        public string? editNotes { get; set; }
        public string? nodeId { get; set; }
        public string? tagValue { get; set; }

        // Path/URL are mutually exclusive — gray out the other when one is set
        public bool editPathEnabled => string.IsNullOrEmpty(editUrl);
        public bool editUrlEnabled => string.IsNullOrEmpty(editPath);
    }

    public class sourceFileNode
    {
        public string? displayName { get; set; }
        public string? fullPath { get; set; }
        public List<documentNode> documents { get; set; } = new List<documentNode>();
        public bool isAddingLink { get; set; } = false;
        public newLinkForm newLink { get; set; } = new newLinkForm();
        public string? nodeId { get; set; }
        public string? tagValue { get; set; }
    }

    public class folderNode
    {
        public string? displayName { get; set; }
        public string? fullPath { get; set; }
        public List<folderNode> subFolders { get; set; } = new List<folderNode>();
        public List<sourceFileNode> files { get; set; } = new List<sourceFileNode>();

        // Combined list for XAML HierarchicalDataTemplate binding
        public List<object> children
        {
            get
            {
                var result = new List<object>();
                result.AddRange(subFolders);
                result.AddRange(files);
                return result;
            }
        }
    }

    // ── Form models — bound to add/edit panels in the UI ─────────────────────

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

        // Path/URL are mutually exclusive — entering one disables the other
        public bool pathEnabled => string.IsNullOrEmpty(_url);
        public bool urlEnabled => string.IsNullOrEmpty(_path);

        public string nickname { get; set; } = "";
        public string docType { get; set; } = "";
        public string notes { get; set; } = "";
        public bool isPrimary { get; set; } = false;

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

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

        // Path/URL are mutually exclusive — entering one disables the other
        public bool pathEnabled => string.IsNullOrEmpty(_url);
        public bool urlEnabled => string.IsNullOrEmpty(_path);

        public string nickname { get; set; } = "";
        public string docType { get; set; } = "";
        public string notes { get; set; } = "";
        public bool isPrimary { get; set; } = false;

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        private void onPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        }
    }

    // ── Value converter — maps docType keys to display names ─────────────────

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