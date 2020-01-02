﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tono;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace tSecretUwp
{
    public class Boolean2VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return DbUtil.ToBoolean(value, true) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility vis)
            {
                return vis == Visibility.Visible;
            } else
            {
                return Visibility.Visible;
            }
        }
    }
}