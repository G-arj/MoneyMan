﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;
using Validation;

namespace Nerdbank.MoneyManagement;

/// <summary>
/// Describes a bank account.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class Account : ModelBase
{
	/// <summary>
	/// Enumerates the kinds of accounts.
	/// </summary>
	public enum AccountType
	{
		/// <summary>
		/// The default account type, which deals with a single fiat currency with deposits, withdrawals and transfers.
		/// </summary>
		Banking = 0,

		/// <summary>
		/// An account that may hold any number of assets of various kinds.
		/// E.g. a brokerage, 401k or cryptocurrency wallet.
		/// </summary>
		Investing = 1,
	}

	/// <summary>
	/// Gets or sets the name of this account.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets a value indicating whether the account has been closed.
	/// </summary>
	/// <remarks>
	/// Closed accounts are excluded from most queries by default.
	/// </remarks>
	public bool IsClosed { get; set; }

	/// <summary>
	/// Gets or sets the type of this account.
	/// </summary>
	public AccountType Type { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="Asset"/> used as the currency for this account,
	/// if the account supports currencies.
	/// </summary>
	/// <remarks>
	/// When <see cref="Type"/> is <see cref="AccountType.Banking" /> this is expected to always have a value.
	/// For <see cref="AccountType.Investing"/> accounts this may be null if the account does not support a single currency.
	/// For example an account that represents many cryptocurrencies but no fiat currency may decide to leave this unset.
	/// </remarks>
	public int? CurrencyAssetId { get; set; }

	private string? DebuggerDisplay => this.Name;

	/// <summary>
	/// Creates a new <see cref="Transaction"/> that describes a withdrawal from this account.
	/// </summary>
	/// <param name="amount">The amount to withdraw.</param>
	/// <returns>The created transaction that has not yet been added to the database.</returns>
	public Transaction Withdraw(Amount amount)
	{
		Verify.Operation(this.Id != 0, "This account has not been saved yet.");
		return new Transaction
		{
			Action = TransactionAction.Withdraw,
			When = DateTime.Now,
			DebitAccountId = this.Id,
			DebitAmount = amount.Value,
			DebitAssetId = amount.AssetId,
		};
	}

	/// <summary>
	/// Creates a new <see cref="Transaction"/> that describes a deposit to this account.
	/// </summary>
	/// <param name="amount">The amount to deposit.</param>
	/// <returns>The created transaction that has not yet been added to the database.</returns>
	public Transaction Deposit(Amount amount)
	{
		Verify.Operation(this.Id != 0, "This account has not been saved yet.");
		return new Transaction
		{
			Action = TransactionAction.Deposit,
			When = DateTime.Now,
			CreditAccountId = this.Id,
			CreditAmount = amount.Value,
			CreditAssetId = amount.AssetId,
		};
	}

	/// <summary>
	/// Creates a new <see cref="Transaction"/> that describes a transfer from this account to another one.
	/// </summary>
	/// <param name="receivingAccount">The account to receive funds from this account.</param>
	/// <param name="amount">The amount to transfer.</param>
	/// <returns>The created transaction that has not yet been added to the database.</returns>
	public Transaction Transfer(Account receivingAccount, Amount amount)
	{
		Requires.NotNull(receivingAccount, nameof(receivingAccount));

		return new Transaction
		{
			Action = TransactionAction.Transfer,
			When = DateTime.Now,
			DebitAccountId = this.Id,
			DebitAmount = amount.Value,
			DebitAssetId = amount.AssetId,
			CreditAccountId = receivingAccount.Id,
			CreditAmount = amount.Value,
			CreditAssetId = amount.AssetId,
		};
	}
}
