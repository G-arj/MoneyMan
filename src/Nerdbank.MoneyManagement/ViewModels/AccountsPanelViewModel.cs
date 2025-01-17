﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft;
using PCLCommandBase;

namespace Nerdbank.MoneyManagement.ViewModels;

public class AccountsPanelViewModel : BindableBase
{
	private static readonly ReadOnlyCollection<EnumValueViewModel<Account.AccountType>> SharedAccountTypes = new(new EnumValueViewModel<Account.AccountType>[]
	{
		new(Account.AccountType.Banking, "Banking"),
		new(Account.AccountType.Investing, "Investing"),
	});

	private readonly DocumentViewModel documentViewModel;
	private readonly SortedObservableCollection<AccountViewModel> accounts = new(AccountSort.Instance);
	private AccountViewModel? selectedAccount;
	private IList? selectedAccounts;

	public AccountsPanelViewModel(DocumentViewModel documentViewModel)
	{
		this.AddCommand = new AddAccountCommand(this);
		this.DeleteCommand = new DeleteAccountCommand(this);
		this.documentViewModel = documentViewModel;
	}

	/// <summary>
	/// Occurs when <see cref="NewBankingAccount(string)"/> is called or the <see cref="AddCommand" /> command is invoked.
	/// </summary>
	/// <remarks>
	/// Views are expected to set focus on the Name text field in response to this event.
	/// </remarks>
	public event EventHandler? AddingNewAccount;

	public string Title => "Accounts";

	public CommandBase AddCommand { get; }

	public string AddCommandCaption => "_Add new";

	public string NameLabel => "_Name";

	public string TypeLabel => "_Type";

	public ReadOnlyCollection<EnumValueViewModel<Account.AccountType>> AccountTypes => SharedAccountTypes;

	public string IsClosedLabel => "Account closed";

	public string IsClosedExplanation => "Closed accounts do not appear in the account list and do not accrue toward your net worth.";

	/// <summary>
	/// Gets a command that deletes all accounts in the <see cref="SelectedAccounts"/> collection, if that property is set;
	/// otherwise the <see cref="SelectedAccount"/> is deleted.
	/// </summary>
	public CommandBase DeleteCommand { get; }

	public IReadOnlyList<AccountViewModel> Accounts => this.accounts;

	/// <summary>
	/// Gets or sets the selected account, or one of the selected accounts.
	/// </summary>
	public AccountViewModel? SelectedAccount
	{
		get => this.selectedAccount;
		set => this.SetProperty(ref this.selectedAccount, value);
	}

	/// <summary>
	/// Gets or sets a collection of selected accounts.
	/// </summary>
	/// <remarks>
	/// This is optional. When set, the <see cref="DeleteCommand"/> will use this collection as the set of accounts to delete.
	/// When not set, the <see cref="SelectedAccount"/> will be used by the <see cref="DeleteCommand"/>.
	/// </remarks>
	public IList? SelectedAccounts
	{
		get => this.selectedAccounts;
		set => this.SetProperty(ref this.selectedAccounts, value);
	}

	internal AccountViewModel? AddingAccount { get; set; }

	/// <summary>
	/// Creates a new <see cref="Account"/> and <see cref="BankingAccountViewModel"/>.
	/// The <see cref="BankingAccountViewModel"/> is added to the view model collection,
	/// but the <see cref="Account"/> will only be added to the database when a property on it has changed.
	/// </summary>
	/// <param name="name">The name for the new account.</param>
	/// <returns>The new <see cref="BankingAccountViewModel"/>.</returns>
	public BankingAccountViewModel NewBankingAccount(string name = "") => (BankingAccountViewModel)this.NewAccount(Account.AccountType.Banking, name);

	/// <summary>
	/// Creates a new <see cref="Account"/> and <see cref="InvestingAccountViewModel"/>.
	/// The <see cref="InvestingAccountViewModel"/> is added to the view model collection,
	/// but the <see cref="Account"/> will only be added to the database when a property on it has changed.
	/// </summary>
	/// <param name="name">The name for the new account.</param>
	/// <returns>The new <see cref="InvestingAccountViewModel"/>.</returns>
	public InvestingAccountViewModel NewInvestingAccount(string name = "") => (InvestingAccountViewModel)this.NewAccount(Account.AccountType.Investing, name);

	/// <summary>
	/// Creates a new <see cref="Account"/> and <see cref="AccountViewModel"/>.
	/// The <see cref="AccountViewModel"/> is added to the view model collection,
	/// but the <see cref="Account"/> will only be added to the database when a property on it has changed.
	/// </summary>
	/// <param name="type">The type of account to be created.</param>
	/// <param name="name">The name for the new account.</param>
	/// <returns>The new <see cref="AccountViewModel"/>.</returns>
	public AccountViewModel NewAccount(Account.AccountType type, string name = "")
	{
		this.AddingNewAccount?.Invoke(this, EventArgs.Empty);
		if (this.AddingAccount is object)
		{
			Verify.Operation(this.AddingAccount.Type == type, "An account of another type is already being added.");
			this.SelectedAccount = this.AddingAccount;
			return this.AddingAccount;
		}

		AccountViewModel newAccountViewModel = AccountViewModel.Create(
			new Account { Type = type, CurrencyAssetId = this.documentViewModel.DefaultCurrency?.Id },
			this.documentViewModel);
		newAccountViewModel.PropertyChanged += this.AccountViewModel_PropertyChanged;

		this.accounts.Add(newAccountViewModel);
		this.SelectedAccount = newAccountViewModel;
		if (string.IsNullOrEmpty(name))
		{
			this.AddingAccount = newAccountViewModel;
			newAccountViewModel.NotifyWhenValid(s =>
			{
				if (this.AddingAccount == s)
				{
					this.documentViewModel.AddTransactionTarget(s);
					if (!s.IsClosed)
					{
						this.documentViewModel.BankingPanel.Add(s);
					}

					this.AddingAccount = null;
				}
			});
		}
		else
		{
			newAccountViewModel.Name = name;
			this.documentViewModel.AddTransactionTarget(newAccountViewModel);
			this.documentViewModel.BankingPanel.Add(newAccountViewModel);
		}

		return newAccountViewModel;
	}

