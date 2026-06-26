using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using mortar.models;
using System.Windows.Media;
using mortar.services;

namespace mortar.windows
{
    public partial class mortarWindowControl : UserControl
    {
        private string solutionDir;
        private bool showSyncStatus = true;
        private string editingPath = null;
        private string editingUrl = null;
        private HashSet<string> expandedNodes = new HashSet<string>();
        private string addingLinkToSource = null;

        public static readonly Dictionary<string, string> docTypeDisplayNames = new Dictionary<string, string>
        {
            { "", "" },
            { "datasheet", "Datasheet" },
            { "requirements", "Requirements" },
            { "schematic", "Schematic" },
            { "testSpec", "Test Specification" },
            { "apiSpec", "API Specification" },
            { "researchPaper", "Research Paper" },
            { "designSpec", "Design Specification" },
            { "runbook", "Runbook" },
            { "license", "License" },
            { "changelog", "Changelog" },
            { "other", "Other" }
        };

        public List<string> docTypeOptions { get; } = new List<string>
        {
            "",
            "datasheet",
            "requirements",
            "schematic",
            "testSpec",
            "apiSpec",
            "researchPaper",
            "designSpec",
            "runbook",
            "license",
            "changelog",
            "other"
        };

        public mortarWindowControl()
        {
            InitializeComponent();
        }

        public void setSolutionDir(string dir)
        {
            solutionDir = dir;
            if (dir != null)
                loadLinks();
            else
                showEmptyState(noSolution: true);
        }

        private void showEmptyState(bool noSolution = false)
        {
            noSolutionText.Visibility = noSolution
                ? Visibility.Visible
                : Visibility.Collapsed;
            noLinksText.Visibility = !noSolution
                ? Visibility.Visible
                : Visibility.Collapsed;
            linksTree.Visibility = Visibility.Collapsed;
            gitWarningBanner.Visibility = Visibility.Collapsed;
        }

        private string getLinksFilePath()
        {
            if (string.IsNullOrEmpty(solutionDir))
                return null;

            return System.IO.Path.Combine(solutionDir, "doclinks.json");
        }

        private void loadLinks()
        {
            string path = getLinksFilePath();

            if (path == null || !File.Exists(path))
            {
                showEmptyState();
                gitWarningBanner.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                saveExpansionState();

                string json = File.ReadAllText(path);
                var links = JsonConvert.DeserializeObject<List<docLink>>(json);

                if (links == null || links.Count == 0)
                {
                    showEmptyState();
                    gitWarningBanner.Visibility = Visibility.Collapsed;
                    return;
                }

                var nodes = buildTree(links);
                linksTree.ItemsSource = nodes;
                linksTree.Visibility = Visibility.Visible;
                noSolutionText.Visibility = Visibility.Collapsed;
                noLinksText.Visibility = Visibility.Collapsed;

                gitWarningBanner.Visibility = hasUncommittedGitChanges(path)
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                linksTree.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Loaded,
                    new Action(restoreExpansionState));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading links: {ex.Message}");
                showEmptyState();
            }
        }

        private List<sourceFileNode> buildTree(List<docLink> links)
        {
            var nodes = new List<sourceFileNode>();

            foreach (var link in links)
            {
                var sourceNode = new sourceFileNode
                {
                    displayName = Path.GetFileName(link.sourceFile),
                    fullPath = link.sourceFile,
                    isAddingLink = addingLinkToSource != null &&
                        pathHelper.pathsEqual(addingLinkToSource, link.sourceFile)
                };

                foreach (var doc in link.documentPaths)
                {
                    bool outOfDate = false;
                    if (showSyncStatus && doc.outOfDateDetection && !string.IsNullOrEmpty(doc.path))
                        outOfDate = checkOutOfDate(link.sourceFile, doc.path);

                    string dotColor = !showSyncStatus || !doc.outOfDateDetection
                        ? "Gray"
                        : outOfDate ? "Red" : "Green";

                    string display = !string.IsNullOrWhiteSpace(doc.nickname)
                        ? doc.nickname
                        : !string.IsNullOrEmpty(doc.path)
                            ? Path.GetFileName(doc.path)
                            : doc.url ?? "unnamed";

                    string docTypeLabel = !string.IsNullOrWhiteSpace(doc.docType)
                        ? $" [{(docTypeDisplayNames.TryGetValue(doc.docType, out string displayName) ? displayName : doc.docType)}]"
                        : "";

                    var node = new documentNode
                    {
                        displayName = $"{display}{docTypeLabel}",
                        fullPath = doc.path,
                        url = doc.url,
                        docType = string.IsNullOrWhiteSpace(doc.docType) ? null : doc.docType,
                        notes = string.IsNullOrWhiteSpace(doc.notes) ? null : doc.notes,
                        isPrimary = doc.isPrimary,
                        isOutOfDate = outOfDate,
                        dotColor = dotColor,
                        notesVisibility = string.IsNullOrWhiteSpace(doc.notes) ? "Collapsed" : "Visible",
                        editNickname = doc.nickname,
                        editPath = doc.path,
                        editUrl = doc.url,
                        editDocType = doc.docType,
                        editNotes = doc.notes,
                        isEditing = isMatchingEditNode(doc.path, doc.url),
                        childrenVisibility = isMatchingEditNode(doc.path, doc.url) ? "Collapsed" : "Visible"
                    };

                    if (!string.IsNullOrWhiteSpace(doc.notes) && !node.isEditing)
                    {
                        node.children.Add(new detailNode { text = $"📝 {doc.notes}" });
                    }

                    sourceNode.documents.Add(node);
                }

                nodes.Add(sourceNode);
            }

            return nodes;
        }

