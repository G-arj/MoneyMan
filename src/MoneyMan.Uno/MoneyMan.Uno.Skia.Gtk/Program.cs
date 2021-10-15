// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace MoneyMan.Uno.Skia.Gtk
{
	using System;
	using GLib;
	using global::Uno.UI.Runtime.Skia;

	internal class Program
	{
		private static void Main(string[] args)
		{
			ExceptionManager.UnhandledException += delegate(UnhandledExceptionArgs expArgs)
			{
				Console.WriteLine("GLIB UNHANDLED EXCEPTION" + expArgs.ExceptionObject.ToString());
				expArgs.ExitApplication = true;
			};

			var host = new GtkHost(() => new App(), args);

			host.Run();
		}
	}
}
