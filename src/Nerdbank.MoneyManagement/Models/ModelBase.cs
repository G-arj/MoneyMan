﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using SQLite;

namespace Nerdbank.MoneyManagement;

public abstract class ModelBase
{
	/// <summary>
	/// Gets or sets the primary key of this database entity.
	/// </summary>
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }

	/// <summary>
	/// Gets a value indicating whether this entity has already received an assigned primary key from the database.
	/// </summary>
	public bool IsPersisted => this.Id > 0;
}
