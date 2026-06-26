using System;
using System.Collections.Generic;
using System.Windows.Data;
using System.Globalization;
using mortar.windows;

namespace mortar.models
{
    public class docTypeDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string key && mortarWindowControl.docTypeDisplayNames.ContainsKey(key))
                return mortarWindowControl.docTypeDisplayNames[key];
            return value ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string display)
            {
                foreach (var kvp in mortarWindowControl.docTypeDisplayNames)
                    if (kvp.Value == display) return kvp.Key;
            }
            return value ?? "";
        }
    }
}