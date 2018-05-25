// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.Runtime.InteropServices;

    internal static class VirtualTerminal
    {
        private const int StdOutputHandle = -11;
        private const uint EnableVirtualTerminalProcessing = 4;

        public const string Underline = "\x1B[4m";
        public const string Reset = "\x1B[0m";

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        /// <summary>
        /// Enables virtual terminal processing.
        /// </summary>
        /// <remarks>
        /// See https://docs.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences for more info.
        /// </remarks>
        public static void Enable()
        {
            IntPtr handle = GetStdHandle(StdOutputHandle);
            GetConsoleMode(handle, out uint mode);
            mode |= EnableVirtualTerminalProcessing;
            SetConsoleMode(handle, mode);
        }
    }
}
