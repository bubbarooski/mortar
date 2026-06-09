using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using mortar.models;
using System.Windows.Media;

namespace mortar.windows
{
    public partial class mortarWindowControl : UserControl
    {
        public mortarWindowControl()
        {
            InitializeComponent();
        }

        private string _solutionDir;

        public void setSolutionDir(string solutionDir)
        {
            // System.Windows.MessageBox.Show($"setSolutionDir called with: {solutionDir}");
            _solutionDir = solutionDir;
            if (solutionDir != null)
                loadLinks();
            else
                LinksTree.ItemsSource = null;
        }

        private string getLinksFilePath()
        {
            if (string.IsNullOrEmpty(_solutionDir))
                return null;

            return System.IO.Path.Combine(_solutionDir, "docLinks.json");
        }

        private void loadLinks()
        {
            string path = getLinksFilePath();

            if (path == null)
            {
                MessageBox.Show("Solution directory not found. Try closing and reopening the mortar window.");
                return;
            }

            if (!File.Exists(path))
            {
                LinksTree.ItemsSource = null;
                MessageBox.Show($"doclinks.json not found at: {path}");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var links = JsonConvert.DeserializeObject<List<docLink>>(json);

                if (links == null || links.Count == 0)
                {
                    LinksTree.ItemsSource = null;
                    return;
                }

                var nodes = buildTree(links);
                LinksTree.ItemsSource = nodes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading links: {ex.Message}");
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
                    fullPath = link.sourceFile
                };

                foreach (var doc in link.documentPaths)
                {
                    bool outOfDate = false;
                    if (doc.outOfDateDetection && !string.IsNullOrEmpty(doc.path))
                        outOfDate = checkOutOfDate(link.sourceFile, doc.path);

                    string display = !string.IsNullOrWhiteSpace(doc.nickname)
                        ? doc.nickname
                        : !string.IsNullOrEmpty(doc.path)
                            ? Path.GetFileName(doc.path)
                            : doc.url ?? "unnamed";

                    sourceNode.documents.Add(new documentNode
                    {
                        displayName = display,
                        fullPath = doc.path,
                        url = doc.url,
                        docType = doc.docType,
                        notes = doc.notes,
                        isPrimary = doc.isPrimary,
                        isOutOfDate = outOfDate
                    });
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

        private void refresh_Click(object sender, RoutedEventArgs e)
        {
            loadLinks();
        }

        private void documentNode_Clicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is StackPanel panel)
            {
                // Deselect the tree item immediately
                if (panel.TemplatedParent is TreeViewItem tvi)
                    tvi.IsSelected = false;

                // Walk up the visual tree to find the TreeViewItem
                var item = getParentTreeViewItem(panel);
                if (item != null)
                    item.IsSelected = false;

                if (panel.DataContext is documentNode node)
                {
                    if (File.Exists(node.fullPath))
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{node.fullPath}\"");
                    else
                        MessageBox.Show($"File not found:\n{node.fullPath}",
                            "mortar", MessageBoxButton.OK, MessageBoxImage.Warning);
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
    }
}