// ****************************************************************************
// * Project:  Resource Reflector
// * File:     Program.cs
// * Author:   Latency McLaughlin
// * Date:     04/19/2014
// ****************************************************************************

using System;
using System.Windows.Forms;
using AssemblyOfImages;

namespace ResourceReflector {
  internal static class Program {
    /// <summary>
    ///   The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main(string[] args) {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run(new Form1());
    }
  }
}