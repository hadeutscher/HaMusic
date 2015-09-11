/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using GongSolutions.Wpf.DragDrop;
using GongSolutions.Wpf.DragDrop.Utilities;
using HaMusicLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;

namespace HaMusic
{
    public class NullBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class VisibilityBindingConverter : IMultiValueConverter
    {
        public IMultiValueConverter Converter1 { get; set; }
        public IValueConverter Converter2 { get; set; }

        #region IValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            object convertedValue = Converter1.Convert(values, targetType, parameter, culture);
            return Converter2.Convert(convertedValue, targetType, parameter, culture);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>Represents a collection of <see cref="IValueConverter"/>s.</summary>
    public sealed class ValueConverterCollection : Collection<IValueConverter> { }

    public class BindingConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return values.Length == 2 && values[0] is long && values[1] is long && (long)values[0] == (long)values[1];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SeekTimeConverter : IMultiValueConverter
    {
        private string TranslateTime(int time)
        {
            if (time == -1)
                time = 0;
            int secs = time % 60;
            time = time / 60;
            int mins = time % 60;
            time = time / 60;
            return time == 0 ? mins.ToString() + ":" + secs.ToString("D2") : time.ToString() + ":" + mins.ToString("D2") + ":" + secs.ToString("D2");
        }

        public object Convert(object[] values, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return values.Select(x => TranslateTime((int)x)).Aggregate((x, y) => x + " / " + y);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                          System.Globalization.CultureInfo culture)
        {
            int returnValue = 0;
            if (parameter is Type)
            {
                returnValue = (int)Enum.Parse((Type)parameter, value.ToString());
            }
            return returnValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                              System.Globalization.CultureInfo culture)
        {
            Enum enumValue = default(Enum);
            if (parameter is Type)
            {
                enumValue = (Enum)Enum.Parse((Type)parameter, value.ToString());
            }
            return enumValue;
        }
    }

    public class ListDropHandler : IDropTarget
    {
        MainWindow parent;

        public ListDropHandler(MainWindow parent)
        {
            this.parent = parent;
        }

