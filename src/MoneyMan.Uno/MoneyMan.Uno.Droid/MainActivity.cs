// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace MoneyMan.Uno.Droid
{
	using Android.App;
	using Android.Content.PM;
	using Android.OS;
	using Android.Views;
	using Android.Widget;

	[Activity(
			MainLauncher = true,
			ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
			WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden)]
	public class MainActivity : Windows.UI.Xaml.ApplicationActivity
	{
	}
}
