// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblTestAccount
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Reads the XUIDs a people command is to act on, from the command line or from a file.
    /// </summary>
    internal static class XuidList
    {
        private const string XuidColumn = "Xuid";

        private static readonly char[] ListSeparators = { ' ', '\t', '\r', '\n', ',', ';' };

        // A Partner Center export writes a XUID as ="2814634367189023" so that spreadsheets keep it as text.
        private static readonly char[] ValueDecoration = { '=', '"', '\'', ' ', '\t' };

        /// <summary>
        /// Normalizes one value to a XUID.
        /// </summary>
        /// <param name="value">The value as given.</param>
        /// <param name="xuid">Receives the XUID in its canonical decimal form.</param>
        /// <returns>True when the value is a XUID.</returns>
        internal static bool TryParse(string value, out string xuid)
        {
            xuid = null;
            string trimmed = value?.Trim(ValueDecoration);

            if (!ulong.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out ulong number) || number == 0)
            {
                return false;
            }

            xuid = number.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        /// <summary>
        /// Reads the values in a file of XUIDs.
        /// </summary>
        /// <remarks>
        /// A file whose first line has a Xuid column, such as the CSV of test accounts that Partner
        /// Center exports, is read by that column. Any other file is read as a plain list of values
        /// separated by spaces, commas, semicolons or new lines.
        /// </remarks>
        /// <param name="path">The file to read.</param>
        /// <returns>The values found, not yet checked to be XUIDs.</returns>
        internal static IList<string> ReadFile(string path)
        {
            string[] lines = File.ReadAllLines(path);
            int header = Array.FindIndex(lines, line => !string.IsNullOrWhiteSpace(line));
            if (header < 0)
            {
                return new List<string>();
            }

            List<string> headings = SplitCsvLine(lines[header]);
            int column = headings.FindIndex(heading => string.Equals(heading.Trim(ValueDecoration), XuidColumn, StringComparison.OrdinalIgnoreCase));

            if (column < 0)
            {
                return lines
                    .SelectMany(line => line.Split(ListSeparators, StringSplitOptions.RemoveEmptyEntries))
                    .Select(value => value.Trim(ValueDecoration))
                    .Where(value => value.Length > 0)
                    .ToList();
            }

            return lines
                .Skip(header + 1)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(SplitCsvLine)
                .Where(cells => column < cells.Count)
                .Select(cells => cells[column].Trim(ValueDecoration))
                .Where(value => value.Length > 0)
                .ToList();
        }

        /// <summary>
        /// Splits one CSV line into its cells, honouring quoted cells and doubled quotes within them.
        /// </summary>
        private static List<string> SplitCsvLine(string line)
        {
            var cells = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (quoted)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        quoted = false;
                    }
                    else
                    {
                        cell.Append(c);
                    }
                }
                else if (c == '"')
                {
                    quoted = true;
                }
                else if (c == ',')
                {
                    cells.Add(cell.ToString());
                    cell.Clear();
                }
                else
                {
                    cell.Append(c);
                }
            }

            cells.Add(cell.ToString());
            return cells;
        }
    }
}
