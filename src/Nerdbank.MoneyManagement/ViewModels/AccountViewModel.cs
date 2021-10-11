﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using System.Diagnostics;
	using Validation;

	[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
	public class AccountViewModel : EntityViewModel<Account>, ITransactionTarget
	{
		private SortedObservableCollection<TransactionViewModel>? transactions;
		private string name = string.Empty;
		private bool isClosed;
		private decimal balance;

		public AccountViewModel(Account? model, DocumentViewModel documentViewModel)
			: base(documentViewModel.MoneyFile)
		{
			this.RegisterDependentProperty(nameof(this.Name), nameof(this.TransferTargetName));
			this.AutoSave = true;

			this.DocumentViewModel = documentViewModel;
			if (model is object)
			{
				this.CopyFrom(model);
			}
		}

		[Required]
		public string Name
		{
			get => this.name;
			set => this.SetProperty(ref this.name, value);
		}

		public string? TransferTargetName => $"[{this.Name}]";

		public bool IsClosed
		{
			get => this.isClosed;
			set => this.SetProperty(ref this.isClosed, value);
		}

		public decimal Balance
		{
			get => this.balance;
			set => this.SetProperty(ref this.balance, value);
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)] // It's lazily initialized, and we don't want the debugger to trip over it.
		public IReadOnlyList<TransactionViewModel> Transactions
		{
			get
			{
				if (this.transactions is null)
				{
					this.transactions = new(TransactionSort.Instance);
					if (this.MoneyFile is object)
					{
						SQLite.TableQuery<Transaction> transactions = this.MoneyFile.Transactions.Where(tx => tx.CreditAccountId == this.Id || tx.DebitAccountId == this.Id);
						foreach (Transaction transaction in transactions)
						{
							TransactionViewModel transactionViewModel = new(this, transaction);
							this.transactions.Add(transactionViewModel);
						}

						this.UpdateBalances(0);
					}

					this.transactions.CollectionChanged += this.Transactions_CollectionChanged;
				}

				return this.transactions;
			}
		}

		internal DocumentViewModel DocumentViewModel { get; }

		private string? DebuggerDisplay => this.Name;

		/// <summary>
		/// Creates a new <see cref="TransactionViewModel"/> for this account,
		/// but does <em>not</em> add it to the collection.
		/// </summary>
		/// <param name="volatileOnly"><see langword="true"/> to only create the view model without adding it to the <see cref="Transactions"/> collection or saving it to the database; <see langword="false"/> otherwise.</param>
		/// <returns>A new <see cref="TransactionViewModel"/> for an uninitialized transaction.</returns>
		public TransactionViewModel NewTransaction(bool volatileOnly = false)
		{
			// Make sure our collection has been initialized by now.
			_ = this.Transactions;

			TransactionViewModel viewModel = new(this, null);
			viewModel.When = DateTime.Now;
			viewModel.Model = new();

			if (!volatileOnly)
			{
				_ = this.Transactions; // make sure our collection is initialized.
				this.transactions!.Add(viewModel);
				viewModel.Save();
			}

			return viewModel;
		}

		public int Add(TransactionViewModel transaction)
		{
			_ = this.Transactions;
			return this.transactions!.Add(transaction);
		}

		public void DeleteTransaction(TransactionViewModel transaction)
		{
			Requires.Argument(transaction.ThisAccount == this, nameof(transaction), "This transaction does not belong to this account.");
			Verify.Operation(this.transactions is object, "Our transactions are not initialized yet.");

			if (this.MoneyFile is object && transaction.Model is object)
			{
				if (!this.MoneyFile.Delete(transaction.Model))
				{
					// We may be removing a view model whose model was never persisted. Make sure we directly remove the view model from our own collection.
					this.RemoveTransactionFromViewModel(transaction);
				}
			}
			else
			{
				int index = this.transactions.Remove(transaction);
				if (index >= 0)
				{
					this.UpdateBalances(index);
				}
			}
		}

		internal void NotifyTransactionDeleted(Transaction transaction)
		{
			if (this.transactions is null)
			{
				// Nothing to refresh.
				return;
			}

			if (this.FindTransaction(transaction.Id) is { } transactionViewModel)
			{
				this.RemoveTransactionFromViewModel(transactionViewModel);
			}
		}

		internal void NotifyTransactionChanged(Transaction transaction)
		{
			if (this.transactions is null)
			{
				// Nothing to refresh.
				return;
			}

			// This transaction may have added or dropped our account as a transfer
			bool removedFromAccount = transaction.CreditAccountId != this.Id && transaction.DebitAccountId != this.Id;
			if (this.FindTransaction(transaction.Id) is { } transactionViewModel)
			{
				if (removedFromAccount)
				{
					this.transactions.Remove(transactionViewModel);
				}
				else
				{
					transactionViewModel.CopyFrom(transaction);
					int index = this.transactions.IndexOf(transactionViewModel);
					if (index >= 0)
					{
						this.UpdateBalances(index);
					}
				}
			}
			else if (!removedFromAccount)
			{
				// This may be a new transaction we need to add.
				this.transactions.Add(new TransactionViewModel(this, transaction));
			}
		}

		protected override void ApplyToCore(Account account)
		{
			Requires.NotNull(account, nameof(account));

			account.Name = this.name;
			account.IsClosed = this.IsClosed;
		}

		protected override void CopyFromCore(Account account)
		{
			Requires.NotNull(account, nameof(account));

			this.Name = account.Name;
			this.IsClosed = account.IsClosed;

			if (this.MoneyFile is object && account is object)
			{
				this.balance = this.MoneyFile.GetBalance(account);
			}

			// Force reinitialization.
			this.transactions = null;
		}

		protected override bool IsPersistedProperty(string propertyName) => propertyName is not nameof(this.Balance);

		private void Transactions_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.NewStartingIndex >= 0)
			{
				if (e.OldStartingIndex >= 0)
				{
					this.UpdateBalances(e.NewStartingIndex, e.OldStartingIndex);
				}
				else
				{
					this.UpdateBalances(e.NewStartingIndex);
				}
			}
			else if (e.OldStartingIndex >= 0)
			{
				this.UpdateBalances(e.OldStartingIndex);
			}
		}

		private void RemoveTransactionFromViewModel(TransactionViewModel transactionViewModel)
		{
			if (this.transactions is null)
			{
				// Nothing to remove when the collection isn't initialized.
				return;
			}

			int index = this.transactions.IndexOf(transactionViewModel);
			this.transactions.RemoveAt(index);
			this.UpdateBalances(index);
		}

		private TransactionViewModel? FindTransaction(int id)
		{
			foreach (TransactionViewModel transactionViewModel in this.Transactions)
			{
				if (transactionViewModel.Model?.Id == id)
				{
					return transactionViewModel;
				}
			}

			return null;
		}

		private void UpdateBalances(int changedIndex1, int changedIndex2 = -1)
		{
			int startingIndex = changedIndex2 == -1 ? changedIndex1 : Math.Min(changedIndex1, changedIndex2);
			decimal balance = startingIndex == 0 ? 0m : this.transactions![startingIndex - 1].Balance;
			for (int i = startingIndex; i < this.transactions!.Count; i++)
			{
				TransactionViewModel transaction = this.transactions[i];
				balance += transaction.Amount;
				if (transaction.Balance == balance && i >= changedIndex2)
				{
					// The balance is already what it needs to be,
					// and we've already reached the last of the one or two positions where transactions may have changed.
					// Short circuit as a perf win.
					return;
				}

				transaction.Balance = balance;
			}
		}

		private class TransactionSort : IOptimizedComparer<TransactionViewModel>
		{
			internal static readonly TransactionSort Instance = new();

			private TransactionSort()
			{
			}

			public int Compare(TransactionViewModel? x, TransactionViewModel? y)
			{
				if (x is null)
				{
					return y is null ? 0 : -1;
				}
				else if (y is null)
				{
					return 1;
				}

				int order = x.When.CompareTo(y.When);
				if (order != 0)
				{
					return order;
				}

				order = -x.Amount.CompareTo(y.Amount);
				if (order != 0)
				{
					return order;
				}

				order = x.Id is null
					? (y.Id is null ? 0 : -1)
					: (y.Id is null) ? 1 : 0;
				if (order != 0)
				{
					return order;
				}

				return x.Payee?.CompareTo(y.Payee) ?? 0;
			}

			public bool IsPropertySignificant(string propertyName) => propertyName is nameof(TransactionViewModel.When) or nameof(TransactionViewModel.Amount) or nameof(TransactionViewModel.Id) or nameof(TransactionViewModel.Payee);
		}
	}
}
