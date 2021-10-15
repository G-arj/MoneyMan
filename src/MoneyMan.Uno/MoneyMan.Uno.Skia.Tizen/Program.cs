// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace MoneyMan.Uno.Skia.Tizen
{
	using global::Tizen.Applications;
	using global::Uno.UI.Runtime.Skia;

	internal class Program
	{
		private static void Main(string[] args)
		{
			var host = new TizenHost(() => new MoneyMan.Uno.App(), args);
			host.Run();
		}
	}
}