        public void DragOver(IDropInfo dropInfo)
        {
            if ((dropInfo.Data is PlaylistItem || dropInfo.Data is IEnumerable<PlaylistItem>) && dropInfo.TargetItem != null)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Move;
            }
            else if (dropInfo.Data is DataObject && ((DataObject)dropInfo.Data).GetDataPresent(DataFormats.FileDrop))
            {
                dropInfo.DropTargetAdorner = null;
                dropInfo.Effects = DragDropEffects.Copy;
            }
            else if (dropInfo.Data is string || dropInfo.Data is IEnumerable<string>)
            {
                dropInfo.DropTargetAdorner = null;
                dropInfo.Effects = DragDropEffects.Copy;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            if ((dropInfo.Data is PlaylistItem || dropInfo.Data is IEnumerable<PlaylistItem>) && dropInfo.TargetItem != null)
            {
                parent.DragMoveItems(dropInfo);
            }
            else if (dropInfo.Data is DataObject && ((DataObject)dropInfo.Data).GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])((DataObject)dropInfo.Data).GetData(DataFormats.FileDrop);
                parent.AddSongs(files);
            }
            else if (dropInfo.Data is string || dropInfo.Data is IEnumerable<string>)
            {
                string[] files = dropInfo.Data is string ? new string[] { (string)dropInfo.Data } : ((IEnumerable<string>)dropInfo.Data).ToArray();
                parent.AddSongs(files);
            }
        }
    }

    public class TabHeaderDropHandler : IDropTarget
    {
        Controls data;

        public TabHeaderDropHandler(Controls data)
        {
            this.data = data;
        }

        public void DragOver(IDropInfo dropInfo)
        {
            data.SelectedPlaylist = (Playlist)((FrameworkElement)dropInfo.VisualTarget).DataContext;
        }

        public void Drop(IDropInfo dropInfo)
        {
        }
    }

    public class MvvmControl : PropertyNotifierBase
    {
        private string _label;
        public string Label
        {
            get { return _label; }
            set { SetField(ref _label, value); }
        }

        private ICommand _command;
        public ICommand Command
        {
            get { return _command; }
            set { SetField(ref _command, value); }
        }

        private Uri _largeImage;
        public Uri LargeImage
        {
            get { return _largeImage; }
            set { SetField(ref _largeImage, value); }
        }
    }

    public class Controls : PropertyNotifierBase
    {
        private readonly static Uri playUri = new Uri("/HaMusic;component/Images/play.png", UriKind.Relative);
        private readonly static Uri pauseUri = new Uri("/HaMusic;component/Images/pause.png", UriKind.Relative);
        private MainWindow parent;

        public Controls(MainWindow parent)
        {
            this.parent = parent;
            ldh = new ListDropHandler(parent);
            thdh = new TabHeaderDropHandler(this);

            PropertyChanged += Controls_PropertyChanged;
        }

        private ICommand _connectCommand;
        public ICommand ConnectCommand
        {
            get
            {
                return _connectCommand ?? (_connectCommand = new RelayCommand(delegate { parent.ConnectExecuted(); }));
            }
        }

        private ICommand _openCommand;
        public ICommand OpenCommand
        {
            get
            {
                return _openCommand ?? (_openCommand = new RelayCommand(delegate { parent.OpenExecuted(); }, delegate { return Enabled; }));
            }
        }

        private ICommand _playpauseCommand;
        public ICommand PlayPauseCommand
        {
            get
            {
                return _playpauseCommand ?? (_playpauseCommand = new RelayCommand(delegate { parent.PlayPauseExecuted(); }, delegate { return Enabled; }));
            }
        }

        private ICommand _stopCommand;
        public ICommand StopCommand
        {
            get
            {
                return _stopCommand ?? (_stopCommand = new RelayCommand(delegate { parent.StopExecuted(); }, delegate { return Enabled; }));
            }
        }

        private ICommand _nextCommand;
        public ICommand NextCommand
        {
            get
            {
                return _nextCommand ?? (_nextCommand = new RelayCommand(delegate { parent.NextExecuted(); }, delegate { return Enabled; }));
            }
        }

        private ICommand _newplCommand;
        public ICommand NewPlaylistCommand
        {
            get
            {
                return _newplCommand ?? (_newplCommand = new RelayCommand(delegate { parent.NewPlaylistExecuted(); }, delegate { return Enabled; }));
            }
        }

        private ICommand _idxsetCommand;
        public ICommand IndexSettingsCommand
        {
            get
            {
                return _idxsetCommand ?? (_idxsetCommand = new RelayCommand(delegate { parent.IndexerSettingsExecuted(); }, delegate { return Enabled; }));
            }
        }

        private void Controls_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ServerDataSource" || e.PropertyName == null)
            {
                SelectedPlaylist = ServerDataSource.Playlists.Count > 0 ? ServerDataSource.Playlists[0] : null;
                ServerDataSource.PropertyChanged += ServerDataSource_PropertyChanged;
                ServerDataSource_PropertyChanged(ServerDataSource, new PropertyChangedEventArgs(null));
            }
        }

        private void ServerDataSource_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Playing" || e.PropertyName == null)
            {
                PlayPauseLabel = ServerDataSource.Playing ? "Pause" : "Play";
                PlayPauseImage = ServerDataSource.Playing ? pauseUri : playUri;
            }
            if (e.PropertyName == "Mode" || e.PropertyName == null)
            {
                OnPropertyChanged("SelectedMove");
            }
        }

        private string _ppLabel = "Play";
        public string PlayPauseLabel
        {
            get
            {
                return _ppLabel;
            }
            set
            {
                SetField(ref _ppLabel, value);
            }
        }

        private Uri _ppImage = playUri;
        public Uri PlayPauseImage
        {
            get
            {
                return _ppImage;
            }
            set
            {
                SetField(ref _ppImage, value);
            }
        }

        private ListDropHandler ldh;
        public ListDropHandler ListDropHandler
        {
            get { return ldh; }
        }

        private TabHeaderDropHandler thdh;
        public TabHeaderDropHandler TabHeaderDropHandler
        {
            get { return thdh; }
        }

        public ObservableCollection<string> MoveTypes
        {
            get
            {
                return new ObservableCollection<string> { "Next", "Random", "Shuffle" };
            }
        }

        private ServerDataSource _sds = new ServerDataSource();
        public ServerDataSource ServerDataSource
        {
            get
            {
                return _sds;
            }
            set
            {
                SetField(ref _sds, value);
            }
        }

        private Playlist _pl = null;
        public Playlist SelectedPlaylist
        {
            get
            {
                return _pl;
            }
            set
            {
                SetField(ref _pl, value);
            }
        }

        private PlaylistItem _pli = null;
        public PlaylistItem SelectedPlaylistItem
        {
            get
            {
                return _pli;
            }
            set
            {
                SetField(ref _pli, value);
            }
        }

        private ObservableCollection<PlaylistItem> _plis = new ObservableCollection<PlaylistItem>();
        public ObservableCollection<PlaylistItem> SelectedPlaylistItems
        {
            get
            {
                return _plis;
            }
            set
            {
                SetField(ref _plis, value);
            }
        }

        private PlaylistItem _focusedItem = null;
        public PlaylistItem FocusedItem
        {
            get
            {
                return _focusedItem;
            }
            set
            {
                SetField(ref _focusedItem, value);
            }
        }


        private bool _enabled = false;
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                SetField(ref _enabled, value);
            }
        }

        public string SelectedMove
        {
            get
            {
                return MoveTypes[(int)ServerDataSource.Mode];
            }
            set
            {
                ServerDataSource.Mode = (HaProtoImpl.MoveType)MoveTypes.IndexOf(value);
            }
        }
    }
}
