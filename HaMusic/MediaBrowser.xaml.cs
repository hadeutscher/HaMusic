/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusic.DragDrop;
using HaMusic.Wpf;
using HaMusicLib;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace HaMusic
{
    /// <summary>
    /// Interaction logic for MediaBrowser.xaml
    /// </summary>
    public partial class MediaBrowser : UserControl
    {
        public static readonly DependencyProperty SourceDataProperty =
            DependencyProperty.Register("SourceData", typeof(ObservableCollection<PlaylistItem>), typeof(MediaBrowser), new PropertyMetadata(new ObservableCollection<string>()));
        public static readonly DependencyProperty SelectedDataProperty =
            DependencyProperty.Register("SelectedData", typeof(ObservableCollection<string>), typeof(MediaBrowser), new PropertyMetadata(new ObservableCollection<string>()));

        public static readonly ObservableCollection<string> TooLong = new ObservableCollection<string> { "Too many results; please define your search better" };

        public MediaBrowser()
        {
            InitializeComponent();
            ((FrameworkElement)this.Content).DataContext = this;
            MultiBinding mb = new MultiBinding() { Converter = new MediaBrowserFilterConverter(this) };
            mb.Bindings.Add(new Binding("SourceData") { Source = this });
            mb.Bindings.Add(new Binding("textBox.Text") { Source = this });
            BindingOperations.SetBinding(this, SelectedDataProperty, mb);
        }

        public ObservableCollection<PlaylistItem> SourceData
        {
            get { return (ObservableCollection<PlaylistItem>)GetValue(SourceDataProperty); }
            set { SetValue(SourceDataProperty, value); }
        }

        public ObservableCollection<string> SelectedData
        {
            get { return (ObservableCollection<string>)GetValue(SelectedDataProperty); }
            set { SetValue(SelectedDataProperty, value); }
        }

        private SortedDragHandler<string> mediaBrowserDragHandler;
        public SortedDragHandler<string> MediaBrowserDragHandler
        {
            get { return mediaBrowserDragHandler ?? (mediaBrowserDragHandler = new SortedDragHandler<string>(this, SelectedDataProperty)); }
        }

        public delegate void ItemDoubleClickedEventHandler(string item);
        public event ItemDoubleClickedEventHandler ItemDoubleClicked;

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // This causes the binding to be recalculated, calling MediaBrowserFilterConverter which will filter according to the new text
            this.InvalidateProperty(SelectedDataProperty);
        }

        public ObservableCollection<string> FilterData(ObservableCollection<PlaylistItem> sourceData, string filter)
        {
            string[] terms = filter.ToLower().Split(' ');
            if (terms.Length > 0 && terms[0] != "")
            {
                ObservableCollection<string> results = new ObservableCollection<string>();
                foreach (PlaylistItem curr in sourceData)
                {
                    bool pass = true;
                    foreach (string term in terms)
                    {
                        if (!curr.Item.ToLower().Contains(term))
                        {
                            pass = false;
                            break;
                        }
                    }
                    if (pass)
                    {
                        results.Add(curr.Item);
                        if (results.Count > 10000)
                        {
                            return TooLong;
                        }
                    }
                }
                return new ObservableCollection<string>(results);
            }
            else
            {
                SelectedData.Clear();
                if (sourceData.Count > 10000)
                {
                    return TooLong;
                }
                else
                {
                    return new ObservableCollection<string>(sourceData.Select(x => x.Item));
                }
            }
            
        }

        private void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ItemDoubleClicked != null)
            {
                string item = ((FrameworkElement)e.OriginalSource).DataContext as string;
                if (item != null)
                {
                    ItemDoubleClicked((string)((FrameworkElement)e.OriginalSource).DataContext);
                }
            }
            e.Handled = true;
        }
    }
}
