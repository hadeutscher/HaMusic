/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusic.Wpf;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;

namespace HaMusic
{
    /// <summary>
    /// Interaction logic for AddressSelector.xaml
    /// </summary>
    public partial class AddressSelector : Window
    {
        public TcpClient Result = null;
        private static DependencyProperty enabledProperty = DependencyProperty.Register("Enabled", typeof(bool), typeof(AddressSelector), new PropertyMetadata(true));

        public AddressSelector()
        {
            InitializeComponent();
            addressBox.Text = Properties.Settings.Default.lastAddr;
            DataContext = this;
            addressBox.Focus();
        }

        public async void Confirm()
        {
            Properties.Settings.Default.lastAddr = addressBox.Text;
            Properties.Settings.Default.Save();
            Result = new TcpClient();
            Enabled = false;
            try
            {
                await Result.ConnectAsync(addressBox.Text, 5151);
            }
            catch (SocketException)
            {
                MessageBox.Show("Could not connect", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                Enabled = true;
            }
            
            DialogResult = true;
            Close();
        }

        public void Cancel()
        {
            DialogResult = false;
            Close();
        }

        private ICommand _okCommand;
        public ICommand OKCommand
        {
            get { return _okCommand ?? (_okCommand = new RelayCommand(delegate { Confirm(); })); }
        }

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return _cancelCommand ?? (_cancelCommand = new RelayCommand(delegate { Cancel(); })); }
        }

        public bool Enabled
        {
            get { return (bool)GetValue(enabledProperty); }
            private set { SetValue(enabledProperty, value); }
        }

        private void Window_KeyDown(object sneder, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Cancel();
            else if (e.Key == Key.Enter)
                Confirm();
        }
    }
}
