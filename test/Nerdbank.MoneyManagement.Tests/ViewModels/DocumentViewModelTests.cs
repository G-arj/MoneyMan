﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class DocumentViewModelTests : MoneyTestBase
{
	public DocumentViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void LoadFromFile()
	{
		this.Money.InsertAll(new ModelBase[]
		{
			new Account { Name = "Checking" },
			new Category { Name = "Cat1" },
		});
		DocumentViewModel documentViewModel = new(this.Money);
		Assert.Contains(documentViewModel.BankingPanel?.Accounts, acct => acct.Name == "Checking");
		Assert.Contains(documentViewModel.CategoriesPanel?.Categories, cat => cat.Name == "Cat1");
	}

	[Fact]
	public void Undo_NewFile()
	{
		Assert.False(this.DocumentViewModel.UndoCommand.CanExecute());
	}

	[Fact]
	public async Task UndoCommandCaption_PropertyChangeEvent()
	{
		await TestUtilities.AssertPropertyChangedEventAsync(
			this.DocumentViewModel,
			async delegate
			{
				await this.DocumentViewModel.AccountsPanel.AddCommand.ExecuteAsync();
				this.DocumentViewModel.AccountsPanel.SelectedAccount!.Name = "some new account";
			},
			nameof(this.DocumentViewModel.UndoCommandCaption));
	}

	[Fact]
	public void NewFileGetsDefaultCategories()
	{
		DocumentViewModel documentViewModel = DocumentViewModel.CreateNew(MoneyFile.Load(":memory:"));
		Assert.Contains(documentViewModel.CategoriesPanel!.Categories, cat => cat.Name == "Groceries");
	}

	[Fact]
	public void NetWorth()
	{
		Account account = new() { Name = "Checking", CurrencyAssetId = this.Money.PreferredAssetId };
		this.Money.Insert(account);
		Transaction tx1 = new() { When = DateTime.Now, CreditAccountId = account.Id, CreditAmount = 10, CreditAssetId = account.CurrencyAssetId };
		this.Money.Insert(tx1);
		Assert.Equal(10, this.DocumentViewModel.NetWorth);

		Transaction tx2 = new() { When = DateTime.Now, DebitAccountId = account.Id, DebitAmount = 3, DebitAssetId = account.CurrencyAssetId };
		TestUtilities.AssertPropertyChangedEvent(this.DocumentViewModel, () => this.Money.Insert(tx2), nameof(this.DocumentViewModel.NetWorth));
		Assert.Equal(7, this.DocumentViewModel.NetWorth);

		this.DocumentViewModel.AccountsPanel.Accounts.Single().IsClosed = true;
		Assert.Equal(0, this.DocumentViewModel.NetWorth);
	}

	[Fact]
	public void NewAccount()
	{
		AccountViewModel accountViewModel = this.DocumentViewModel.AccountsPanel.NewBankingAccount();
		accountViewModel.Name = "some new account";
		Account account = Assert.Single(this.Money.Accounts);
		Assert.Equal(accountViewModel.Name, account.Name);
	}

	[Theory, PairwiseData]
	public void AddedAccountAddsToTransactionTargets(Account.AccountType type)
	{
		AccountViewModel accountViewModel = this.DocumentViewModel.AccountsPanel.NewAccount(type);
		accountViewModel.Name = "some new account";
		Account account = Assert.Single(this.Money.Accounts);
		Assert.Equal(accountViewModel.Name, account.Name);
		Assert.Contains(accountViewModel, this.DocumentViewModel.TransactionTargets);
	}

	[Theory, PairwiseData]
	public void DeletedAccountRemovesFromTransactionTargets(Account.AccountType type)
	{
		AccountViewModel accountViewModel = this.DocumentViewModel.AccountsPanel.NewAccount(type);
		accountViewModel.Name = "some new account";
		Assert.Contains(accountViewModel, this.DocumentViewModel.TransactionTargets);

		this.DocumentViewModel.AccountsPanel.DeleteAccount(accountViewModel);
		Assert.DoesNotContain(accountViewModel, this.DocumentViewModel.TransactionTargets);
	}

	[Fact]
	public void AddedCategoryAddsToTransactionTargets()
	{
		CategoryViewModel categoryViewModel = this.DocumentViewModel.CategoriesPanel.NewCategory("some new category");
		Category category = Assert.Single(this.Money.Categories);
		Assert.Equal(categoryViewModel.Name, category.Name);
		Assert.Contains(categoryViewModel, this.DocumentViewModel.TransactionTargets);
	}

	[Fact]
	public void DeletedCategoryRemovesFromTransactionTargets()
	{
		CategoryViewModel categoryViewModel = this.DocumentViewModel.CategoriesPanel.NewCategory("some new category");
		Assert.Contains(categoryViewModel, this.DocumentViewModel.TransactionTargets);

		this.DocumentViewModel.CategoriesPanel.DeleteCategory(categoryViewModel);
		Assert.DoesNotContain(categoryViewModel, this.DocumentViewModel.TransactionTargets);
	}

	[Fact]
	public void TransactionTargets_DoesNotIncludeVolatileAccounts()
	{
		AccountViewModel accountViewModel = this.DocumentViewModel.AccountsPanel.NewBankingAccount();
		Assert.DoesNotContain(accountViewModel, this.DocumentViewModel.TransactionTargets);
		accountViewModel.Name = "Checking";
		Assert.Contains(accountViewModel, this.DocumentViewModel.TransactionTargets);
	}

	/// <summary>
	/// Verifies that transaction targets includes closed accounts.
	/// This is important because editing old transactions must be able to show that it possibly transferred to/from an account that is now closed.
	/// </summary>
	[Fact]
	public void TransactionTargetsIncludesClosedAccounts()
	{
		AccountViewModel closed = this.DocumentViewModel.AccountsPanel.NewBankingAccount("ToBeClosed");
		Assert.Contains(closed, this.DocumentViewModel.TransactionTargets);
		closed.IsClosed = true;
		Assert.Contains(closed, this.DocumentViewModel.TransactionTargets);
		this.ReloadViewModel();
		Assert.Contains(this.DocumentViewModel.TransactionTargets, tt => tt.Name == closed.Name);
	}

	[Fact]
	public void TransactionTargets_IncludesSplitSingleton()
	{
		Assert.Contains(SplitCategoryPlaceholder.Singleton, this.DocumentViewModel.TransactionTargets);
	}

	[Fact]
	public void TransactionTargets_IsSorted()
	{
		AccountViewModel accountG = this.DocumentViewModel.AccountsPanel.NewBankingAccount("g");
		AccountViewModel accountA = this.DocumentViewModel.AccountsPanel.NewBankingAccount("a");
		CategoryViewModel categoryA = this.DocumentViewModel.CategoriesPanel.NewCategory("a");
		CategoryViewModel categoryG = this.DocumentViewModel.CategoriesPanel.NewCategory("g");
		Assert.Equal<ITransactionTarget>(
			new ITransactionTarget[] { categoryA, categoryG, SplitCategoryPlaceholder.Singleton, accountA, accountG },
			this.DocumentViewModel.TransactionTargets);
	}

	[Fact]
	public void Reset()
	{
		AccountViewModel account = this.DocumentViewModel.AccountsPanel.NewBankingAccount("checking");
		this.DocumentViewModel.Reset();
		Assert.Equal(2, this.DocumentViewModel.TransactionTargets.Count);
		Assert.Contains(this.DocumentViewModel.TransactionTargets, tt => tt.Name == account.Name);
		Assert.Contains(this.DocumentViewModel.TransactionTargets, tt => tt.Name == SplitCategoryPlaceholder.Singleton.Name);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			this.DocumentViewModel.Dispose();
		}

		base.Dispose(disposing);
	}
}
