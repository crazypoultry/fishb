/*
Copyright 2012 HighVoltz

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Documents;
using System.Drawing;
using System.Runtime.InteropServices;

namespace FishBot
{
    public class Log
    {
        private static readonly string LogPath;

        private static int _lineCount;

        public static int LineCount
        {
            get
            {
                return _lineCount;
            }
        }

        static Log()
        {
            string logFolder = Path.Combine(ApplicationPath, "Logs");
            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);
            LogPath = Path.Combine(logFolder, string.Format("Log[{0:yyyy-MM-dd_hh-mm-ss}].txt", DateTime.Now));
        }

        public static string ApplicationPath
        {
            get { return Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName); }
        }

        public static void Write(string format, params object[] args)
        {
            Write(Color.Black, format, args);
        }

        public static void Err(string format, params object[] args)
        {
            Write(Color.Red, format, args);
        }

        public static void Debug(string format, params object[] args)
        {
            Debug(Color.Black, format, args);
        }

        public static void Write(Color color, string format, params object[] args)
        {
            if (MainWindow.Instance == null)
                return;

            MainWindow.Instance.Invoke(
                new Action(() =>
                {
                    InternalWrite(color, string.Format(format, args));
                    WriteToLog(format, args);
                }));
        }

        //public static void Write(Color hColor, string header, Color mColor, string format, params object[] args)
        //{
        //    if (MainWindow.Instance == null)
        //        return;
        //    if (Thread.CurrentThread == MainWindow.Instance.Dispatcher.Thread)
        //    {
        //        InternalWrite(hColor, header, mColor, string.Format(format, args));
        //        WriteToLog(header + format, args);
        //    }
        //    else
        //    {
        //        MainWindow.Instance.Dispatcher.Invoke(
        //            new Action(() =>
        //            {
        //                InternalWrite(hColor, header, mColor, string.Format(format, args));
        //                WriteToLog(header + format, args);
        //            }));
        //    }
        //}

        // same Write. might use a diferent tab someday.

        public static void Debug(Color color, string format, params object[] args)
        {
            if (MainWindow.Instance == null)
                return;

            MainWindow.Instance.BeginInvoke(
                new Action(() =>
                {
                    InternalWrite(color, string.Format(format, args));
                    WriteToLog(format, args);
                }));
        }

        private static void InternalWrite(Color color, string text)
        {
            try
            {
                RichTextBox rtb = MainWindow.Instance.LogTextBox;

                Int32 maxsize = 10000;
                Int32 dropsize = maxsize / 100; // maxsize / 4;

                if (rtb.Text.Length > maxsize)
                {
                    // this method preserves the text colouring
                    // find the first end-of-line past the endmarker

                    Int32 endmarker = rtb.Text.IndexOf('\n', dropsize) + 1;
                    if (endmarker < dropsize)
                        endmarker = dropsize;

                    rtb.Select(0, endmarker);
                    rtb.SelectedText = "";
                }

                _lineCount = rtb.Lines.Length;

                rtb.SelectionStart = rtb.Text.Length;
                rtb.SelectionLength = 0;
                rtb.SelectionColor = color;
                rtb.AppendText(string.Format("[{0:T}] {1}\r", DateTime.Now, text));

                rtb.ClearUndo();

                ScrollToBottom(rtb);
            }
            catch
            {
            }
        }

        //private static void InternalWrite(Color headerColor, string header, Color msgColor, string format, params object[] args)
        //{
        //    try
        //    {
        //        RichTextBox rtb = MainWindow.Instance.LogTextBox;
        //        Color headerColorMedia = Color.FromArgb(headerColor.A, headerColor.R, headerColor.G, headerColor.B);
        //        Color msgColorMedia = Color.FromArgb(msgColor.A, msgColor.R, msgColor.G, msgColor.B);

        //        var headerTr = new TextRange(rtb.Document.ContentEnd, rtb.Document.ContentEnd)
        //        {
        //            Text = string.Format("[{0:T}] {1}", DateTime.Now, header)
        //        };
        //        headerTr.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(headerColorMedia));

        //        var messageTr = new TextRange(rtb.Document.ContentEnd, rtb.Document.ContentEnd);
        //        string msg = String.Format(format, args);
        //        messageTr.Text = msg + '\r';
        //        messageTr.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(msgColorMedia));
        //        rtb.ScrollToEnd();
        //    }
        //    catch
        //    {
        //    }
        //}

        public static void WriteToLog(string format, params object[] args)
        {
            try
            {
                using (var logStringWriter = new StreamWriter(LogPath, true))
                {
                    logStringWriter.WriteLine(string.Format("[" + DateTime.Now.ToString(CultureInfo.InvariantCulture) + "] " + format, args));
                }
            }
            catch
            {
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(System.IntPtr hWnd, int wMsg, System.IntPtr wParam, System.IntPtr lParam);

        private const int WM_VSCROLL = 0x115;
        private const int SB_BOTTOM = 7;

        /// <summary>
        /// Scrolls the vertical scroll bar of a multi-line text box to the bottom.
        /// </summary>
        /// <param name="tb">The text box to scroll</param>
        private static void ScrollToBottom(System.Windows.Forms.RichTextBox tb)
        {
            if (System.Environment.OSVersion.Platform != System.PlatformID.Unix)
                SendMessage(tb.Handle, WM_VSCROLL, new System.IntPtr(SB_BOTTOM), System.IntPtr.Zero);
        }
    }
}
