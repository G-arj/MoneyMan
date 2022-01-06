﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels;

public class CategoryAccountViewModel : AccountViewModel
{
	public CategoryAccountViewModel(Account? model, DocumentViewModel documentViewModel)
		: base(model, documentViewModel)
	{
		ThrowOnUnexpectedAccountType(nameof(model), Account.AccountType.Category, this.Model.Type);
		this.Type = Account.AccountType.Category;
		this.CopyFrom(this.Model);
	}

	public override string? TransferTargetName => this.Name;

	protected override bool IsEmpty => throw new NotImplementedException();

	protected override bool IsPopulated => throw new NotImplementedException();

	public override void DeleteTransaction(TransactionViewModel transaction)
	{
		throw new NotImplementedException();
	}

	public override TransactionViewModel? FindTransaction(int? id)
	{
		throw new NotImplementedException();
	}

	internal override void NotifyAccountDeleted(ICollection<int> accountIds)
	{
		throw new NotImplementedException();
	}

	internal override void NotifyTransactionChanged(IReadOnlyList<TransactionAndEntry> transaction)
	{
		throw new NotImplementedException();
	}

	protected override void RemoveTransactionFromViewModel(TransactionViewModel transaction)
	{
		throw new NotImplementedException();
	}
}