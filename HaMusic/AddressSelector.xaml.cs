/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Windows;
using System.Windows.Input;

namespace HaMusic
{
    /// <summary>
    /// Interaction logic for AddressSelector.xaml
    /// </summary>
    public partial class AddressSelector : Window
    {
        public string Result = "";

        public AddressSelector()
        {
            InitializeComponent();
            _okCommand = new RelayCommand(delegate { Confirm(); });
            _cancelCommand = new RelayCommand(delegate { Cancel(); });
            addressBox.Text = Properties.Settings.Default.lastAddr;
            DataContext = this;
            addressBox.Focus();
        }

        public void Confirm()
        {
            Properties.Settings.Default.lastAddr = addressBox.Text;
            Properties.Settings.Default.Save();
            Result = addressBox.Text;
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
            get { return _okCommand; }
        }

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return _cancelCommand; }
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
