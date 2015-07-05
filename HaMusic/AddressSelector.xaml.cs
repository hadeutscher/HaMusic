/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Microsoft.TeamFoundation.MVVM;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

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
            _okCommand = new RelayCommand(delegate(object o) { Confirm(); });
            _cancelCommand = new RelayCommand(delegate(object o) { Cancel(); });
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
