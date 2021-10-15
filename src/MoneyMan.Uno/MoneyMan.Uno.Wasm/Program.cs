// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace MoneyMan.Uno.Wasm
{
	using System;
	using Windows.UI.Xaml;

	public class Program
	{
		private static App app = null!;

		private static int Main(string[] args)
		{
			Windows.UI.Xaml.Application.Start(_ => app = new App());

			return 0;
		}
	}
}
