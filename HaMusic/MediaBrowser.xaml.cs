/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusic.DragDrop;
using HaMusic.Wpf;
using HaMusicLib;
using System;
using System.Collections.Generic;
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
            DependencyProperty.Register("SourceData", typeof(Playlist), typeof(MediaBrowser));
        public static readonly DependencyProperty SelectedDataProperty =
            DependencyProperty.Register("SelectedData", typeof(ObservableCollection<PlaylistItem>), typeof(MediaBrowser));

        public static readonly ObservableCollection<PlaylistItem> TooLong = new ObservableCollection<PlaylistItem> { new PlaylistItem() { Item = "Too many results; please define your search better" } };

        public MediaBrowser()
        {
            InitializeComponent();
            ((FrameworkElement)this.Content).DataContext = this;
            MultiBinding mb = new MultiBinding() { Converter = new MediaBrowserFilterConverter(this) };
            mb.Bindings.Add(new Binding("SourceData.PlaylistItems") { Source = this });
            mb.Bindings.Add(new Binding("Text") { Source = textBox });
            BindingOperations.SetBinding(this, SelectedDataProperty, mb);
        }

        public ObservableCollection<PlaylistItem> SourceData
        {
            get { return (ObservableCollection<PlaylistItem>)GetValue(SourceDataProperty); }
            set { SetValue(SourceDataProperty, value); }
        }

        public ObservableCollection<PlaylistItem> SelectedData
        {
            get { return (ObservableCollection<PlaylistItem>)GetValue(SelectedDataProperty); }
            set { SetValue(SelectedDataProperty, value); }
        }

        private SortedDragHandler<PlaylistItem> mediaBrowserDragHandler;
        public SortedDragHandler<PlaylistItem> MediaBrowserDragHandler
        {
            get { return mediaBrowserDragHandler ?? (mediaBrowserDragHandler = new SortedDragHandler<PlaylistItem>(this, SelectedDataProperty)); }
        }

        //public delegate void ItemDoubleClickedEventHandler(PlaylistItem item);
        //public event ItemDoubleClickedEventHandler ItemDoubleClicked;
        public new event EventHandler<MouseButtonEventArgs> MouseDoubleClick;

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // This causes the binding to be recalculated, calling MediaBrowserFilterConverter which will filter according to the new text
            this.InvalidateProperty(SelectedDataProperty);
        }

        public ObservableCollection<PlaylistItem> FilterData(IList<PlaylistItem> sourceData, string filter)
        {
            string[] terms = filter.ToLower().Split(' ');
            ObservableCollection<PlaylistItem> results;
            if (terms.Length > 0 && terms[0] != "")
            {
                results = new ObservableCollection<PlaylistItem>();
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
                        results.Add(curr);
                        if (results.Count > 10000)
                        {
                            return TooLong;
                        }
                    }
                }
            }
            else
            {
                if (sourceData.Count > 10000)
                {
                    return TooLong;
                }
                else
                {
                    results = new ObservableCollection<PlaylistItem>(sourceData);
                }
            }
            results.OrderBy(x => x.Item);
            return results;
        }

        private void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (MouseDoubleClick != null)
                MouseDoubleClick(sender, e);
        }

        public event RoutedEventHandler PlayItemClick;
        public event RoutedEventHandler PlayItemAndReturnClick;
        public event RoutedEventHandler PlayItemNextClick;
        public event RoutedEventHandler PlayItemNextAndReturnClick;

        private void MenuItem_PlayItem(object sender, RoutedEventArgs e)
        {
            if (PlayItemClick != null)
                PlayItemClick(sender, e);
        }

        private void MenuItem_PlayItemAndReturn(object sender, RoutedEventArgs e)
        {
            if (PlayItemAndReturnClick != null)
                PlayItemAndReturnClick(sender, e);
        }

        private void MenuItem_PlayItemNext(object sender, RoutedEventArgs e)
        {
            if (PlayItemNextClick != null)
                PlayItemNextClick(sender, e);
        }

        private void MenuItem_PlayItemNextAndReturn(object sender, RoutedEventArgs e)
        {
            if (PlayItemNextAndReturnClick != null)
                PlayItemNextAndReturnClick(sender, e);
        }
    }
}
