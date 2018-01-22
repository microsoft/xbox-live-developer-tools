//-----------------------------------------------------------------------
// <copyright file="RichTextBoxExtensions.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
//     Internal use only.
// </copyright>
//-----------------------------------------------------------------------

using System.Drawing;
using System.Windows.Forms;

namespace SessionHistoryViewer
{
    public static class RichTextBoxExtensions
    {
        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionBackColor = color;
            box.AppendText(text);
            box.SelectionBackColor = box.ForeColor;
        }
    }
}