        private bool checkOutOfDate(string sourceFile, string documentPath)
        {
            if (!File.Exists(sourceFile) || !File.Exists(documentPath))
                return false;

            try
            {
                DateTime srcModified = File.GetLastWriteTimeUtc(sourceFile);
                DateTime docModified = File.GetLastWriteTimeUtc(documentPath);
                return docModified > srcModified;
            }
            catch
            {
                return false;
            }
        }

        private void refreshClick(object sender, RoutedEventArgs e)
        {
            loadLinks();
        }

        private void syncToggleClick(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggle)
                showSyncStatus = toggle.IsChecked ?? true;
            loadLinks();
        }

        private void documentNodeClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is StackPanel panel)
            {
                var item = getParentTreeViewItem(panel);
                if (item != null)
                    item.IsSelected = false;

                if (panel.DataContext is documentNode node)
                {
                    if (string.IsNullOrEmpty(node.fullPath) && !string.IsNullOrEmpty(node.url))
                    {
                        System.Diagnostics.Process.Start(node.url);
                        return;
                    }

                    if (!string.IsNullOrEmpty(node.fullPath))
                    {
                        if (File.Exists(node.fullPath))
                            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{node.fullPath}\"");
                        else
                            MessageBox.Show($"File not found:\n{node.fullPath}",
                                "mortar", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    MessageBox.Show("No path or URL associated with this link.",
                        "mortar", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private TreeViewItem getParentTreeViewItem(DependencyObject item)
        {
            if (item == null) return null;
            var parent = VisualTreeHelper.GetParent(item);
            while (parent != null && !(parent is TreeViewItem))
                parent = VisualTreeHelper.GetParent(parent);
            return parent as TreeViewItem;
        }

        private void contextMenuOpen(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem &&
                menuItem.DataContext is documentNode node)
            {
                if (string.IsNullOrEmpty(node.fullPath) && !string.IsNullOrEmpty(node.url))
                {
                    System.Diagnostics.Process.Start(node.url);
                    return;
                }

                if (!string.IsNullOrEmpty(node.fullPath))
                {
                    if (File.Exists(node.fullPath))
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{node.fullPath}\"");
                    else
                        MessageBox.Show($"File not found:\n{node.fullPath}",
                            "mortar", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void contextMenuCopyPath(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem &&
                menuItem.DataContext is documentNode node)
            {
                string value = !string.IsNullOrEmpty(node.fullPath)
                    ? node.fullPath
                    : node.url;

                if (!string.IsNullOrEmpty(value))
                {
                    System.Windows.Clipboard.SetText(value);
                    MessageBox.Show("Copied to clipboard.",
                        "mortar", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private bool hasUncommittedGitChanges(string filePath)
        {
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"status --porcelain \"{filePath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = solutionDir
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output);
            }
            catch
            {
                return false;
            }
        }

        private void dismissGitWarning(object sender, RoutedEventArgs e)
        {
            gitWarningBanner.Visibility = Visibility.Collapsed;
        }

        private void saveEdit(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is documentNode node)
            {
                string path = getLinksFilePath();
                if (path == null) return;

                var links = storageService.loadLinks(path);
                if (links == null) return;

                docLink link = null;
                documentEntry doc = null;

                foreach (var l in links)
                {
                    foreach (var d in l.documentPaths)
                    {
                        bool pathMatch = !string.IsNullOrEmpty(node.fullPath) &&
                                         !string.IsNullOrEmpty(d.path) &&
                                         pathHelper.pathsEqual(d.path, node.fullPath);
                        bool urlMatch = !string.IsNullOrEmpty(node.url) &&
                                        d.url == node.url;

                        if (pathMatch || urlMatch)
                        {
                            link = l;
                            doc = d;
                            break;
                        }
                    }
                    if (doc != null) break;
                }

                if (doc == null) return;

                doc.nickname = string.IsNullOrWhiteSpace(node.editNickname) ? null : node.editNickname;
                doc.path = string.IsNullOrWhiteSpace(node.editPath) ? null : node.editPath;
                doc.url = string.IsNullOrWhiteSpace(node.editUrl) ? null : node.editUrl;
                doc.docType = string.IsNullOrWhiteSpace(node.editDocType) ? null : node.editDocType;
                doc.notes = string.IsNullOrWhiteSpace(node.editNotes) ? null : node.editNotes;

                storageService.saveLinks(path, links);

                editingPath = null;
                editingUrl = null;
                loadLinks();
            }
        }

        private void cancelEdit(object sender, RoutedEventArgs e)
        {
            editingPath = null;
            editingUrl = null;
            loadLinks();
        }

        private void contextMenuEdit(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem &&
                menuItem.DataContext is documentNode node)
            {
                editingPath = node.fullPath;
                editingUrl = node.url;
                loadLinks();
            }
        }

        private bool isMatchingEditNode(string path, string url)
        {
            if (editingPath != null && !string.IsNullOrEmpty(path))
                return pathHelper.pathsEqual(editingPath, path);
            if (editingUrl != null && !string.IsNullOrEmpty(url))
                return editingUrl == url;
            return false;
        }

        private void saveExpansionState()
        {
            expandedNodes.Clear();
            foreach (var item in getTreeViewItems(linksTree))
            {
                if (item.IsExpanded && item.DataContext is sourceFileNode node)
                    expandedNodes.Add(node.fullPath);
            }
        }

        private IEnumerable<TreeViewItem> getTreeViewItems(ItemsControl parent)
        {
            for (int i = 0; i < parent.Items.Count; i++)
            {
                var item = parent.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
                if (item == null) continue;
                yield return item;
                foreach (var child in getTreeViewItems(item))
                    yield return child;
            }
        }

        private void restoreExpansionState()
        {
            foreach (var item in getTreeViewItems(linksTree))
            {
                if (item.DataContext is sourceFileNode node &&
                    expandedNodes.Contains(node.fullPath))
                    item.IsExpanded = true;

                if (item.DataContext is documentNode docNode && docNode.isEditing)
                {
                    item.IsExpanded = true;
                    var parent = getParentTreeViewItem(item);
                    if (parent != null)
                        parent.IsExpanded = true;
                }
            }
        }

        private void contextMenuAddLink(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem &&
                menuItem.DataContext is sourceFileNode node)
            {
                addingLinkToSource = node.fullPath;
                loadLinks();
            }
        }

        private void saveNewLink(object sender, RoutedEventArgs e)
        {
            if (sender is Button button &&
                button.DataContext is sourceFileNode node)
            {
                var path = getLinksFilePath();
                if (path == null) return;

                var links = storageService.loadLinks(path);
                if (links == null) return;

                var panel = button.Parent as StackPanel;
                var outerPanel = panel?.Parent as StackPanel;

                string newPathValue = (outerPanel?.FindName("newLinkPath") as TextBox)?.Text?.Trim();
                string newUrlValue = (outerPanel?.FindName("newLinkUrl") as TextBox)?.Text?.Trim();
                string newNickname = (outerPanel?.FindName("newLinkNickname") as TextBox)?.Text?.Trim();
                string newDocType = (outerPanel?.FindName("newLinkDocType") as ComboBox)?.SelectedValue as string;
                string newNotes = (outerPanel?.FindName("newLinkNotes") as TextBox)?.Text?.Trim();
                bool newPrimary = (outerPanel?.FindName("newLinkPrimary") as CheckBox)?.IsChecked ?? false;

                if (string.IsNullOrEmpty(newPathValue) && string.IsNullOrEmpty(newUrlValue))
                {
                    MessageBox.Show("Please provide a path or URL.",
                        "mortar", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string normalizedPath = string.IsNullOrEmpty(newPathValue)
                    ? null
                    : pathHelper.normalizePath(newPathValue);

                var existing = links.Find(l => pathHelper.pathsEqual(l.sourceFile, node.fullPath));

                var entry = new documentEntry
                {
                    path = normalizedPath,
                    url = string.IsNullOrEmpty(newUrlValue) ? null : newUrlValue,
                    nickname = string.IsNullOrEmpty(newNickname) ? null : newNickname,
                    docType = string.IsNullOrEmpty(newDocType) ? null : newDocType,
                    notes = string.IsNullOrEmpty(newNotes) ? null : newNotes,
                    isPrimary = newPrimary,
                    outOfDateDetection = true
                };

                if (existing != null)
                {
                    existing.documentPaths.Add(entry);
                }
                else
                {
                    links.Add(new docLink
                    {
                        sourceFile = node.fullPath,
                        documentPaths = new List<documentEntry> { entry },
                        linkedAt = DateTime.UtcNow.ToString("o")
                    });
                }

                storageService.saveLinks(path, links);
                addingLinkToSource = null;
                loadLinks();
            }
        }

        private void cancelNewLink(object sender, RoutedEventArgs e)
        {
            addingLinkToSource = null;
            loadLinks();
        }

        private void headerAddLink(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Add Link from header coming soon.",
                "mortar", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}