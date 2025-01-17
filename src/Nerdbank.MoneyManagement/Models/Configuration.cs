﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement;

public class Configuration : ModelBase
{
	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="Asset"/> that is used for displaying the value of accounts.
	/// </summary>
	public int PreferredAssetId { get; set; }
}
