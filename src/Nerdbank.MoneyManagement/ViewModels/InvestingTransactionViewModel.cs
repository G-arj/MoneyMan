﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft;

namespace Nerdbank.MoneyManagement.ViewModels;

public class InvestingTransactionViewModel : TransactionViewModel
{
	private TransactionAction? action;
	private AccountViewModel? depositAccount;
	private AccountViewModel? withdrawAccount;
	private AssetViewModel? deposit;
	private AssetViewModel? withdrawAsset;
	private AssetViewModel? relatedAsset;
	private decimal? depositAmount;
	private decimal? withdrawAmount;

	public InvestingTransactionViewModel(InvestingAccountViewModel thisAccount, IReadOnlyList<TransactionAndEntry> models)
		: base(thisAccount, models)
	{
		this.RegisterDependentProperty(nameof(this.DepositAmount), nameof(this.SimpleAmount));
		this.RegisterDependentProperty(nameof(this.WithdrawAmount), nameof(this.SimpleAmount));
		this.RegisterDependentProperty(nameof(this.DepositAsset), nameof(this.SimpleAsset));
		this.RegisterDependentProperty(nameof(this.WithdrawAsset), nameof(this.SimpleAsset));
		this.RegisterDependentProperty(nameof(this.DepositAmount), nameof(this.SimpleCurrencyImpact));
		this.RegisterDependentProperty(nameof(this.WithdrawAmount), nameof(this.SimpleCurrencyImpact));
		this.RegisterDependentProperty(nameof(this.DepositAsset), nameof(this.SimpleCurrencyImpact));
		this.RegisterDependentProperty(nameof(this.WithdrawAsset), nameof(this.SimpleCurrencyImpact));
		this.RegisterDependentProperty(nameof(this.DepositAmount), nameof(this.SimplePrice));
		this.RegisterDependentProperty(nameof(this.WithdrawAmount), nameof(this.SimplePrice));
		this.RegisterDependentProperty(nameof(this.Action), nameof(this.Assets));
		this.RegisterDependentProperty(nameof(this.SimpleAccount), nameof(this.Assets));
		this.RegisterDependentProperty(nameof(this.Action), nameof(this.Accounts));
		this.RegisterDependentProperty(nameof(this.Action), nameof(this.IsSimplePriceApplicable));
		this.RegisterDependentProperty(nameof(this.Action), nameof(this.IsSimpleAssetApplicable));
		this.RegisterDependentProperty(nameof(this.Action), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.DepositAmount), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.DepositAsset), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.DepositAccount), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.WithdrawAmount), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.WithdrawAsset), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.WithdrawAccount), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.RelatedAsset), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.DepositAmount), nameof(this.DepositAmountFormatted));
		this.RegisterDependentProperty(nameof(this.DepositAsset), nameof(this.DepositAmountFormatted));
		this.RegisterDependentProperty(nameof(this.WithdrawAmount), nameof(this.WithdrawAmountFormatted));
		this.RegisterDependentProperty(nameof(this.WithdrawAsset), nameof(this.WithdrawAmountFormatted));
		this.RegisterDependentProperty(nameof(this.SimpleCurrencyImpact), nameof(this.SimpleCurrencyImpactFormatted));

		this.CopyFrom(models);
	}

	/// <inheritdoc cref="Transaction.Action"/>
	[Required, NonZero]
	public TransactionAction? Action
	{
		get => this.action;
		set
		{
			if (this.action != value)
			{
				this.SetProperty(ref this.action, value);
				if (value.HasValue)
				{
					switch (value.Value)
					{
						case TransactionAction.Buy:
							this.DepositAccount ??= this.ThisAccount;
							this.WithdrawAccount ??= this.ThisAccount;
							this.WithdrawAsset ??= this.ThisAccount.CurrencyAsset;
							break;
						case TransactionAction.Sell:
							this.DepositAccount ??= this.ThisAccount;
							this.WithdrawAccount ??= this.ThisAccount;
							this.DepositAsset ??= this.ThisAccount.CurrencyAsset;
							break;
						case TransactionAction.Exchange:
							this.DepositAccount ??= this.ThisAccount;
							this.WithdrawAccount ??= this.ThisAccount;
							break;
						case TransactionAction.Remove:
							this.WithdrawAccount = this.ThisAccount;
							this.DepositAccount = null;
							this.DepositAmount = null;
							this.DepositAsset = null;
							break;
						case TransactionAction.Withdraw:
							this.WithdrawAccount = this.ThisAccount;
							this.DepositAccount = null;
							this.DepositAmount = null;
							this.DepositAsset = null;
							this.WithdrawAsset = this.ThisAccount.CurrencyAsset;
							break;
						case TransactionAction.Interest:
						case TransactionAction.Deposit:
							this.DepositAccount = this.ThisAccount;
							this.DepositAsset = this.ThisAccount.CurrencyAsset;
							this.WithdrawAccount = null;
							this.WithdrawAmount = null;
							this.WithdrawAsset = null;
							break;
						case TransactionAction.Add:
							this.DepositAccount = this.ThisAccount;
							this.WithdrawAccount = null;
							this.WithdrawAmount = null;
							this.WithdrawAsset = null;
							break;
						case TransactionAction.Dividend:
							this.DepositAccount = this.ThisAccount;
							this.DepositAsset = this.ThisAccount.CurrencyAsset;
							this.WithdrawAccount = null;
							this.WithdrawAmount = null;
							this.WithdrawAsset = null;
							break;
					}
				}
			}
		}
	}

	/// <summary>
	/// Gets a collection of <see cref="TransactionAction"/> that may be set to <see cref="Action"/>,
	/// with human-readable captions.
	/// </summary>
	public ReadOnlyCollection<EnumValueViewModel<TransactionAction>> Actions { get; } = new ReadOnlyCollection<EnumValueViewModel<TransactionAction>>(new EnumValueViewModel<TransactionAction>[]
	{
		new(TransactionAction.Buy, nameof(TransactionAction.Buy)),
		new(TransactionAction.Sell, nameof(TransactionAction.Sell)),
		new(TransactionAction.Exchange, nameof(TransactionAction.Exchange)),
		new(TransactionAction.Transfer, nameof(TransactionAction.Transfer)),
		new(TransactionAction.Dividend, nameof(TransactionAction.Dividend)),
		new(TransactionAction.Interest, nameof(TransactionAction.Interest)),
		new(TransactionAction.Add, nameof(TransactionAction.Add)),
		new(TransactionAction.Remove, nameof(TransactionAction.Remove)),
		new(TransactionAction.Deposit, nameof(TransactionAction.Deposit)),
		new(TransactionAction.Withdraw, nameof(TransactionAction.Withdraw)),
	});

	/// <inheritdoc cref="TransactionViewModel.ThisAccount"/>
	public new InvestingAccountViewModel ThisAccount => (InvestingAccountViewModel)base.ThisAccount;

	public AssetViewModel? DepositAsset
	{
		get => this.deposit;
		set => this.SetProperty(ref this.deposit, value);
	}

	[Range(0, int.MaxValue, ErrorMessage = "Must not be a negative number.")]
	public decimal? DepositAmount
	{
		get => this.depositAmount;
		set => this.SetProperty(ref this.depositAmount, value);
	}

	public AccountViewModel? DepositAccount
	{
		get => this.depositAccount;
		set => this.SetProperty(ref this.depositAccount, value);
	}

	public AssetViewModel? WithdrawAsset
	{
		get => this.withdrawAsset;
		set => this.SetProperty(ref this.withdrawAsset, value);
	}

	[Range(0, int.MaxValue, ErrorMessage = "Must not be a negative number.")]
	public decimal? WithdrawAmount
	{
		get => this.withdrawAmount;
		set => this.SetProperty(ref this.withdrawAmount, value);
	}

	public AccountViewModel? WithdrawAccount
	{
		get => this.withdrawAccount;
		set => this.SetProperty(ref this.withdrawAccount, value);
	}

	/// <inheritdoc cref="Transaction.RelatedAssetId"/>
	public AssetViewModel? RelatedAsset
	{
		get => this.relatedAsset;
		set => this.SetProperty(ref this.relatedAsset, value);
	}

	[NonNegativeTransactionAmount]
	public decimal? SimpleAmount
	{
		get
		{
			if (this.IsDepositOperation)
			{
				return this.DepositAmount;
			}
			else if (this.IsWithdrawOperation)
			{
				return this.WithdrawAmount;
			}
			else if (this.Action == TransactionAction.Transfer)
			{
				return this.DepositAccount == this.ThisAccount ? this.DepositAmount : -this.WithdrawAmount;
			}
			else
			{
				return null;
			}
		}

		set
		{
			if (this.IsDepositOperation)
			{
				this.DepositAmount = value;
			}
			else if (this.IsWithdrawOperation)
			{
				this.WithdrawAmount = value;
			}
			else if (this.Action == TransactionAction.Transfer)
			{
				this.WithdrawAmount = this.DepositAmount = value.HasValue ? Math.Abs(value.Value) : null;
				if (value > 0)
				{
					AccountViewModel? other = this.DepositAccount;
					this.DepositAccount = this.ThisAccount;
					if (other != this.ThisAccount && other is not null)
					{
						this.WithdrawAccount = other;
					}
				}
				else
				{
					AccountViewModel? other = this.WithdrawAccount;
					this.WithdrawAccount = this.ThisAccount;
					if (other != this.ThisAccount && other is not null)
					{
						this.DepositAccount = other;
					}
				}
			}
			else
			{
				ThrowNotSimpleAction();
			}

			this.OnPropertyChanged();
		}
	}

	public AssetViewModel? SimpleAsset
	{
		get =>
			this.Action == TransactionAction.Dividend ? this.RelatedAsset :
			this.IsDepositOperation ? this.DepositAsset :
			this.IsWithdrawOperation ? this.WithdrawAsset :
			this.Action == TransactionAction.Transfer && this.DepositAsset == this.WithdrawAsset ? this.DepositAsset :
			null;
		set
		{
			if (this.Action == TransactionAction.Dividend)
			{
				this.RelatedAsset = value;
			}
			else if (this.Action is TransactionAction.Buy or TransactionAction.Add)
			{
				this.DepositAsset = value;
			}
			else if (this.Action is TransactionAction.Sell or TransactionAction.Remove)
			{
				this.WithdrawAsset = value;
			}
			else if (this.Action == TransactionAction.Transfer)
			{
				this.WithdrawAsset = this.DepositAsset = value;
			}
			else
			{
				ThrowNotSimpleAction();
			}

			this.OnPropertyChanged();
		}
	}

	public bool IsSimpleAssetApplicable => this.Action is TransactionAction.Dividend or TransactionAction.Buy or TransactionAction.Sell or TransactionAction.Add or TransactionAction.Remove;

	[Range(0, int.MaxValue, ErrorMessage = "Must not be a negative number.")]
	public decimal? SimplePrice
	{
		get
		{
			if (this.Action is TransactionAction.Buy)
			{
				return this.DepositAmount != 0 && this.WithdrawAmount != 0 ? this.WithdrawAmount / this.DepositAmount : null;
			}
			else if (this.Action is TransactionAction.Sell)
			{
				return this.DepositAmount != 0 && this.WithdrawAmount != 0 ? this.DepositAmount / this.WithdrawAmount : null;
			}
			else
			{
				return null;
			}
		}

		set
		{
			if (this.Action is TransactionAction.Buy)
			{
				this.WithdrawAmount = value * this.DepositAmount;
			}
			else if (this.Action is TransactionAction.Sell)
			{
				this.DepositAmount = value * this.WithdrawAmount;
			}
			else
			{
				throw ThrowNotSimpleAction();
			}

			this.OnPropertyChanged();
		}
	}

	public bool IsSimplePriceApplicable => this.Action is TransactionAction.Buy or TransactionAction.Sell;

	public AccountViewModel? SimpleAccount
	{
		get => this.SimpleAmount > 0 ? this.WithdrawAccount : this.DepositAccount;
		set
		{
			if (this.SimpleAmount > 0)
			{
				this.DepositAccount = this.ThisAccount;
				this.WithdrawAccount = value;
			}
			else
			{
				this.WithdrawAccount = this.ThisAccount;
				this.DepositAccount = value;
			}

			this.OnPropertyChanged();
		}
	}

	public decimal? SimpleCurrencyImpact
	{
		get
		{
			if (this.Action == TransactionAction.Dividend && this.DepositAsset == this.ThisAccount.CurrencyAsset)
			{
				return this.DepositAmount;
			}

			decimal impact = 0;
			if (this.WithdrawAsset == this.ThisAccount.CurrencyAsset && this.WithdrawAmount.HasValue)
			{
				impact -= this.WithdrawAmount.Value;
			}

			if (this.DepositAsset == this.ThisAccount.CurrencyAsset && this.DepositAmount.HasValue)
			{
				impact += this.DepositAmount.Value;
			}

			return impact;
		}
	}

	public string? SimpleCurrencyImpactFormatted => this.ThisAccount.CurrencyAsset?.Format(this.SimpleCurrencyImpact);

	public string Description
	{
		get
		{
			return this.Action switch
			{
				TransactionAction.Add => $"{this.DepositAmount} {this.DepositAsset?.TickerOrName}",
				TransactionAction.Remove => $"{this.WithdrawAmount} {this.WithdrawAsset?.TickerOrName}",
				TransactionAction.Interest => $"+{this.DepositAmountFormatted}",
				TransactionAction.Dividend => $"{this.RelatedAsset?.TickerOrName} +{this.DepositAmountFormatted}",
				TransactionAction.Sell => $"{this.WithdrawAmount} {this.WithdrawAsset?.TickerOrName} @ {this.SimplePriceFormatted}",
				TransactionAction.Buy => $"{this.DepositAmount} {this.DepositAsset?.TickerOrName} @ {this.SimplePriceFormatted}",
				TransactionAction.Transfer => this.ThisAccount == this.DepositAccount ? $"{this.WithdrawAccount?.Name} -> {this.DepositAmountFormatted} {this.DepositAsset?.TickerOrName}" : $"{this.DepositAccount?.Name} <- {this.WithdrawAmountFormatted} {this.WithdrawAsset?.TickerOrName}",
				TransactionAction.Deposit => $"{this.DepositAmountFormatted}",
				TransactionAction.Withdraw => $"{this.WithdrawAmountFormatted}",
				TransactionAction.Exchange => $"{this.WithdrawAmount} {this.WithdrawAsset?.TickerOrName} -> {this.DepositAmount} {this.DepositAsset?.TickerOrName}",
				_ => string.Empty,
			};
		}
	}

	public string? DepositAmountFormatted => this.DepositAsset?.Format(this.DepositAmount);

	public string? WithdrawAmountFormatted => this.WithdrawAsset?.Format(this.WithdrawAmount);

	public string? SimplePriceFormatted => this.ThisAccount.CurrencyAsset?.Format(this.SimplePrice);

	public IEnumerable<AssetViewModel> Assets => this.ThisAccount.DocumentViewModel.AssetsPanel.Assets.Where(a => (this.Action == TransactionAction.Transfer && (this.SimpleAccount is not BankingAccountViewModel || (a.Type == Asset.AssetType.Currency))) || (this.Action != TransactionAction.Transfer && this.IsCashTransaction == (a.Type == Asset.AssetType.Currency)));

	public IEnumerable<AccountViewModel> Accounts => this.ThisAccount.DocumentViewModel.AccountsPanel.Accounts.Where(a => a != this.ThisAccount);

	private bool IsDepositOperation => this.Action is TransactionAction.Buy or TransactionAction.Add or TransactionAction.Deposit or TransactionAction.Interest or TransactionAction.Dividend;

	private bool IsWithdrawOperation => this.Action is TransactionAction.Sell or TransactionAction.Remove or TransactionAction.Withdraw;

	private bool IsCashTransaction => this.Action is TransactionAction.Deposit or TransactionAction.Withdraw;

	protected override void ApplyToCore()
	{
		Verify.Operation(this.Action.HasValue, $"{nameof(this.Action)} must be set first.");

		base.ApplyToCore();
		this.Transaction.Action = this.Action.Value;
		this.Transaction.RelatedAssetId = this.RelatedAsset?.Id;
	}

	protected override void CopyFromCore()
	{
		this.SetProperty(ref this.action, this.Transaction.Action, nameof(this.Action));

		// TODO: code here
		this.RelatedAsset = this.ThisAccount.DocumentViewModel.GetAsset(this.Transaction.RelatedAssetId);

		base.CopyFromCore();
	}

	protected override bool IsPersistedProperty(string propertyName)
	{
		return base.IsPersistedProperty(propertyName)
			&& propertyName is not (nameof(this.SimpleAsset) or nameof(this.SimpleAmount) or nameof(this.SimplePrice) or nameof(this.SimpleCurrencyImpact))
			&& !(propertyName.EndsWith("IsReadOnly") || propertyName.EndsWith("ToolTip") || propertyName.EndsWith("Formatted"));
	}

	[DoesNotReturn]
	private static Exception ThrowNotSimpleAction() => throw new InvalidOperationException("Not a simple operation.");
}
