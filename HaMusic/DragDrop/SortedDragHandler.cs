/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using GongSolutions.Wpf.DragDrop;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace HaMusic.DragDrop
{
    public class SortedDragHandler<T> : DefaultDragHandler
    {
        private DependencyObject obj;
        private DependencyProperty prop;

        public SortedDragHandler(DependencyObject obj, DependencyProperty prop)
        {
            this.obj = obj;
            this.prop = prop;
        }

        public static Dictionary<T, int> GetIndices(IEnumerable<T> list)
        {
            Dictionary<T, int> result = new Dictionary<T, int>();
            int i = 0;
            foreach (T item in list)
            {
                result.Add(item, i++);
            }
            return result;
        }

        public override void StartDrag(IDragInfo dragInfo)
        {
            List<T> items = dragInfo.SourceItems.OfType<T>().ToList();
            if (items.Count == 0)
            {
                dragInfo.Data = null;
            }
            else if (items.Count == 1)
            {
                dragInfo.Data = items;
            }
            else
            {
                // More than 1, we need to sort

                Dictionary<T, int> indices = GetIndices((IEnumerable<T>)obj.GetValue(prop));
                items.Sort((x, y) => indices[x].CompareTo(indices[y]));
                dragInfo.Data = items;
            }

            dragInfo.Effects = (dragInfo.Data != null) ?
                           DragDropEffects.Copy | DragDropEffects.Move :
                           DragDropEffects.None;
        }
    }
}
