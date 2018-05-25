// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Microsoft.Xbox.Services.DevTools.XblConfig;

    internal static class ObjectPrinter
    {
        public static string Print(object obj, IEnumerable<PropertyInfo> properties)
        {
            StringBuilder sb = new StringBuilder();
            // Get the maximum length of the property names.
            IDictionary<string, string> propertyNames = GetPropertyNames(properties);
            int longest = propertyNames.Aggregate((max, cur) => max.Value.Length > cur.Value.Length ? max : cur).Value.Length;

            string format = $"{{0, {longest}}}: {{1}}";
            foreach (PropertyInfo property in properties)
            {
                object propertyValue = property.GetValue(obj);
                string propertyString = propertyValue.ToString();
                if (propertyValue is IEnumerable && !(propertyValue is string))
                {
                    object[] propVal = ((IEnumerable)propertyValue).Cast<object>().ToArray();
                    propertyString = string.Join(",", propVal);
                }

                sb.AppendFormat(format, propertyNames[property.Name], propertyString);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public static string Print(object obj)
        {
            IEnumerable<PropertyInfo> properties = SortAndFilterProperties(obj.GetType(), false);
            return Print(obj, properties);
        }

        public static string Print(IEnumerable list)
        {
            IEnumerator enumerator = list.GetEnumerator();
            IEnumerable<PropertyInfo> properties = enumerator.MoveNext() ? SortAndFilterProperties(enumerator.Current.GetType(), true) : null;
            if (properties == null)
            {
                return string.Join(Environment.NewLine, list.Cast<string>());
            }

            return Print(list, properties);
        }

        public static string Print(IEnumerable list, IEnumerable<PropertyInfo> properties)
        {
            if (properties == null)
            {
                return string.Empty;
            }

            StringBuilder formatSB = new StringBuilder();
            StringBuilder headerFormatSB = new StringBuilder();
            int index = 0;

            IDictionary<string, string> propertyNames = GetPropertyNames(properties);
            foreach (PropertyInfo property in properties)
            {
                int maxLength = propertyNames[property.Name].Length;

                foreach (object item in list)
                {
                    int len = item.GetType().GetProperty(property.Name).GetValue(item).ToString().Length;
                    if (len > maxLength)
                    {
                        maxLength = len;
                    }
                }

                headerFormatSB.Append($"{{{index}, -{maxLength + VirtualTerminal.Underline.Length + VirtualTerminal.Reset.Length}}}  ");
                formatSB.Append($"{{{index++}, -{maxLength}}}  ");
            }

            string format = formatSB.ToString().Trim();
            string headerFormat = headerFormatSB.ToString().Trim();

            StringBuilder sb = new StringBuilder();
            string[] namesOld = propertyNames.Select(c => $"{c.Value}").ToArray();
            string[] names = propertyNames.Select(c => $"{VirtualTerminal.Underline}{c.Value}{VirtualTerminal.Reset}").ToArray();
            sb.AppendFormat(headerFormat, names);
            sb.AppendLine();

            foreach (object item in list)
            {
                List<object> items = new List<object>();
                foreach (PropertyInfo property in properties)
                {
                    items.Add(property.GetValue(item));
                }

                sb.AppendFormat(format, items.ToArray());
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string FormatCamelCase(string str)
        {
            return System.Text.RegularExpressions.Regex.Replace(str, "(?<=[a-z])([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
        }

        private static IDictionary<string, string> GetPropertyNames(IEnumerable<PropertyInfo> properties)
        {
            Dictionary<string, string> names = new Dictionary<string, string>();
            foreach (PropertyInfo property in properties)
            {
                DisplayAttribute display = property.GetCustomAttribute<DisplayAttribute>();
                if (display == null || string.IsNullOrEmpty(display.Name))
                {
                    names.Add(property.Name, FormatCamelCase(property.Name));
                }
                else
                {
                    names.Add(property.Name, display.Name);
                }
            }

            return names;
        }

        private static IEnumerable<PropertyInfo> SortAndFilterProperties(Type type, bool isList)
        {
            if (type.Module.ScopeName == "CommonLanguageRuntimeLibrary")
            {
                return null;
            }

            SortedDictionary<int, PropertyInfo> orderedProperties = new SortedDictionary<int, PropertyInfo>();
            List<PropertyInfo> unorderedProperties = new List<PropertyInfo>();
            
            foreach (PropertyInfo property in type.GetProperties())
            {
                DisplayAttribute displayAttribute = property.GetCustomAttribute<DisplayAttribute>();
                if (displayAttribute != null)
                {
                    if (isList)
                    {
                        if (displayAttribute.GetListOrder().HasValue && !displayAttribute.ListOmit)
                        {
                            orderedProperties.Add(displayAttribute.ListOrder, property);
                        }
                        else if (!displayAttribute.ListOmit)
                        {
                            unorderedProperties.Add(property);
                        }
                    }
                    else
                    {
                        if (displayAttribute.GetOrder().HasValue && !displayAttribute.Omit)
                        {
                            orderedProperties.Add(displayAttribute.Order, property);
                        }
                        else if (!displayAttribute.Omit)
                        {
                            unorderedProperties.Add(property);
                        }
                    }

                    continue;
                }

                unorderedProperties.Add(property);
            }

            return orderedProperties.Select(c => c.Value).Concat(unorderedProperties);
        }
    }
}