	public void DeleteAccount(AccountViewModel accountViewModel)
	{
		this.accounts.Remove(accountViewModel);
		this.documentViewModel.BankingPanel.Remove(accountViewModel);
		this.documentViewModel.RemoveTransactionTarget(accountViewModel);

		using IDisposable? transaction = this.documentViewModel.MoneyFile.UndoableTransaction($"Deleted account \"{accountViewModel.Name}\"", accountViewModel.Model);
		this.documentViewModel.MoneyFile.Delete(accountViewModel.Model);

		if (accountViewModel.CurrencyAsset is object)
		{
			accountViewModel.CurrencyAsset.NotifyUseChange();
		}

		if (this.SelectedAccount == accountViewModel)
		{
			this.SelectedAccount = null;
		}

		if (this.AddingAccount == accountViewModel)
		{
			this.AddingAccount = null;
		}
	}

	public AccountViewModel? FindAccount(int id) => this.Accounts.FirstOrDefault(acct => acct.Id == id);

	internal void Add(AccountViewModel accountViewModel)
	{
		accountViewModel.PropertyChanged += this.AccountViewModel_PropertyChanged;
		this.accounts.Add(accountViewModel);
		this.documentViewModel.AddTransactionTarget(accountViewModel);
	}

	/// <summary>
	/// Clears the view model without deleting anything from the database.
	/// </summary>
	internal void ClearViewModel()
	{
		this.accounts.Clear();
		this.selectedAccount = null;
		this.selectedAccounts?.Clear();
	}

	private void AccountViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(AccountViewModel.Type))
		{
			AccountViewModel before = (AccountViewModel)sender!;
			AccountViewModel after = before.Recreate();
			AccountViewModel? selectedAccount = this.SelectedAccount;
			this.accounts.Remove(before);
			before.PropertyChanged -= this.AccountViewModel_PropertyChanged;
			this.accounts.Add(after);
			after.PropertyChanged += this.AccountViewModel_PropertyChanged;
			if (selectedAccount == before)
			{
				this.SelectedAccount = after;
			}

			this.documentViewModel.BankingPanel.Replace(before, after);

			this.documentViewModel.RemoveTransactionTarget(before);
			this.documentViewModel.AddTransactionTarget(after);
		}
	}

	private class AddAccountCommand : CommandBase
	{
		private readonly AccountsPanelViewModel viewModel;

		public AddAccountCommand(AccountsPanelViewModel viewModel)
		{
			this.viewModel = viewModel;
		}

		protected override Task ExecuteCoreAsync(object? parameter, CancellationToken cancellationToken)
		{
			this.viewModel.NewAccount(this.viewModel.AddingAccount?.Type ?? Account.AccountType.Banking);
			return Task.CompletedTask;
		}
	}

	private class DeleteAccountCommand : CommandBase
	{
		private readonly AccountsPanelViewModel viewModel;
		private INotifyCollectionChanged? subscribedSelectedAccounts;

		public DeleteAccountCommand(AccountsPanelViewModel viewModel)
		{
			this.viewModel = viewModel;
			this.viewModel.PropertyChanged += this.ViewModel_PropertyChanged;
			this.SubscribeToSelectionChanged();
		}

		public string Caption => "_Delete";

		public override bool CanExecute(object? parameter) => base.CanExecute(parameter) && (this.viewModel.SelectedAccounts?.Count > 0 || this.viewModel.SelectedAccount is object);

		protected override Task ExecuteCoreAsync(object? parameter, CancellationToken cancellationToken)
		{
			if (this.viewModel.SelectedAccounts is object)
			{
				foreach (AccountViewModel account in this.viewModel.SelectedAccounts.OfType<AccountViewModel>().ToList())
				{
					this.viewModel.DeleteAccount(account);
				}
			}
			else if (this.viewModel.SelectedAccount is object)
			{
				this.viewModel.DeleteAccount(this.viewModel.SelectedAccount);
			}

			return Task.CompletedTask;
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(this.viewModel.SelectedAccounts))
			{
				this.SubscribeToSelectionChanged();
			}
			else if (e.PropertyName is nameof(this.viewModel.SelectedAccount))
			{
				this.OnCanExecuteChanged();
			}
		}

		private void SelectedAccounts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => this.OnCanExecuteChanged();

		private void SubscribeToSelectionChanged()
		{
			if (this.subscribedSelectedAccounts is object)
			{
				this.subscribedSelectedAccounts.CollectionChanged -= this.SelectedAccounts_CollectionChanged;
			}

			this.subscribedSelectedAccounts = this.viewModel.SelectedAccounts as INotifyCollectionChanged;

			if (this.subscribedSelectedAccounts is object)
			{
				this.subscribedSelectedAccounts.CollectionChanged += this.SelectedAccounts_CollectionChanged;
			}
		}
	}
}
