﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nerdbank.MoneyManagement.Tests;
using Nerdbank.MoneyManagement.ViewModels;
using Xunit;
using Xunit.Abstractions;

public class DocumentViewModelTests : TestBase
{
	private DocumentViewModel viewModel = new DocumentViewModel(null);

	public DocumentViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void InitialState()
	{
		Assert.False(this.viewModel.IsFileOpen);
		Assert.Null(this.viewModel.AccountsPanel);
		Assert.Null(this.viewModel.CategoriesPanel);
	}

	[Fact]
	public void NewFileGetsDefaultCategories()
	{
		this.viewModel = DocumentViewModel.CreateNew(this.GenerateTemporaryFileName());
		Assert.Contains(this.viewModel.CategoriesPanel!.Categories, cat => cat.Name == "Groceries");
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			this.viewModel.Dispose();
		}

		base.Dispose(disposing);
	}
}
