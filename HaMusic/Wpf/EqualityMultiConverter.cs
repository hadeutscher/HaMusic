/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using System;
using System.Collections.Generic;
using System.Windows.Data;

namespace HaMusic.Wpf
{
    public class EqualityMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            bool first_long_set = false;
            long curr_long = 0;
            foreach (object val in values)
            {
                if (val is long)
                {
                    if (first_long_set)
                    {
                        if ((long)val != curr_long)
                            return false;
                    }
                    else
                    {
                        curr_long = (long)val;
                        first_long_set = true;
                    }
                }
                else if (val is HaProtoImpl.InjectionType)
                {
                    if ((HaProtoImpl.InjectionType)val == HaProtoImpl.InjectionType.INJECT_AS_IF_SONG_ENDED)
                        return false;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
