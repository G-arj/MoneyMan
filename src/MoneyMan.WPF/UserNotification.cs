﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Windows;
using Nerdbank.MoneyManagement.ViewModels;

namespace MoneyMan;

internal class UserNotification : IUserNotification
{
	private readonly Window mainWindow;

	internal UserNotification(Window mainWindow)
	{
		this.mainWindow = mainWindow;
	}

	internal string MessageBoxTitle => "MoneyMan";

	public async Task<IUserNotification.UserAction> AskOrCancelAsync(string text, IUserNotification.UserAction defaultButton, CancellationToken cancellationToken)
	{
		return await this.mainWindow.Dispatcher.InvokeAsync(delegate
		{
			MessageBoxResult defaultResult = defaultButton switch
			{
				IUserNotification.UserAction.Yes => MessageBoxResult.Yes,
				IUserNotification.UserAction.No => MessageBoxResult.No,
				IUserNotification.UserAction.Cancel => MessageBoxResult.Cancel,
				_ => throw new ArgumentException("Unsupported default action", nameof(defaultButton)),
			};
			MessageBoxResult result = MessageBox.Show(
				this.mainWindow,
				text,
				this.MessageBoxTitle,
				MessageBoxButton.YesNoCancel,
				MessageBoxImage.Question,
				defaultResult);
			return result switch
			{
				MessageBoxResult.Yes => IUserNotification.UserAction.Yes,
				MessageBoxResult.No => IUserNotification.UserAction.No,
				MessageBoxResult.Cancel => IUserNotification.UserAction.Cancel,
				_ => throw new InvalidOperationException("Unsupported dialog result: " + result),
			};
		});
	}

	public async Task<bool> ConfirmAsync(string text, bool defaultConfirm, CancellationToken cancellationToken)
	{
		return await this.mainWindow.Dispatcher.InvokeAsync(delegate
		{
			return MessageBox.Show(this.mainWindow, text, this.MessageBoxTitle, MessageBoxButton.OKCancel, MessageBoxImage.Warning, defaultConfirm ? MessageBoxResult.OK : MessageBoxResult.Cancel) == MessageBoxResult.OK;
		});
	}

	public async Task NotifyAsync(string text, CancellationToken cancellationToken)
	{
		await this.mainWindow.Dispatcher.InvokeAsync(delegate
		{
			MessageBox.Show(this.mainWindow, text);
		});
	}

	public async Task PresentAsync(IPresentedWindowViewModel viewModel, CancellationToken cancellationToken = default)
	{
		await this.mainWindow.Dispatcher.InvokeAsync(delegate
		{
			Window window = viewModel switch
			{
				PickerWindowViewModel picker => new PickerWindow(picker) { Owner = this.mainWindow },
				_ => throw new NotSupportedException("Unrecognized view model type: " + viewModel.GetType().FullName),
			};
			viewModel.Closing += (s, e) => window.Close();
			bool cancelled = false;
			using CancellationTokenRegistration ctr = cancellationToken.Register(delegate
			{
				window.Close();
				cancelled = true;
			});
			window.ShowDialog();
			if (cancelled)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
		});
	}
}
