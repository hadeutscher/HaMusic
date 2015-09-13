/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using GongSolutions.Wpf.DragDrop;
using GongSolutions.Wpf.DragDrop.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace HaMusic.DragDrop
{
    public class MediaBrowserDragHandler : DefaultDragHandler
    {
        public override void StartDrag(IDragInfo dragInfo)
        {
            IEnumerable<object> objs = dragInfo.SourceItems.Cast<object>();
            dragInfo.Data = objs.Count() > 0 ? TypeUtilities.CreateDynamicallyTypedList(objs) : null;

            dragInfo.Effects = (dragInfo.Data != null) ?
                           DragDropEffects.Copy | DragDropEffects.Move :
                           DragDropEffects.None;
        }
    }
}
