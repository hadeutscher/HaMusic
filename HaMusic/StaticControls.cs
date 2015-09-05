/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace HaMusic
{
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

    public class MvvmControl : PropertyNotifierBase
    {
        private string _label;
        public string Label
        {
            get { return _label; }
            set { SetField(ref _label, value, "Label"); }
        }

        private ICommand _command;
        public ICommand Command
        {
            get { return _command; }
            set { SetField(ref _command, value, "Command"); }
        }

        private Uri _largeImage;
        public Uri LargeImage
        {
            get { return _largeImage; }
            set { SetField(ref _largeImage, value, "LargeImage"); }
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
                SetField(ref _ppLabel, value, "PlayPauseLabel");
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
                SetField(ref _ppImage, value, "PlayPauseImage");
            }
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
                SetField(ref _sds, value, "ServerDataSource");
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
                SetField(ref _pl, value, "SelectedPlaylist");
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
                SetField(ref _enabled, value, "Enabled");
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
