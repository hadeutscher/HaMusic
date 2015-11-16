/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusic.DragDrop;
using HaMusic.Wpf;
using HaMusicLib;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;

namespace HaMusic
{
    public class Data : PropertyNotifierBase
    {
        private readonly static Uri playUri = new Uri("/HaMusic;component/Images/play.png", UriKind.Relative);
        private readonly static Uri pauseUri = new Uri("/HaMusic;component/Images/pause.png", UriKind.Relative);
        private MainWindow parent;

        public Data(MainWindow parent)
        {
            this.parent = parent;
            PropertyChanged += Controls_PropertyChanged;
            BindingOperations.SetBinding(parent, MainWindow.SelectedPlaylistItemsProperty, new Binding("SelectedPlaylist.PlaylistItems") { Source = this });
        }

        private ICommand _connectCommand;
        public ICommand ConnectCommand
        {
            get
            {
                return _connectCommand ?? (_connectCommand = new RelayCommand(delegate { parent.ConnectExecuted(); }));
            }
        }

        private ICommand _showPlayingCommand;
        public ICommand ShowPlayingCommand
        {
            get
            {
                return _showPlayingCommand ?? (_showPlayingCommand = new RelayCommand(delegate { parent.BringSelectedItemIntoView(); }));
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

        private SortedDragHandler<PlaylistItem> _playlistDragHandler;
        public SortedDragHandler<PlaylistItem> PlaylistDragHandler
        {
            get { return _playlistDragHandler ?? (_playlistDragHandler = new SortedDragHandler<PlaylistItem>(parent, MainWindow.SelectedPlaylistItemsProperty)); }
        }

        private PlaylistDropHandler _playlistDropHandler;
        public PlaylistDropHandler PlaylistDropHandler
        {
            get { return _playlistDropHandler ?? (_playlistDropHandler = new PlaylistDropHandler(parent, this)); }
        }

        private TabHeaderDropHandler _tabHeaderDropHandler;
        public TabHeaderDropHandler TabHeaderDropHandler
        {
            get { return _tabHeaderDropHandler ?? (_tabHeaderDropHandler = new TabHeaderDropHandler(this)); }
        }

        public class MoveType : PropertyNotifierBase
        {
            public string Name { get; }
            public HaProtoImpl.MoveType Type { get; }

            public MoveType(string name, HaProtoImpl.MoveType type)
            {
                this.Name = name;
                this.Type = type;
            }
        }

        private ObservableCollection<MoveType> _moveTypes;
        public ObservableCollection<MoveType> MoveTypes
        {
            get
            {
                return _moveTypes ?? (_moveTypes = new ObservableCollection<MoveType> {
                    new MoveType("Next", HaProtoImpl.MoveType.NEXT),
                    new MoveType("Random", HaProtoImpl.MoveType.RANDOM),
                    new MoveType("Shuffle", HaProtoImpl.MoveType.SHUFFLE)
                });
            }
        }

        private ServerDataSource _sds;
        public ServerDataSource ServerDataSource
        {
            get
            {
                return _sds ?? (_sds = new ServerDataSource());
            }
            set
            {
                SetField(ref _sds, value);
            }
        }

        private Playlist _pl;
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

        private PlaylistItem _pli;
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

        private ObservableCollection<PlaylistItem> _plis;
        public ObservableCollection<PlaylistItem> SelectedPlaylistItems
        {
            get
            {
                return _plis ?? (_plis = new ObservableCollection<PlaylistItem>());
            }
            set
            {
                SetField(ref _plis, value);
            }
        }

        private PlaylistItem _focusedItem;
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

        private PlaylistItem _itemInView;
        public PlaylistItem ItemInView
        {
            get
            {
                return _itemInView;
            }
            set
            {
                // Force notify even if field is equal
                SetFieldAndNotify(ref _itemInView, value);
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

        private bool _isFinding = false;
        public bool IsFinding
        {
            get
            {
                return _isFinding;
            }
            set
            {
                SetField(ref _isFinding, value);
            }
        }

        public MoveType SelectedMove
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
