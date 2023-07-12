// ****************************************************************************
// Project:  Resource Reflector
// File:     Program.cs
// Author:   Latency McLaughlin
// Date:     04/19/2014
// ****************************************************************************

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ResourceReflector;

namespace Resource_Reflector
{
    internal static class Program
    {
        public static DictionaryBindingList<TKey, TValue> ToBindingList<TKey, TValue>(this IDictionary<TKey, TValue> data) => new(data);

        public static void InvokeEx<T>(this T @this, Action<T> action) where T : Control
        {
            if (@this.InvokeRequired)
            {
                @this.Invoke(action, @this);
            }
            else
            {
                if (!@this.IsHandleCreated)
                    return;
                if (@this.IsDisposed)
                    throw new ObjectDisposedException("@this is disposed.");
                action(@this);
            }
        }

        public static IAsyncResult BeginInvokeEx<T>(this T @this, Action<T> action) where T : Control => @this.BeginInvoke(() => @this.InvokeEx(action));

        public static void EndInvokeEx<T>(this T @this, IAsyncResult result) where T : Control => @this.EndInvoke(result);

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(args.Length == 0 ? null : args[0]));
        }
    }
}