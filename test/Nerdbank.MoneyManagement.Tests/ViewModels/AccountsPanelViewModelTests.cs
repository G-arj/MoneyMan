﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class AccountsPanelViewModelTests : MoneyTestBase
{
	public AccountsPanelViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	private AccountsPanelViewModel ViewModel => this.DocumentViewModel.AccountsPanel;

	[Fact]
	public void InitialState()
	{
		Assert.Empty(this.ViewModel.Accounts);
		Assert.Null(this.ViewModel.SelectedAccount);
	}

	[Fact]
	public void NewBankingAccount()
	{
		Assert.True(this.ViewModel.AddCommand.CanExecute(null));

		TestUtilities.AssertRaises(
			h => this.ViewModel.AddingNewAccount += h,
			h => this.ViewModel.AddingNewAccount -= h,
			() => this.ViewModel.NewBankingAccount());
		AccountViewModel newAccount = Assert.Single(this.ViewModel.Accounts);
		Assert.Same(newAccount, this.ViewModel.SelectedAccount);
		Assert.Equal(string.Empty, newAccount.Name);

		newAccount.Name = "cat";
		Assert.Equal("cat", Assert.Single(this.Money.Accounts).Name);

		Assert.Same(this.DocumentViewModel.DefaultCurrency, newAccount.CurrencyAsset);
	}

	[Fact]
	public void NewInvestingAccount()
	{
		Assert.True(this.ViewModel.AddCommand.CanExecute(null));

		TestUtilities.AssertRaises(
			h => this.ViewModel.AddingNewAccount += h,
			h => this.ViewModel.AddingNewAccount -= h,
			() => this.ViewModel.NewInvestingAccount());
		AccountViewModel newAccount = Assert.Single(this.ViewModel.Accounts);
		Assert.Same(newAccount, this.ViewModel.SelectedAccount);
		Assert.Equal(string.Empty, newAccount.Name);

		newAccount.Name = "cat";
		Assert.Equal("cat", Assert.Single(this.Money.Accounts).Name);

		Assert.Same(this.DocumentViewModel.DefaultCurrency, newAccount.CurrencyAsset);
	}

	[Fact]
	public async Task AddCommand()
	{
		Assert.True(this.ViewModel.AddCommand.CanExecute(null));

		await TestUtilities.AssertRaisesAsync(
			h => this.ViewModel.AddingNewAccount += h,
			h => this.ViewModel.AddingNewAccount -= h,
			() => this.ViewModel.AddCommand.ExecuteAsync());
		AccountViewModel newAccount = Assert.Single(this.ViewModel.Accounts);
		Assert.Same(newAccount, this.ViewModel.SelectedAccount);
		Assert.Equal(string.Empty, newAccount.Name);

		newAccount.Name = "cat";
		Assert.Equal("cat", Assert.Single(this.Money.Accounts).Name);
	}

	[Theory, PairwiseData]
	public async Task AddCommand_SwitchToInvestingAccount(bool setNameFirst)
	{
		await this.ViewModel.AddCommand.ExecuteAsync();
		AccountViewModel newAccount = this.ViewModel.SelectedAccount!;
		Assert.Equal(Account.AccountType.Banking, newAccount.Type);
		Assert.IsType<BankingAccountViewModel>(newAccount);
		if (setNameFirst)
		{
			newAccount.Name = "test";
		}

		// Changing the type must do something very peculiar.
		// The view model itself is replaced by another instance of the new type.
		newAccount.Type = Account.AccountType.Investing;
		Assert.NotSame(newAccount, this.ViewModel.SelectedAccount);
		newAccount = this.ViewModel.SelectedAccount!;
		Assert.Equal(Account.AccountType.Investing, newAccount.Type);
		Assert.IsType<InvestingAccountViewModel>(newAccount);
		if (setNameFirst)
		{
			Assert.Equal("test", newAccount.Name);
		}
	}

	[Fact]
	public async Task SwitchAccountType_ReflectedInBankingPanel()
	{
		await this.ViewModel.AddCommand.ExecuteAsync();
		AccountViewModel bankingAccount = this.ViewModel.SelectedAccount!;
		bankingAccount.Name = "test";
		Assert.Contains(bankingAccount, this.DocumentViewModel.BankingPanel.Accounts);
		this.DocumentViewModel.BankingPanel.SelectedAccount = bankingAccount;

		bankingAccount.Type = Account.AccountType.Investing;
		AccountViewModel investingAccount = this.ViewModel.SelectedAccount!;

		Assert.DoesNotContain(bankingAccount, this.DocumentViewModel.BankingPanel.Accounts);
		Assert.Contains(investingAccount, this.DocumentViewModel.BankingPanel.Accounts);
		Assert.Same(investingAccount, this.DocumentViewModel.BankingPanel.SelectedAccount);
	}

	[Fact]
	public async Task SwitchAccountType_ReflectedInTransferTargets()
	{
		await this.ViewModel.AddCommand.ExecuteAsync();
		AccountViewModel bankingAccount = this.ViewModel.SelectedAccount!;
		bankingAccount.Name = "test";
		Assert.Contains(bankingAccount, this.DocumentViewModel.BankingPanel.Accounts);

		bankingAccount.Type = Account.AccountType.Investing;
		AccountViewModel investingAccount = this.ViewModel.SelectedAccount!;

		Assert.DoesNotContain(bankingAccount, this.DocumentViewModel.TransactionTargets);
		Assert.Contains(investingAccount, this.DocumentViewModel.TransactionTargets);
	}

	[Fact]
	public async Task SwitchAccountType_AndBackAgain()
	{
		await this.ViewModel.AddCommand.ExecuteAsync();
		AccountViewModel? newAccount = this.ViewModel.SelectedAccount;
		Assert.NotNull(newAccount);
		newAccount!.Name = "test";

		newAccount!.Type = Account.AccountType.Investing;
		newAccount = this.ViewModel.SelectedAccount;
		Assert.IsType<InvestingAccountViewModel>(newAccount);

		newAccount!.Type = Account.AccountType.Banking;
		newAccount = this.ViewModel.SelectedAccount;
		Assert.IsType<BankingAccountViewModel>(newAccount);
	}

	[Fact]
	public async Task AddCommand_Twice()
	{
		await this.ViewModel.AddCommand.ExecuteAsync();
		AccountViewModel? newAccount = this.ViewModel.SelectedAccount;
		Assert.NotNull(newAccount);
		newAccount!.Name = "cat";

		await this.ViewModel.AddCommand.ExecuteAsync();
		newAccount = this.ViewModel.SelectedAccount;
		Assert.NotNull(newAccount);
		newAccount!.Name = "dog";

		Assert.Equal(2, this.Money.Accounts.Count());
	}

	[Fact]
	public async Task AddCommand_SwitchToInvesting_AddAgain()
	{
		int accountCount = this.ViewModel.Accounts.Count;
		await this.ViewModel.AddCommand.ExecuteAsync();
		AccountViewModel? newAccount = this.ViewModel.SelectedAccount;
		Assert.NotNull(newAccount);
		newAccount!.Type = Account.AccountType.Investing;
		Assert.Equal(accountCount + 1, this.ViewModel.Accounts.Count);

		await this.ViewModel.AddCommand.ExecuteAsync();
		newAccount = this.ViewModel.SelectedAccount;
		Assert.NotNull(newAccount);
		Assert.Equal(Account.AccountType.Investing, newAccount!.Type);
		Assert.Equal(accountCount + 1, this.ViewModel.Accounts.Count);
	}

	[Fact]
	public async Task AddCommand_Undo()
	{
		const string name = "name";
		await this.ViewModel.AddCommand.ExecuteAsync();
		this.ViewModel.SelectedAccount!.Name = name;
		this.DocumentViewModel.SelectedViewIndex = DocumentViewModel.SelectableViews.Banking;

		Assert.True(this.DocumentViewModel.UndoCommand.CanExecute());
		await this.DocumentViewModel.UndoCommand.ExecuteAsync();
		Assert.DoesNotContain(this.ViewModel.Accounts, acct => acct.Name == name);

		Assert.Equal(DocumentViewModel.SelectableViews.Accounts, this.DocumentViewModel.SelectedViewIndex);
		Assert.Null(this.DocumentViewModel.AccountsPanel.SelectedAccount);
	}

	[Fact]
	public async Task DeleteCommand_Undo()
	{
		const string name = "name";
		AccountViewModel account = this.ViewModel.NewBankingAccount(name);
		await this.ViewModel.DeleteCommand.ExecuteAsync();
		Assert.DoesNotContain(this.ViewModel.Accounts, acct => acct.Name == name);
		this.DocumentViewModel.SelectedViewIndex = DocumentViewModel.SelectableViews.Banking;

		await this.DocumentViewModel.UndoCommand.ExecuteAsync();
		Assert.Contains(this.ViewModel.Accounts, acct => acct.Name == name);

		Assert.Equal(DocumentViewModel.SelectableViews.Accounts, this.DocumentViewModel.SelectedViewIndex);
		Assert.Equal(account.Id, this.DocumentViewModel.AccountsPanel.SelectedAccount?.Id);
	}

	[Theory, PairwiseData]
	public async Task DeleteCommand(bool saveFirst)
	{
		AccountViewModel viewModel = this.DocumentViewModel.AccountsPanel.NewBankingAccount();
		if (saveFirst)
		{
			viewModel.Name = "cat";
		}

		Assert.True(this.ViewModel.DeleteCommand.CanExecute());
		await this.ViewModel.DeleteCommand.ExecuteAsync();
		Assert.Empty(this.ViewModel.Accounts);
		Assert.Null(this.ViewModel.SelectedAccount);
		Assert.Empty(this.Money.Accounts);
	}

	[Fact]
	public async Task DeleteCommand_Multiple()
	{
		var cat1 = this.DocumentViewModel.AccountsPanel.NewBankingAccount("cat1");
		var cat2 = this.DocumentViewModel.AccountsPanel.NewBankingAccount("cat2");
		var cat3 = this.DocumentViewModel.AccountsPanel.NewBankingAccount("cat3");

		this.ViewModel.SelectedAccounts = new[] { cat1, cat3 };
		Assert.True(this.ViewModel.DeleteCommand.CanExecute());
		await this.ViewModel.DeleteCommand.ExecuteAsync();

		Assert.Equal("cat2", Assert.Single(this.ViewModel.Accounts).Name);
		Assert.Null(this.ViewModel.SelectedAccount);
		Assert.Equal("cat2", Assert.Single(this.Money.Accounts).Name);
	}

	[Fact]
	public async Task AddTwiceRedirectsToFirstIfNotCommitted()
	{
		Assert.True(this.ViewModel.AddCommand.CanExecute());
		await this.ViewModel.AddCommand.ExecuteAsync();
		AccountViewModel? first = this.ViewModel.SelectedAccount;
		Assert.NotNull(first);

		Assert.True(this.ViewModel.AddCommand.CanExecute());
		await this.ViewModel.AddCommand.ExecuteAsync();
		AccountViewModel? second = this.ViewModel.SelectedAccount;
		Assert.Same(first, second);

		first!.Name = "Some account";
		Assert.True(this.ViewModel.AddCommand.CanExecute());
		await this.ViewModel.AddCommand.ExecuteAsync();
		AccountViewModel? third = this.ViewModel.SelectedAccount;
		Assert.NotNull(third);
		Assert.NotSame(first, third);
		Assert.Equal(string.Empty, third!.Name);
	}

	[Fact]
	public async Task AddThenDelete()
	{
		await this.ViewModel.AddCommand.ExecuteAsync();
		Assert.True(this.ViewModel.DeleteCommand.CanExecute());
		await this.ViewModel.DeleteCommand.ExecuteAsync();
		Assert.Null(this.ViewModel.SelectedAccount);
		Assert.Empty(this.ViewModel.Accounts);
	}

	[Fact]
	public void AccountListIsSorted()
	{
		AccountViewModel checking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
		AccountViewModel savings = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Savings");
		Assert.Same(checking, this.DocumentViewModel.AccountsPanel.Accounts[0]);
		Assert.Same(savings, this.DocumentViewModel.AccountsPanel.Accounts[1]);

		// Insert one new account that should sort to the top.
		AccountViewModel anotherChecking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Another checking");
		Assert.Same(anotherChecking, this.DocumentViewModel.AccountsPanel.Accounts[0]);
		Assert.Same(checking, this.DocumentViewModel.AccountsPanel.Accounts[1]);
		Assert.Same(savings, this.DocumentViewModel.AccountsPanel.Accounts[2]);

		// Rename an account and confirm it is re-sorted.
		checking.Name = "The last checking";
		Assert.Same(anotherChecking, this.DocumentViewModel.AccountsPanel.Accounts[0]);
		Assert.Same(savings, this.DocumentViewModel.AccountsPanel.Accounts[1]);
		Assert.Same(checking, this.DocumentViewModel.AccountsPanel.Accounts[2]);
	}

	[Fact]
	public async Task AccountTypeChangeDisallowedWhenNonEmpty()
	{
		await this.ViewModel.AddCommand.ExecuteAsync();
		BankingAccountViewModel newAccount = (BankingAccountViewModel)this.ViewModel.SelectedAccount!;
		newAccount.Name = "some account";
		BankingTransactionViewModel transaction = newAccount.NewTransaction();
		transaction.When = DateTime.Now;
		transaction.Payee = "test";

		Assert.Throws<InvalidOperationException>(() => newAccount.Type = Account.AccountType.Investing);
		Assert.Equal(Account.AccountType.Banking, newAccount.Type);
	}
}
