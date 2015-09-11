/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace HaMusic
{
    /// <summary>
    /// Interaction logic for IndexerSettings.xaml
    /// </summary>
    public partial class IndexerSettings : Window
    {
        private ObservableCollection<string> sourcesList;
        private bool dirty = false;
        private List<string> origSources;
        private static readonly HashSet<string> extensionWhitelist = new HashSet<string> { ".mp3", ".m4a", ".aac", ".wav", ".mp4" };

        public ObservableCollection<string> SourcesList
        {
            get
            {
                return sourcesList;
            }
        }

        public IndexerSettings()
        {
            InitializeComponent();
            if (Properties.Settings.Default.mediaSources == null)
            {
                origSources = new List<string>();
            }
            else
            {
                origSources = Properties.Settings.Default.mediaSources.Cast<string>().ToList();
            }
            sourcesList = new ObservableCollection<string>(origSources);
            this.DataContext = this;

        }

        private void SaveSources()
        {
            Properties.Settings.Default.mediaSources = new System.Collections.Specialized.StringCollection();
            Properties.Settings.Default.mediaSources.AddRange(sourcesList.ToArray());
            Properties.Settings.Default.Save();
        }

        private void RemoveSource_Click(object sender, RoutedEventArgs e)
        {
            if (sourcesListView.SelectedValue != null)
            {
                sourcesList.Remove((string)sourcesListView.SelectedValue);
                dirty = true;
            }
        }

        private async void Reindex_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("You have changed the media sources list, in order to reindex, the media source list must be saved. Continue?", "Reindex", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }
            SaveSources();
            await Reindex(sourcesList.ToList());
        }

        private async void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (dirty)
            {
                if (MessageBox.Show("You have changed the media sources list, for this to be saved, the index must be rebuilt. Continue?", "Reindex", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return;
                }
                SaveSources();
                if (await Reindex(sourcesList.ToList()))
                {
                    this.DialogResult = true;
                }
                else
                {
                    return;
                }
            }
            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        private void AddSource_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog ofd = new VistaFolderBrowserDialog() { Description = "Select Source" };
            if (ofd.ShowDialog() != true)
            {
                return;
            }
            sourcesList.Add(ofd.SelectedPath);
            dirty = true;
        }

        private async Task<bool> Reindex(List<string> sources)
        {
            IsEnabled = false;
            sourcesListView.Visibility = Visibility.Hidden;
            logBox.Visibility = Visibility.Visible;
            Closing += IndexerSettings_PreventClose;
            Exception error = null;
            try
            {
                using (StreamWriter sw = new StreamWriter(File.Create(MainWindow.defaultIndexPath)))
                {
                    foreach (string source in sources)
                    {
                        await IndexRecursive(sw, new DirectoryInfo(source));
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                Closing -= IndexerSettings_PreventClose;
                logBox.Visibility = Visibility.Hidden;
                sourcesListView.Visibility = Visibility.Visible;
                IsEnabled = true;
            }
            if (error != null)
            {
                MessageBox.Show(string.Format("{0}\r\n{1}", error.Message, error.StackTrace), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            dirty = false;
            return true;
        }

        private void IndexerSettings_PreventClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }

        private async Task IndexRecursive(StreamWriter sw, DirectoryInfo dir)
        {
            logBox.Text = "Indexing " + dir.FullName + "...";
            foreach (DirectoryInfo subdir in dir.EnumerateDirectories())
            {
                await IndexRecursive(sw, subdir);
            }
            foreach (FileInfo file in dir.EnumerateFiles())
            {
                if (extensionWhitelist.Contains(file.Extension.ToLower()))
                    await sw.WriteLineAsync(file.FullName);
            }
        }
        
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
