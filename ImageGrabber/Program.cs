using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ImageGrabber {
  internal static class Program {
    public static DictionaryBindingList<TKey, TValue> ToBindingList<TKey, TValue>(this IDictionary<TKey, TValue> data) {
      return new DictionaryBindingList<TKey, TValue>(data);
    }

    public static void InvokeEx<T>(this T @this, Action<T> action)
      where T : Control {
      if (@this.InvokeRequired) {
        @this.Invoke(action, new object[] { @this });
      } else {
        if (!@this.IsHandleCreated)
          return;
        if (@this.IsDisposed)
          throw new ObjectDisposedException("@this is disposed.");

        action(@this);
      }
    }

    public static IAsyncResult BeginInvokeEx<T>(this T @this, Action<T> action)
      where T : Control {
      return @this.BeginInvoke((Action) (() => @this.InvokeEx(action)));
    }

    public static void EndInvokeEx<T>(this T @this, IAsyncResult result)
      where T : Control {
      @this.EndInvoke(result);
    }

    /// <summary>
    ///   The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main(string[] args) {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run(new ImageGrabberForm(args.Length == 0 ? null : args[0]));
    }
  }
}