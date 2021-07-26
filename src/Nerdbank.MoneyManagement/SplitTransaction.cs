﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement
{
	using SQLite;

	/// <summary>
	/// Represents one line of a split transaction.
	/// </summary>
	public class SplitTransaction
	{
		/// <summary>
		/// Gets or sets the primary key of this database entity.
		/// </summary>
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="Transaction.Id"/> of the <see cref="Transaction"/> to which this split belongs.
		/// </summary>
		[NotNull]
		public int TransactionId { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="Category.Id"/> of the <see cref="Category"/> assigned to this line of the split transaction.
		/// </summary>
		public int? CategoryId { get; set; }

		/// <summary>
		/// Gets or sets a memo to go with this line of the split transaction.
		/// </summary>
		public string? Memo { get; set; }

		/// <summary>
		/// Gets or sets the amount of this line of the split transaction. Always non-negative.
		/// </summary>
		[NotNull]
		public decimal Amount { get; set; }
	}
}
