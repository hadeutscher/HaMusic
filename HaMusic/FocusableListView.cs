/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Windows;
using System.Windows.Controls;

namespace HaMusic
{
    public class FocusableListView : ListView
    {
        public static DependencyProperty FocusedItemProperty = DependencyProperty.Register("FocusedItem", typeof(object), typeof(FocusableListView), new PropertyMetadata(null, new PropertyChangedCallback(FocusedItemChanged)));

        public static void FocusedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == null)
                return;
            FocusableListView _this = (FocusableListView)d;
            _this.UpdateLayout();
            DependencyObject container = _this.ItemContainerGenerator.ContainerFromItem(e.NewValue);
            if (container != null && container is ListViewItem)
            {
                ((ListViewItem)container).Focus();
            }
        }

        public object FocusedItem
        {
            get
            {
                return GetValue(FocusedItemProperty);
            }
            set
            {
                SetValue(FocusedItemProperty, value);
            }
        }
    }
}
