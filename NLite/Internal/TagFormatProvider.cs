﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using NLite.Globalization;
using NLite.Collections;

namespace NLite.Text.Internal
{
    sealed class DateColonFormatProvider : ITagFormatProvider
    {
        public bool SupportColon { get { return true; } }

        public string Tag { get { return "DATE"; } }

        public string Format(string str, params string[] args)
        {
            return DateTime.Now.ToString(str);
        }
    }

    sealed class DateFormatProvider : ITagFormatProvider
    {
        public bool SupportColon { get { return false; } }

        public string Tag { get { return "DATE"; } }

        public string Format(string str, params string[] args)
        {
            return DateTime.Today.ToShortDateString();
        }
    }

    sealed class TimeFormatProvider : ITagFormatProvider
    {
        public bool SupportColon { get { return false; } }
        public string Tag { get { return "TIME"; } }
        public string Format(string str, params string[] args)
        {
            return DateTime.Today.ToShortTimeString();
        }
    }

    sealed class ProductNameFormatProvider : ITagFormatProvider
    {
        public bool SupportColon { get { return false; } }
        public string Tag { get { return "PRODUCTNAME"; } }

        public string Format(string str, params string[] args)
        {
            return NLiteEnvironment.ProductName;
        }
    }

    sealed class GuidFormateProvider : ITagFormatProvider
    {
        public bool SupportColon { get { return false; } }
        public string Tag { get { return "GUID"; } }

        public string Format(string str, params string[] args)
        {
            return Guid.NewGuid().ToString();
        }
    }

    sealed class SdkPathFormateProvider : ITagFormatProvider
    {
        public bool SupportColon { get { return false; } }
        public string Tag { get { return "SDK_PATH"; } }

        public string Format(string str, params string[] args)
        {
            return NLiteEnvironment.SDK_Path;
        }
    }

    sealed class EnvironmentVariableFormatProvider : ITagFormatProvider
    {
        public bool SupportColon { get { return true; } }
        public string Tag { get { return "ENV"; } }

        public string Format(string str, params string[] args)
        {
            return Environment.GetEnvironmentVariable(str);
        }
    }

    //sealed class ResourceFormatProvider : ITagFormatProvider
    //{
    //    public bool SupportColon { get { return true; } }
    //    public string Tag { get { return "RES"; } }

    //    public string Format(string str, params string[] args)
    //    {
    //        var strNew = ResourceRepository.Strings.Get(str);
    //        if (args != null && args.Length > 0 && strNew.HasValue())
    //        {
    //            var agrArray = args.Cast<object>().ToArray();
    //            strNew = strNew.Format(agrArray);//string.Format(strNew, agrArray);
    //        }
    //        return strNew;
    //    }
    //}

    /// <summary>
    /// ${property:PropertyName}
    /// ${property:PropertyName??DefaultValue}
    /// </summary>
    sealed class PropertyFormatProvider : ITagFormatProvider
    {
        public bool SupportColon { get { return true; } }
        public string Tag { get { return "PROPERTY"; } }

        public string Format(string str, params string[] args)
        {
            string defaultValue = "";
            int pos = str.LastIndexOf("??", StringComparison.Ordinal);
            if (pos >= 0)
            {
                defaultValue = str.Substring(pos + 2);
                str = str.Substring(0, pos);
            }
            pos = str.IndexOf('/');
            if (pos >= 0)
            {
                PropertySet properties = PropertyManager.Instance.Properties.Get(str.Substring(0, pos), new PropertySet());
                str = str.Substring(pos + 1);
                pos = str.IndexOf('/');
                while (pos >= 0)
                {
                    properties = properties.Get(str.Substring(0, pos), new PropertySet());
                    str = str.Substring(pos + 1);
                }
                return properties.Get(str, defaultValue);
            }
            else
            {
                return PropertyManager.Instance.Properties.Get(str, defaultValue);
            }
        }
    }
}
