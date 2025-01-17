﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class CategoriesPanelViewModelTests : MoneyTestBase
{
	public CategoriesPanelViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	private CategoriesPanelViewModel ViewModel => this.DocumentViewModel.CategoriesPanel;

	[Fact]
	public void InitialState()
	{
		Assert.Empty(this.ViewModel.Categories);
		Assert.Null(this.ViewModel.SelectedCategory);
	}

	[Fact]
	public void NewCategory()
	{
		Assert.True(this.ViewModel.AddCommand.CanExecute(null));

		TestUtilities.AssertRaises(
			h => this.ViewModel.AddingNewCategory += h,
			h => this.ViewModel.AddingNewCategory -= h,
			() => this.ViewModel.NewCategory());
		CategoryViewModel newCategory = Assert.Single(this.ViewModel.Categories);
		Assert.Same(newCategory, this.ViewModel.SelectedCategory);
		Assert.Equal(string.Empty, newCategory.Name);

		newCategory.Name = "cat";
		Assert.Equal("cat", Assert.Single(this.Money.Categories).Name);
	}

	[Fact]
	public async Task AddCommand()
	{
		Assert.True(this.ViewModel.AddCommand.CanExecute(null));

		await TestUtilities.AssertRaisesAsync(
			h => this.ViewModel.AddingNewCategory += h,
			h => this.ViewModel.AddingNewCategory -= h,
			() => this.ViewModel.AddCommand.ExecuteAsync());
		CategoryViewModel newCategory = Assert.Single(this.ViewModel.Categories);
		Assert.Same(newCategory, this.ViewModel.SelectedCategory);
		Assert.Equal(string.Empty, newCategory.Name);

		newCategory.Name = "cat";
		Assert.Equal("cat", Assert.Single(this.Money.Categories).Name);
	}

	[Fact]
	public async Task AddCommand_Twice()
	{
		await this.ViewModel.AddCommand.ExecuteAsync();
		CategoryViewModel? newCategory = this.ViewModel.SelectedCategory;
		Assert.NotNull(newCategory);
		newCategory!.Name = "cat";

		await this.ViewModel.AddCommand.ExecuteAsync();
		newCategory = this.ViewModel.SelectedCategory;
		Assert.NotNull(newCategory);
		newCategory!.Name = "dog";

		Assert.Equal(2, this.Money.Categories.Count());
	}

	[Theory, PairwiseData]
	public async Task DeleteCommand(bool saveFirst)
	{
		CategoryViewModel viewModel = this.DocumentViewModel.CategoriesPanel.NewCategory();
		if (saveFirst)
		{
			viewModel.Name = "cat";
		}

		Assert.True(this.ViewModel.DeleteCommand.CanExecute());
		await this.ViewModel.DeleteCommand.ExecuteAsync();
		Assert.Empty(this.ViewModel.Categories);
		Assert.Null(this.ViewModel.SelectedCategory);
		Assert.Empty(this.Money.Categories);
	}

	[Fact]
	public async Task Delete_MultipleWhenCategoriesAreNotInUse()
	{
		var cat1 = this.DocumentViewModel.CategoriesPanel.NewCategory("cat1");
		var cat2 = this.DocumentViewModel.CategoriesPanel.NewCategory("cat2");
		var cat3 = this.DocumentViewModel.CategoriesPanel.NewCategory("cat3");

		this.ViewModel.SelectedCategories = new[] { cat1, cat3 };
		Assert.True(this.ViewModel.DeleteCommand.CanExecute());
		await this.ViewModel.DeleteCommand.ExecuteAsync();

		Assert.Equal("cat2", Assert.Single(this.ViewModel.Categories).Name);
		Assert.Null(this.ViewModel.SelectedCategory);
		Assert.Equal("cat2", Assert.Single(this.Money.Categories).Name);
	}

	[Fact]
	public async Task Delete_WhenCategoryIsInUse()
	{
		var cat1 = this.ViewModel.NewCategory("cat1");
		var cat2 = this.ViewModel.NewCategory("cat2");

		BankingAccountViewModel checking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
		BankingTransactionViewModel transaction = checking.NewTransaction();
		transaction.CategoryOrTransfer = cat1;

		this.ViewModel.SelectedCategory = cat1;
		bool presented = false;
		this.UserNotification.Presentation += (s, e) =>
		{
			var pickerWindowViewModel = (PickerWindowViewModel)e;
			Assert.True(pickerWindowViewModel.Options.Contains(cat2));
			pickerWindowViewModel.SelectedOption = cat2;
			pickerWindowViewModel.ProceedCommand?.Execute(null);
			presented = true;
		};
		await this.ViewModel.DeleteCommand.ExecuteAsync();
		Assert.True(presented);

		Assert.Same(cat2, transaction.CategoryOrTransfer);
		Assert.DoesNotContain(cat1, this.ViewModel.Categories);

		this.ReloadViewModel();
		checking = (BankingAccountViewModel)this.DocumentViewModel.AccountsPanel.Accounts[0];
		transaction = checking.Transactions.Single(t => t.Id == transaction.Id);
		Assert.Equal(cat2.Name, transaction.CategoryOrTransfer?.Name);
		Assert.DoesNotContain(this.ViewModel.Categories, cat => cat.Name == cat1.Name);
	}

	[Fact]
	public async Task Delete_WhenCategoryIsInUseBySplit()
	{
		var cat1 = this.ViewModel.NewCategory("cat1");
		var cat2 = this.ViewModel.NewCategory("cat2");

		BankingAccountViewModel checking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
		BankingTransactionViewModel transaction = checking.NewTransaction();
		SplitTransactionViewModel split = transaction.NewSplit();
		split.CategoryOrTransfer = cat1;

		this.ViewModel.SelectedCategories = new[] { cat1 };
		bool presented = false;
		this.UserNotification.Presentation += (s, e) =>
		{
			var pickerWindowViewModel = (PickerWindowViewModel)e;
			Assert.True(pickerWindowViewModel.Options.Contains(cat2));
			Assert.False(pickerWindowViewModel.Options.Contains(cat1));
			pickerWindowViewModel.SelectedOption = cat2;
			pickerWindowViewModel.ProceedCommand?.Execute(null);
			presented = true;
		};
		await this.ViewModel.DeleteCommand.ExecuteAsync();
		Assert.True(presented);

		Assert.Same(cat2, split.CategoryOrTransfer);

		this.ReloadViewModel();
		checking = (BankingAccountViewModel)this.DocumentViewModel.AccountsPanel.Accounts[0];
		transaction = checking.Transactions.Single(t => t.Id == transaction.Id);
		split = transaction.Splits[0];
		Assert.Equal(cat2.Name, split.CategoryOrTransfer?.Name);
	}

	[Fact]
	public async Task Delete_WhenCategoryIsNotInUse()
	{
		var cat1 = this.ViewModel.NewCategory("cat1");
		var cat2 = this.ViewModel.NewCategory("cat2");

		BankingAccountViewModel checking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
		BankingTransactionViewModel transaction = checking.NewTransaction();
		transaction.CategoryOrTransfer = cat1;

		this.ViewModel.SelectedCategory = cat2;
		await this.ViewModel.DeleteCommand.ExecuteAsync();

		Assert.Same(cat1, transaction.CategoryOrTransfer);
		Assert.DoesNotContain(cat2, this.ViewModel.Categories);

		this.ReloadViewModel();
		checking = (BankingAccountViewModel)this.DocumentViewModel.AccountsPanel.Accounts[0];
		transaction = checking.Transactions.Single(t => t.Id == transaction.Id);
		Assert.Equal(cat1.Name, transaction.CategoryOrTransfer?.Name);
		Assert.DoesNotContain(this.ViewModel.Categories, cat => cat.Name == cat2.Name);
	}

	[Fact]
	public async Task Delete_MultipleWhenCategoriesAreInUse()
	{
		var cat1 = this.ViewModel.NewCategory("cat1");
		var cat2 = this.ViewModel.NewCategory("cat2");
		var cat3 = this.ViewModel.NewCategory("cat3");

		BankingAccountViewModel checking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
		BankingTransactionViewModel transaction = checking.NewTransaction();
		transaction.CategoryOrTransfer = cat1;

		this.ViewModel.SelectedCategories = new[] { cat1, cat2 };
		bool presented = false;
		this.UserNotification.Presentation += (s, e) =>
		{
			var pickerWindowViewModel = (PickerWindowViewModel)e;
			Assert.False(pickerWindowViewModel.Options.Contains(cat1));
			Assert.False(pickerWindowViewModel.Options.Contains(cat2));
			Assert.True(pickerWindowViewModel.Options.Contains(cat3));
			pickerWindowViewModel.SelectedOption = pickerWindowViewModel.Options[0]; // the "clear" option
			pickerWindowViewModel.ProceedCommand?.Execute(null);
			presented = true;
		};
		await this.ViewModel.DeleteCommand.ExecuteAsync();
		Assert.True(presented);

		Assert.Null(transaction.CategoryOrTransfer);

		this.ReloadViewModel();
		checking = (BankingAccountViewModel)this.DocumentViewModel.AccountsPanel.Accounts[0];
		transaction = checking.Transactions.Single(t => t.Id == transaction.Id);
		Assert.Null(transaction.CategoryOrTransfer);
	}

	[Fact]
	public async Task Delete_AllWhenCategoriesAreInUse()
	{
		var cat1 = this.ViewModel.NewCategory("cat1");
		var cat2 = this.ViewModel.NewCategory("cat2");
		var cat3 = this.ViewModel.NewCategory("cat3");

		BankingAccountViewModel checking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
		BankingTransactionViewModel transaction = checking.NewTransaction();
		transaction.CategoryOrTransfer = cat1;

		this.ViewModel.SelectedCategories = new[] { cat1, cat2, cat3 };
		await this.ViewModel.DeleteCommand.ExecuteAsync();

		Assert.Null(transaction.CategoryOrTransfer);

		this.ReloadViewModel();
		checking = (BankingAccountViewModel)this.DocumentViewModel.AccountsPanel.Accounts[0];
		transaction = checking.Transactions.Single(t => t.Id == transaction.Id);
		Assert.Null(transaction.CategoryOrTransfer);
	}

	[Fact]
	public async Task AddTwiceRedirectsToFirstIfNotCommitted()
	{
		Assert.True(this.ViewModel.AddCommand.CanExecute());
		await this.ViewModel.AddCommand.ExecuteAsync();
		CategoryViewModel? first = this.ViewModel.SelectedCategory;
		Assert.NotNull(first);

		Assert.True(this.ViewModel.AddCommand.CanExecute());
		await this.ViewModel.AddCommand.ExecuteAsync();
		CategoryViewModel? second = this.ViewModel.SelectedCategory;
		Assert.Same(first, second);

		first!.Name = "Some category";
		Assert.True(this.ViewModel.AddCommand.CanExecute());
		await this.ViewModel.AddCommand.ExecuteAsync();
		CategoryViewModel? third = this.ViewModel.SelectedCategory;
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
		Assert.Null(this.ViewModel.SelectedCategory);
		Assert.Empty(this.ViewModel.Categories);
	}

	[Fact]
	public async Task AddCommand_Undo()
	{
		await this.ViewModel.AddCommand.ExecuteAsync();
		const string name = "Some new category";
		this.ViewModel.SelectedCategory!.Name = name;
		this.DocumentViewModel.SelectedViewIndex = DocumentViewModel.SelectableViews.Banking;

		await this.DocumentViewModel.UndoCommand.ExecuteAsync();
		Assert.DoesNotContain(this.ViewModel.Categories, cat => cat.Name == name);

		Assert.Equal(DocumentViewModel.SelectableViews.Categories, this.DocumentViewModel.SelectedViewIndex);
	}

	[Theory, PairwiseData]
	public async Task DeleteCommand_Undo(bool useSelectedCollection)
	{
		const string name = "Some new category";
		CategoryViewModel category = this.ViewModel.NewCategory(name);

		BankingAccountViewModel checking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("checking");
		BankingTransactionViewModel transaction = checking.NewTransaction();
		transaction.Memo = "some memo";
		transaction.CategoryOrTransfer = category;

		if (useSelectedCollection)
		{
			this.ViewModel.SelectedCategories = new[] { category };
		}

		await this.ViewModel.DeleteCommand.ExecuteAsync();
		Assert.DoesNotContain(this.ViewModel.Categories, cat => cat.Name == name);
		this.DocumentViewModel.SelectedViewIndex = DocumentViewModel.SelectableViews.Banking;

		await this.DocumentViewModel.UndoCommand.ExecuteAsync();
		Assert.Contains(this.ViewModel.Categories, cat => cat.Name == name);

		Assert.Equal(DocumentViewModel.SelectableViews.Categories, this.DocumentViewModel.SelectedViewIndex);
		Assert.Equal(category.Id, this.DocumentViewModel.CategoriesPanel.SelectedCategory?.Id);

		// Verify that transaction assignments were also restored.
		BankingTransactionViewModel refreshedTransaction = ((BankingAccountViewModel)this.DocumentViewModel.AccountsPanel.FindAccount(checking.Id)!).Transactions[0];
		Assert.Equal(transaction.Memo, refreshedTransaction.Memo);
		Assert.Equal(category.Id, refreshedTransaction.CategoryOrTransfer?.Id);
	}
}
