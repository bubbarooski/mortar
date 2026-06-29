using Microsoft.VisualStudio.Shell;
using mortar.models;
using mortar.services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace mortar.windows
{
    public partial class mortarWindowControl : UserControl
    {
        private string solutionDir;
        private bool showSyncStatus = true;
        private string editingPath = null;
        private string editingUrl = null;
        private string originalEditingPath = null;
        private string originalEditingUrl = null;
        private bool pathCleared = false;
        private bool urlCleared = false;
        private HashSet<string> expandedNodes = new HashSet<string>();
        private string addingLinkToSource = null;
        private bool showingHeaderAddForm = false;
        private Dictionary<string, documentNode> nodeRegistry = new Dictionary<string, documentNode>();
        private Dictionary<string, sourceFileNode> sourceNodeRegistry = new Dictionary<string, sourceFileNode>();
        public newSourceLinkForm headerLinkForm { get; set; } = new newSourceLinkForm();

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
            savedBorder.Visibility = Visibility.Collapsed;
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
            return System.IO.Path.Combine(solutionDir, "docLinks.mor");
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

        private List<folderNode> buildTree(List<docLink> links)
        {
            var folderMap = new Dictionary<string, folderNode>();
            nodeRegistry.Clear();
            sourceNodeRegistry.Clear();

            foreach (var link in links)
            {
                string resolvedSourceFile = pathHelper.resolveRelativePath(solutionDir, link.sourceFile);

                bool isAddingToThisFile = addingLinkToSource != null &&
                    pathHelper.pathsEqual(addingLinkToSource, resolvedSourceFile);

                string sourceId = resolvedSourceFile;
                var sourceNode = new sourceFileNode
                {
                    nodeId = sourceId,
                    displayName = Path.GetFileName(resolvedSourceFile),
                    fullPath = resolvedSourceFile,
                    isAddingLink = isAddingToThisFile,
                    newLink = new newLinkForm(),
                    tagValue = resolvedSourceFile
                };
                sourceNodeRegistry[sourceId] = sourceNode;

                if (!isAddingToThisFile)
                {
                    foreach (var doc in link.documentPaths)
                    {
                        bool outOfDate = false;
                        if (showSyncStatus && doc.outOfDateDetection && !string.IsNullOrEmpty(doc.path))
                            outOfDate = checkOutOfDate(resolvedSourceFile, doc.path);

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

                        bool isEditing = isMatchingEditNode(doc.path, doc.url);

                        string docId = $"{link.sourceFile}|{doc.path ?? doc.url ?? doc.nickname ?? Guid.NewGuid().ToString()}";
                        var node = new documentNode
                        {
                            nodeId = docId,
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
                            editPath = isEditing && pathCleared ? null : doc.path,
                            editUrl = isEditing && urlCleared ? null : doc.url,
                            editDocType = doc.docType,
                            editNotes = doc.notes,
                            isEditing = isEditing,
                            childrenVisibility = isEditing ? "Collapsed" : "Visible",
                            tagValue = !string.IsNullOrEmpty(doc.path) ? $"path:{doc.path}" : $"url:{doc.url}"
                        };

                        nodeRegistry[docId] = node;

                        if (!string.IsNullOrWhiteSpace(doc.notes) && !node.isEditing)
                            node.children.Add(new detailNode { text = $"📝 {doc.notes}" });

                        sourceNode.documents.Add(node);
                    }
                }

                addToFolderTree(folderMap, sourceNode, resolvedSourceFile);
            }

            // Build final tree with solution root as top-level folder
            var solutionFolder = new folderNode
            {
                displayName = Path.GetFileName(solutionDir.TrimEnd(Path.DirectorySeparatorChar)),
                fullPath = solutionDir
            };

            if (folderMap.ContainsKey("__root__"))
                solutionFolder.files.AddRange(folderMap["__root__"].files);

            foreach (var folder in folderMap.Values
                .Where(f => f.fullPath != "__root__" &&
                    !pathHelper.pathsEqual(f.fullPath, solutionDir.TrimEnd(Path.DirectorySeparatorChar)))
                .OrderBy(f => f.displayName))
            {
                solutionFolder.subFolders.Add(folder);
            }

            return new List<folderNode> { solutionFolder };
        }

        private void addToFolderTree(Dictionary<string, folderNode> rootMap, sourceFileNode sourceNode, string resolvedSourceFile)
        {
            string fileDir = Path.GetDirectoryName(resolvedSourceFile);
            string normalizedSolutionDir = solutionDir.TrimEnd(Path.DirectorySeparatorChar);

            // File is directly in solution root — add to a special root bucket
            if (string.Equals(fileDir, normalizedSolutionDir, StringComparison.OrdinalIgnoreCase))
            {
                if (!rootMap.ContainsKey("__root__"))
                    rootMap["__root__"] = new folderNode
                    {
                        displayName = Path.GetFileName(normalizedSolutionDir),
                        fullPath = normalizedSolutionDir
                    };
                rootMap["__root__"].files.Add(sourceNode);
                return;
            }

            // Build relative path segments from solution root to file's directory
            string relativePath = pathHelper.makeRelativePath(
                normalizedSolutionDir + Path.DirectorySeparatorChar, fileDir + Path.DirectorySeparatorChar);
            string[] parts = relativePath.TrimEnd(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);

            // Walk/create the folder tree from root down
            Dictionary<string, folderNode> currentLevel = rootMap;
            folderNode parentFolder = null;
            string builtPath = normalizedSolutionDir;

            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                builtPath = Path.Combine(builtPath, part);

                if (!currentLevel.ContainsKey(builtPath))
                {
                    var newFolder = new folderNode
                    {
                        displayName = part,
                        fullPath = builtPath
                    };
                    currentLevel[builtPath] = newFolder;
                    parentFolder?.subFolders.Add(newFolder);
                }

                parentFolder = currentLevel[builtPath];
                var nextLevel = new Dictionary<string, folderNode>();
                foreach (var sub in parentFolder.subFolders)
                    nextLevel[sub.fullPath] = sub;
                currentLevel = nextLevel;
            }

            parentFolder?.files.Add(sourceNode);
        }

        private (string path, string url) getIdentifierFromContextMenu(object sender)
        {
            if (sender is MenuItem menuItem &&
                menuItem.Parent is ContextMenu contextMenu &&
                contextMenu.PlacementTarget is FrameworkElement element)
            {
                System.Diagnostics.Debug.WriteLine($"Tag value: '{element.Tag}'");
                if (element.Tag is string tag)
                {
                    System.Diagnostics.Debug.WriteLine($"Tag string: '{tag}'");
                    if (tag.StartsWith("path:"))
                        return (tag.Substring(5), null);
                    if (tag.StartsWith("url:"))
                        return (null, tag.Substring(4));
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"getIdentifierFromContextMenu failed - sender is MenuItem: {sender is MenuItem}");
            }
            return (null, null);
        }

        private sourceFileNode getSourceNodeFromContextMenu(object sender)
        {
            if (sender is MenuItem menuItem &&
                menuItem.Parent is ContextMenu contextMenu &&
                contextMenu.PlacementTarget is FrameworkElement element &&
                element.Tag is string nodeId &&
                sourceNodeRegistry.TryGetValue(nodeId, out var node))
                return node;
            return null;
        }

        private bool isMatchingEditNode(string path, string url)
        {
            bool result = false;
            if (originalEditingPath != null && !string.IsNullOrEmpty(path))
                result = pathHelper.pathsEqual(originalEditingPath, path);
            else if (originalEditingUrl != null && !string.IsNullOrEmpty(url))
                result = originalEditingUrl == url;

            System.Diagnostics.Debug.WriteLine(
                $"isMatchingEditNode: origPath={originalEditingPath ?? "null"} " +
                $"origUrl={originalEditingUrl ?? "null"} " +
                $"docPath={path ?? "null"} docUrl={url ?? "null"} result={result}");

            return result;
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
            catch { return false; }
        }

        // ── Document context menu handlers ───────────────────────────────────

        private void contextMenuOpen(object sender, RoutedEventArgs e)
        {
            var (path, url) = getIdentifierFromContextMenu(sender);

            if (!string.IsNullOrEmpty(url))
            {
                System.Diagnostics.Process.Start(url);
                return;
            }

            if (!string.IsNullOrEmpty(path))
            {
                if (File.Exists(path))
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
                else
                    MessageBox.Show($"File not found:\n{path}",
                        "mortar", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void contextMenuCopyPath(object sender, RoutedEventArgs e)
        {
            var (path, url) = getIdentifierFromContextMenu(sender);
            string value = !string.IsNullOrEmpty(path) ? path : url;

            if (!string.IsNullOrEmpty(value))
            {
                System.Windows.Clipboard.SetText(value);
                MessageBox.Show("Copied to clipboard.",
                    "mortar", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void contextMenuEdit(object sender, RoutedEventArgs e)
        {
            var (path, url) = getIdentifierFromContextMenu(sender);
            System.Diagnostics.Debug.WriteLine($"contextMenuEdit: path={path ?? "null"} url={url ?? "null"}");

            if (string.IsNullOrEmpty(path) && string.IsNullOrEmpty(url)) return;

            originalEditingPath = path;
            originalEditingUrl = url;
            editingPath = path;
            editingUrl = url;
            pathCleared = false;
            urlCleared = false;
            loadLinks();
        }

        private void contextMenuDelete(object sender, RoutedEventArgs e)
        {
            var (path, url) = getIdentifierFromContextMenu(sender);
            if (string.IsNullOrEmpty(path) && string.IsNullOrEmpty(url)) return;

            string label = !string.IsNullOrEmpty(path) ? Path.GetFileName(path) : url;
            var result = MessageBox.Show(
                $"Delete link to \"{label}\"?\n\nThis cannot be undone.",
                "mortar", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            string linksPath = getLinksFilePath();
            if (linksPath == null) return;

            var links = storageService.loadLinks(linksPath);
            if (links == null) return;

            foreach (var link in links)
            {
                var entry = link.documentPaths.Find(d =>
                    (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(d.path) &&
                     pathHelper.pathsEqual(d.path, path)) ||
                    (!string.IsNullOrEmpty(url) && d.url == url));

                if (entry != null)
                {
                    link.documentPaths.Remove(entry);
                    if (link.documentPaths.Count == 0)
                        links.Remove(link);
                    break;
                }
            }

            storageService.saveLinks(linksPath, links);
            loadLinks();
        }

        // ── Source file context menu handlers ────────────────────────────────

        private void contextMenuAddLink(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem &&
                menuItem.Parent is ContextMenu contextMenu &&
                contextMenu.PlacementTarget is FrameworkElement element &&
                element.Tag is string sourceFile &&
                !string.IsNullOrEmpty(sourceFile))
            {
                addingLinkToSource = sourceFile;
                loadLinks();
            }
        }

        // ── Edit panel handlers ──────────────────────────────────────────────

        private void saveEdit(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is documentNode node)
            {
                // Validate at least one of path or url is provided
                if (string.IsNullOrWhiteSpace(node.editPath) && string.IsNullOrWhiteSpace(node.editUrl))
                {
                    MessageBox.Show("Please provide at least a path or URL.",
                        "mortar", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

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
                        bool pathMatch = !string.IsNullOrEmpty(originalEditingPath) &&
                                         !string.IsNullOrEmpty(d.path) &&
                                         pathHelper.pathsEqual(d.path, originalEditingPath);
                        bool urlMatch = !string.IsNullOrEmpty(originalEditingUrl) &&
                                        d.url == originalEditingUrl;

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
                resetEditState();
                loadLinks();
                showSavedConfirmation();
            }
        }

        private void cancelEdit(object sender, RoutedEventArgs e)
        {
            resetEditState();
            loadLinks();
        }

        private void resetEditState()
        {
            editingPath = null;
            editingUrl = null;
            originalEditingPath = null;
            originalEditingUrl = null;
            pathCleared = false;
            urlCleared = false;
        }

        private void clearEditPath(object sender, RoutedEventArgs e)
        {
            pathCleared = true;
            loadLinks();
        }

        private void clearEditUrl(object sender, RoutedEventArgs e)
        {
            urlCleared = true;
            loadLinks();
        }

        // ── Add link panel handlers ──────────────────────────────────────────

        private void saveNewLink(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"saveNewLink fired, sender type: {sender?.GetType()?.Name}");
            if (sender is Button debugBtn)
            {
                System.Diagnostics.Debug.WriteLine($"DataContext type: {debugBtn.DataContext?.GetType()?.FullName ?? "null"}");
            }

            if (sender is Button button && button.DataContext is sourceFileNode node)
            {
                var path = getLinksFilePath();
                if (path == null) return;

                var links = storageService.loadLinks(path);
                if (links == null) return;

                string newPathValue = node.newLink.path?.Trim();
                string newUrlValue = node.newLink.url?.Trim();
                string newNickname = node.newLink.nickname?.Trim();
                string newDocType = node.newLink.docType?.Trim();
                string newNotes = node.newLink.notes?.Trim();
                bool newPrimary = node.newLink.isPrimary;

                if (string.IsNullOrEmpty(newPathValue) && string.IsNullOrEmpty(newUrlValue))
                {
                    MessageBox.Show("Please provide a path or URL.",
                        "mortar", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!string.IsNullOrEmpty(newPathValue) && !File.Exists(newPathValue))
                {
                    MessageBox.Show($"File not found:\n{newPathValue}\n\nPlease provide a valid path.",
                        "mortar", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string normalizedPath = string.IsNullOrEmpty(newPathValue)
                    ? null
                    : pathHelper.normalizePath(newPathValue);

                // Store source file as relative path for portability
                string relativeSource = pathHelper.makeRelativePath(solutionDir, node.fullPath);
                System.Diagnostics.Debug.WriteLine($"solutionDir: {solutionDir}");
                System.Diagnostics.Debug.WriteLine($"node.fullPath: {node.fullPath}");
                System.Diagnostics.Debug.WriteLine($"relativeSource: {relativeSource}");

                var existing = links.Find(l => pathHelper.pathsEqual(
                    pathHelper.resolveRelativePath(solutionDir, l.sourceFile),
                    node.fullPath));

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
                    existing.documentPaths.Add(entry);
                else
                    links.Add(new docLink
                    {
                        sourceFile = relativeSource,
                        documentPaths = new List<documentEntry> { entry },
                        linkedAt = DateTime.UtcNow.ToString("o")
                    });

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

        // ── Header add link handlers ─────────────────────────────────────────

        private void headerAddLink(object sender, RoutedEventArgs e)
        {
            showingHeaderAddForm = !showingHeaderAddForm;
            headerAddPanel.Visibility = showingHeaderAddForm
                ? Visibility.Visible
                : Visibility.Collapsed;
            headerLinkForm = new newSourceLinkForm();
            headerAddPanel.DataContext = headerLinkForm;
        }

        private void saveHeaderLink(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"saveHeaderLink fired");
            System.Diagnostics.Debug.WriteLine($"solutionDir: {solutionDir}");
            System.Diagnostics.Debug.WriteLine($"sourceFileValue: {headerLinkForm.sourceFile}");

            string sourceFileValue = headerLinkForm.sourceFile?.Trim();
            string newPathValue = headerLinkForm.path?.Trim();
            string newUrlValue = headerLinkForm.url?.Trim();
            string newNickname = headerLinkForm.nickname?.Trim();
            string newDocType = headerLinkForm.docType?.Trim();
            string newNotes = headerLinkForm.notes?.Trim();
            bool newPrimary = headerLinkForm.isPrimary;

            if (string.IsNullOrEmpty(sourceFileValue))
            {
                MessageBox.Show("Please provide a source file path.",
                    "mortar", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(newPathValue) && string.IsNullOrEmpty(newUrlValue))
            {
                MessageBox.Show("Please provide a path or URL.",
                    "mortar", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(newPathValue) && !File.Exists(newPathValue))
            {
                MessageBox.Show($"File not found:\n{newPathValue}\n\nPlease provide a valid path.",
                    "mortar", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string path = getLinksFilePath();
            if (path == null) return;

            var links = storageService.loadLinks(path);
            if (links == null) return;

            string normalizedSource = pathHelper.normalizePath(sourceFileValue);
            string normalizedPath = string.IsNullOrEmpty(newPathValue)
                ? null
                : pathHelper.normalizePath(newPathValue);

            if (normalizedSource == null)
            {
                MessageBox.Show("Invalid source file path.",
                    "mortar", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Store source file as relative path for portability
            string relativeSource = pathHelper.makeRelativePath(solutionDir, normalizedSource);
            System.Diagnostics.Debug.WriteLine($"relativeSource: {relativeSource}");

            var existing = links.Find(l => pathHelper.pathsEqual(
                pathHelper.resolveRelativePath(solutionDir, l.sourceFile),
                normalizedSource));

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
                existing.documentPaths.Add(entry);
            else
                links.Add(new docLink
                {
                    sourceFile = relativeSource,
                    documentPaths = new List<documentEntry> { entry },
                    linkedAt = DateTime.UtcNow.ToString("o")
                });

            storageService.saveLinks(path, links);
            showingHeaderAddForm = false;
            headerAddPanel.Visibility = Visibility.Collapsed;
            headerLinkForm = new newSourceLinkForm();
            loadLinks();
        }

        private void cancelHeaderLink(object sender, RoutedEventArgs e)
        {
            showingHeaderAddForm = false;
            headerAddPanel.Visibility = Visibility.Collapsed;
            headerLinkForm = new newSourceLinkForm();
        }

        // ── Tree view helpers ────────────────────────────────────────────────

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

        private void treeViewItemSelected(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem tvi)
            {
                tvi.IsSelected = false;
                e.Handled = true;
            }
        }

        private void documentNodeClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is StackPanel panel && panel.DataContext is documentNode node)
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

        private TreeViewItem getParentTreeViewItem(DependencyObject item)
        {
            if (item == null) return null;
            var parent = VisualTreeHelper.GetParent(item);
            while (parent != null && !(parent is TreeViewItem))
                parent = VisualTreeHelper.GetParent(parent);
            return parent as TreeViewItem;
        }

        private void saveExpansionState()
        {
            expandedNodes.Clear();
            foreach (var item in getTreeViewItems(linksTree))
            {
                if (!item.IsExpanded) continue;
                if (item.DataContext is sourceFileNode sNode)
                    expandedNodes.Add(sNode.fullPath);
                else if (item.DataContext is folderNode fNode)
                    expandedNodes.Add(fNode.fullPath);
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
                if (item.DataContext is sourceFileNode sNode &&
                    expandedNodes.Contains(sNode.fullPath))
                    item.IsExpanded = true;
                else if (item.DataContext is folderNode fNode &&
                    expandedNodes.Contains(fNode.fullPath))
                    item.IsExpanded = true;

                if (item.DataContext is documentNode docNode && docNode.isEditing)
                {
                    item.IsExpanded = true;
                    var parent = getParentTreeViewItem(item);
                    if (parent != null)
                    {
                        parent.IsExpanded = true;
                        var grandParent = getParentTreeViewItem(parent);
                        if (grandParent != null)
                            grandParent.IsExpanded = true;
                    }
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
            catch { return false; }
        }

        private void dismissGitWarning(object sender, RoutedEventArgs e)
        {
            gitWarningBanner.Visibility = Visibility.Collapsed;
        }

        private void showSavedConfirmation()
        {
            savedBorder.Visibility = Visibility.Visible;
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            timer.Tick += (s, e) =>
            {
                savedBorder.Visibility = Visibility.Collapsed;
                ((System.Windows.Threading.DispatcherTimer)s).Stop();
            };
            timer.Start();
        }

        private void contextMenuDeleteSourceFile(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem &&
                menuItem.Parent is ContextMenu contextMenu &&
                contextMenu.PlacementTarget is FrameworkElement element &&
                element.Tag is string sourceFile &&
                !string.IsNullOrEmpty(sourceFile))
            {
                var result = MessageBox.Show(
                    $"Remove \"{Path.GetFileName(sourceFile)}\" and all its links from mortar?\n\nThis cannot be undone.",
                    "mortar", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                string path = getLinksFilePath();
                if (path == null) return;

                var links = storageService.loadLinks(path);
                if (links == null) return;

                links.RemoveAll(l => pathHelper.pathsEqual(l.sourceFile, sourceFile));

                storageService.saveLinks(path, links);
                loadLinks();
            }
        }
    }

    public class docTypeDisplayConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string key && mortarWindowControl.docTypeDisplayNames.ContainsKey(key))
                return mortarWindowControl.docTypeDisplayNames[key];
            return value ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string display)
            {
                foreach (var kvp in mortarWindowControl.docTypeDisplayNames)
                    if (kvp.Value == display) return kvp.Key;
            }
            return value ?? "";
        }
    }
}
