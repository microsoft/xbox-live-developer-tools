// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SessionHistoryViewer
{
    using System;
    using Microsoft.Win32;

    public class UserSettings
    {
        public UserSettings()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft", true);
            this.SessionHistoryKey = key.CreateSubKey("SessionHistoryViewer");
        }

        private RegistryKey SessionHistoryKey { get; set; }

        public string Sandbox
        {
            get
            {
                return (string)this.SessionHistoryKey.GetValue("sandbox", string.Empty);
            }

            set
            {
                this.SessionHistoryKey.SetValue("sandbox", value);
            }
        }

        public string Scid
        {
            get
            {
                return (string)this.SessionHistoryKey.GetValue("scid", string.Empty);
            }

            set
            {
                this.SessionHistoryKey.SetValue("scid", value);
            }
        }

        public string TemplateName
        {
            get
            {
                return (string)this.SessionHistoryKey.GetValue("template", string.Empty);
            }

            set
            {
                this.SessionHistoryKey.SetValue("template", value);
            }
        }

        public string QueryKey
        {
            get
            {
                return (string)this.SessionHistoryKey.GetValue("queryKey", string.Empty);
            }

            set
            {
                this.SessionHistoryKey.SetValue("queryKey", value);
            }
        }

        public int QueryType
        {
            get
            {
                return (int)this.SessionHistoryKey.GetValue("queryType", 0);
            }

            set
            {
                this.SessionHistoryKey.SetValue("queryType", value);
            }
        }

        public string ListView1ColumnWidths
        {
            get
            {
                return (string)this.SessionHistoryKey.GetValue("lv1Colums", "231,231,55,140,65,250");
            }

            set
            {
                this.SessionHistoryKey.SetValue("lv1Colums", value);
            }
        }

        public string ListView2ColumnWidths
        {
            get
            {
                return (string)this.SessionHistoryKey.GetValue("lv2Colums", "50,120,120,80,100,250,450");
            }

            set
            {
                this.SessionHistoryKey.SetValue("lv2Colums", value);
            }
        }

        public bool ShowLocalTime
        {
            get
            {
                try
                {
                    string valueAsBool = (string)this.SessionHistoryKey.GetValue("showLocalTime", "true");
                    return bool.Parse(valueAsBool);
                }
                catch (FormatException)
                {
                    return true;
                }
            }

            set
            {
                this.SessionHistoryKey.SetValue("showLocalTime", value);
            }
        }

        public int AccountSource
        {
            get
            {
                return (int)this.SessionHistoryKey.GetValue("accountSource", 0);
            }

            set
            {
                this.SessionHistoryKey.SetValue("accountSource", value);
            }
        }
    }
}
