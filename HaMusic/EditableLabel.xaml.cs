/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HaMusic
{
    /// <summary>
    /// Interaction logic for EditableLabel.xaml
    /// </summary>
    public partial class EditableLabel : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(EditableLabel), new PropertyMetadata("Foo"));
        public static readonly DependencyProperty OpenProperty =
            DependencyProperty.Register("Open", typeof(bool), typeof(EditableLabel), new PropertyMetadata(false));

        public event TextChangedEventHandler TextChanged;

        private string initialText = "";

        public EditableLabel()
        {
            InitializeComponent();
            ((FrameworkElement)Content).DataContext = this;
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public bool Open
        {
            get { return (bool)GetValue(OpenProperty); }
            set { SetValue(OpenProperty, value); }
        }

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!Open)
            {
                SetOpen();
                e.Handled = true;
            }
        }

        public void SetOpen()
        {
            Open = true;
            textBox.SelectAll();
            initialText = Text;
        }

        public void SetClosed(bool save)
        {
            Open = false;
            if (save)
            {
                if (Text != initialText && TextChanged != null)
                    TextChanged(this, null);
            }
            else
            {
                Text = initialText;
            }
        }

        private void GlobalKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (Open && (e.Key == Key.Escape || e.Key == Key.Enter))
            {
                SetClosed(e.Key == Key.Enter);
            }

        }

        private void GloablLostFocus(object sender, RoutedEventArgs e)
        {
            if (Open)
                SetClosed(false);
        }
    }
}
