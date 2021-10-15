// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

#pragma warning disable SA1300 // Element should begin with upper-case letter
namespace MoneyMan.Uno.macOS
#pragma warning restore SA1300 // Element should begin with upper-case letter
{
	using AppKit;

	internal static class MainClass
	{
		private static void Main(string[] args)
		{
			NSApplication.Init();
			NSApplication.SharedApplication.Delegate = new App();
			NSApplication.Main(args);
		}
	}
}
