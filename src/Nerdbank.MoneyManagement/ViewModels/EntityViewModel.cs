﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using PCLCommandBase;
using Validation;

namespace Nerdbank.MoneyManagement.ViewModels;

public abstract class EntityViewModel : BindableBase, IDataErrorInfo
{
	public EntityViewModel(MoneyFile moneyFile)
	{
		this.MoneyFile = moneyFile ?? throw new ArgumentNullException(nameof(moneyFile));

		this.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName is object && this.IsPersistedProperty(e.PropertyName))
			{
				this.IsDirty = true;
				if (this.MoneyFile.IsDisposed is not true && this.AutoSave && string.IsNullOrEmpty(this.Error))
				{
					this.Save();
				}
			}
		};
	}

	/// <summary>
	/// Occurs when the entity is saved to the database.
	/// </summary>
	public event EventHandler? Saved;

	/// <inheritdoc cref="ModelBase.IsPersisted"/>
	public abstract bool IsPersisted { get; }

	/// <summary>
	/// Gets or sets a value indicating whether this entity has been changed since <see cref="ApplyToModel"/> was last called.
	/// </summary>
	public bool IsDirty { get; set; }

	public virtual string Error
	{
		get
		{
			PropertyInfo[] propertyInfos = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

			foreach (PropertyInfo propertyInfo in propertyInfos)
			{
				if (propertyInfo.DeclaringType?.IsAssignableFrom(typeof(EntityViewModel)) is false)
				{
					var errorMsg = this[propertyInfo.Name];
					if (errorMsg?.Length > 0)
					{
						return errorMsg;
					}
				}
			}

			return string.Empty;
		}
	}

	/// <summary>
	/// Gets the file to which this view model should be saved or from which it should be refreshed.
	/// </summary>
	public MoneyFile MoneyFile { get; }

	/// <summary>
	/// Gets or sets a value indicating whether changes to this view model are automatically persisted to the model.
	/// </summary>
	protected bool AutoSave { get; set; } = true;

	protected abstract ModelBase? UndoTarget { get; }

	public virtual string this[string columnName]
	{
		get
		{
			var validationResults = new List<ValidationResult>();

			PropertyInfo? property = this.GetType().GetProperties().FirstOrDefault(p => p.Name == columnName);
			Requires.Argument(property is not null, nameof(columnName), "No property by that name.");

			ValidationContext validationContext = new(this)
			{
				MemberName = columnName,
			};

			var isValid = Validator.TryValidateProperty(property.GetValue(this), validationContext, validationResults);
			if (isValid)
			{
				return string.Empty;
			}

			return validationResults.First().ErrorMessage ?? string.Empty;
		}
	}

	public void Save()
	{
		this.ApplyToModel();
		if (!this.MoneyFile.IsDisposed)
		{
			bool wasPersisted = this.IsPersisted;
			using IDisposable? transaction = this.MoneyFile.UndoableTransaction((this.IsPersisted ? "Update" : "Add") + $" {this.GetType().Name}", this.UndoTarget);

			this.SaveCore();

			this.Saved?.Invoke(this, EventArgs.Empty);

			if (!wasPersisted)
			{
				this.OnPropertyChanged(nameof(this.IsPersisted));
			}
		}
	}

	/// <summary>
	/// Writes this view model to the underlying model.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="Error"/> is non-empty.</exception>
	public virtual void ApplyToModel()
	{
		Verify.Operation(string.IsNullOrEmpty(this.Error), "View model is not in a valid state. Check the " + nameof(this.Error) + " property. " + this.Error);
		this.ApplyToCore();
		this.IsDirty = false;
	}

	protected abstract void SaveCore();

	protected abstract void ApplyToCore();

	protected virtual bool IsPersistedProperty(string propertyName) => true;

	protected AutoSaveSuspension SuspendAutoSave(bool saveOnDisposal = true) => new(this, saveOnDisposal);

	protected struct AutoSaveSuspension : IDisposable
	{
		private readonly EntityViewModel entity;
		private readonly bool saveOnDisposal;
		private readonly bool oldAutoSave;

		internal AutoSaveSuspension(EntityViewModel entity, bool saveOnDisposal)
		{
			this.entity = entity;
			this.saveOnDisposal = saveOnDisposal;
			this.oldAutoSave = entity.AutoSave;
			entity.AutoSave = false;
		}

		public void Dispose()
		{
			this.entity.AutoSave = this.oldAutoSave;
			if (this.saveOnDisposal && this.entity.IsDirty)
			{
				this.entity.Save();
			}
		}
	}
}
