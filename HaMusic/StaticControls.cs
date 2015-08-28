/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace HaMusic
{
    public abstract class PropertyNotifierBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected bool SetField<T>(ref T field, T value, string name)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(name);
            return true;
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
        private Uri playUri = new Uri("/HaMusic;component/Images/play.png", UriKind.Relative);
        private Uri pauseUri = new Uri("/HaMusic;component/Images/pause.png", UriKind.Relative);

        public Controls(MainWindow parent)
        {
            _songs = new ObservableCollection<string>();
            _connect = new MvvmControl()
            {
                Label = "Connect",
                Command = new RelayCommand(delegate { parent.ConnectExecuted(); }),
                LargeImage = new Uri("/HaMusic;component/Images/connect.png", UriKind.Relative)
            };
            _open = new MvvmControl()
            {
                Label = "Open",
                Command = new RelayCommand(delegate { parent.OpenExecuted(); }),
                LargeImage = new Uri("/HaMusic;component/Images/open.png", UriKind.Relative)
            };
            _clear = new MvvmControl()
            {
                Label = "Clear",
                Command = new RelayCommand(delegate { parent.ClearExecuted(); }),
                LargeImage = new Uri("/HaMusic;component/Images/clear.png", UriKind.Relative)
            };
            _playPause = new MvvmControl()
            {
                Label = "Play/Pause",
                Command = new RelayCommand(delegate { parent.PlayPauseExecuted(); }),
                LargeImage = playUri
            };
            _stop = new MvvmControl()
            {
                Label = "Stop",
                Command = new RelayCommand(delegate { parent.StopExecuted(); }),
                LargeImage = new Uri("/HaMusic;component/Images/stop.png", UriKind.Relative)
            };
            _next = new MvvmControl()
            {
                Label = "Next",
                Command = new RelayCommand(delegate { parent.NextExecuted(); }),
                LargeImage = new Uri("/HaMusic;component/Images/next.png", UriKind.Relative)
            };
        }

        private MvvmControl _open;
        public MvvmControl Open
        {
            get { return _open; }
        }

        private MvvmControl _connect;
        public MvvmControl Connect
        {
            get { return _connect; }
        }

        private MvvmControl _clear;
        public MvvmControl Clear
        {
            get { return _clear; }
        }

        private MvvmControl _playPause;
        public MvvmControl PlayPause
        {
            get { return _playPause; }
        }

        private MvvmControl _stop;
        public MvvmControl Stop
        {
            get { return _stop; }
        }

        private MvvmControl _next;
        public MvvmControl Next
        {
            get { return _next; }
        }

        private ObservableCollection<string> _songs;
        public ObservableCollection<string> Songs
        {
            get { return _songs; }
        }

        private bool _playing = false;
        public bool Playing
        {
            get 
            { 
                return _playing;
            }
            set 
            { 
                SetField(ref _playing, value, "Playing");
                PlayPause.Label = _playing ? "Pause" : "Play";
                PlayPause.LargeImage = _playing ? pauseUri : playUri;
            }
        }

        public ObservableCollection<string> MoveTypes
        {
            get
            {
                return new ObservableCollection<string> { "Next", "Random", "Shuffle" };
            }
        }

        int _selectedMove = 0;
        public int SelectedMove
        {
            get
            {
                return _selectedMove;
            }
            set
            {
                SetField(ref _selectedMove, value, "SelectedMove");
            }
        }
    }
}
