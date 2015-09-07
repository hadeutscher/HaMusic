/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HaMusic
{
    /// <summary>
    /// Interaction logic for MediaBrowser.xaml
    /// </summary>
    public partial class MediaBrowser : UserControl
    {
        public static readonly DependencyProperty SelectedDataProperty =
            DependencyProperty.Register("SelectedData", typeof(ObservableCollection<string>), typeof(MediaBrowser), new PropertyMetadata(new ObservableCollection<string>()));

        public MediaBrowser()
        {
            InitializeComponent();
            ((FrameworkElement)this.Content).DataContext = this;
        }

        private List<string> _sourceData = new List<string>();

        public List<string> SourceData
        {
            get { return _sourceData; }
            set
            {
                _sourceData = value;
                FilterData();
            }
        }

        private ObservableCollection<string> _selectedData = new ObservableCollection<string>();
        private ObservableCollection<string> SelectedData
        {
            get { return (ObservableCollection<string>)GetValue(SelectedDataProperty); }
            set { SetValue(SelectedDataProperty, value); }
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterData();
        }

        private void FilterData()
        {
            string[] terms = textBox.Text.ToLower().Split(' ');
            if (terms.Length > 0 && terms[0] != "")
            {
                List<string> results = new List<string>();
                foreach (string curr in _sourceData)
                {
                    bool pass = true;
                    foreach (string term in terms)
                    {
                        if (!curr.ToLower().Contains(term))
                        {
                            pass = false;
                            break;
                        }
                    }
                    if (pass)
                    {
                        results.Add(curr);
                        if (results.Count > 10000)
                        {
                            SelectedData = new ObservableCollection<string> { "Too many results; please define your search better" };
                            listView.IsEnabled = false;
                            return;
                        }
                    }
                }
                SelectedData = new ObservableCollection<string>(results);
            }
            else
            {
                SelectedData.Clear();
                if (_sourceData.Count > 10000)
                {
                    SelectedData = new ObservableCollection<string> { "Too many results; please define your search better" };
                    listView.IsEnabled = false;
                    return;
                }
                else
                {
                    SelectedData = new ObservableCollection<string>(_sourceData);
                }
            }
            listView.IsEnabled = true;
        }
    }
}
