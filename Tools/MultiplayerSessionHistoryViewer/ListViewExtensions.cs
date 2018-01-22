//-----------------------------------------------------------------------
// <copyright file="ListViewExtensions.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
//     Internal use only.
// </copyright>
//-----------------------------------------------------------------------

using System.Windows.Forms;

namespace SessionHistoryViewer
{
    public static class ListViewExtensions
    {
        public static void AddColumn(this ListView lv, string columnHeaderText, string[] headerWidths, int columnIndex, int defaultWidth)
        {
            int columnHeaderWidth = defaultWidth;

            if (headerWidths.Length > columnIndex)
            {
                columnHeaderWidth = int.Parse(headerWidths[columnIndex]);
            }

            lv.Columns.Add(columnHeaderText, columnHeaderWidth);
        }
    }
}