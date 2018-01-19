//-----------------------------------------------------------------------
// <copyright file="UserSettings.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
//     Internal use only.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Microsoft.Win32;

namespace SessionHistoryViewer
{
    public class UserSettings
    {
        private RegistryKey SessionHistoryKey { get; set; }

        public UserSettings()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft", true);
            SessionHistoryKey = key.CreateSubKey("SessionHistoryViewer");
        }

        public string Sandbox
        {
            get
            {
                return (string)SessionHistoryKey.GetValue("sandbox", string.Empty);
            }
            set
            {
                SessionHistoryKey.SetValue("sandbox", value);
            }
        }

        public string Scid
        {
            get
            {
                return (string)SessionHistoryKey.GetValue("scid", string.Empty);
            }
            set
            {
                SessionHistoryKey.SetValue("scid", value);
            }
        }

        public string TemplateName
        {
            get
            {
                return (string)SessionHistoryKey.GetValue("template", string.Empty);
            }
            set
            {
                SessionHistoryKey.SetValue("template", value);
            }
        }


        public string QueryKey
        {
            get
            {
                return (string)SessionHistoryKey.GetValue("queryKey", string.Empty);
            }
            set
            {
                SessionHistoryKey.SetValue("queryKey", value);
            }
        }

        public int QueryType
        {
            get
            {
                return (int)SessionHistoryKey.GetValue("queryType", 0);
            }
            set
            {
                SessionHistoryKey.SetValue("queryType", value);
            }
        }

        public string ListView1ColumnWidths
        {
            get
            {
                return (string)SessionHistoryKey.GetValue("lv1Colums", "231,231,55,140,65,250");
            }
            set
            {
                SessionHistoryKey.SetValue("lv1Colums", value);
            }
        }

        public string ListView2ColumnWidths
        {
            get
            {
                return (string)SessionHistoryKey.GetValue("lv2Colums", "50,120,120,80,100,250,450");
            }
            set
            {
                SessionHistoryKey.SetValue("lv2Colums", value);
            }
        }

        public bool ShowLocalTime
        {
            get
            {
                try
                {
                    string valueAsBool = (string)SessionHistoryKey.GetValue("showLocalTime", "true");
                    return bool.Parse(valueAsBool);
                }
                catch (FormatException)
                {
                    return true;
                }
            }
            set
            {
                SessionHistoryKey.SetValue("showLocalTime", value);
            }
        }

        public int AccountSource
        {
            get
            {
                return (int)SessionHistoryKey.GetValue("accountSource", 0);
            }
            set
            {
                SessionHistoryKey.SetValue("accountSource", value);
            }
        }

    }
}
