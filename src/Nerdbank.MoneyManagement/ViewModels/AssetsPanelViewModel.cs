﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections;
using System.Collections.Specialized;
using Microsoft;
using PCLCommandBase;

namespace Nerdbank.MoneyManagement.ViewModels;

public class AssetsPanelViewModel : BindableBase
{
	private readonly SortedObservableCollection<AssetViewModel> assets = new(AssetSort.Instance);
	private readonly SortedObservableCollection<AssetPriceViewModel> assetPrices = new(AssetPriceSort.Instance);
	private readonly DocumentViewModel documentViewModel;
	private AssetViewModel? selectedAsset;
	private AssetPriceViewModel? selectedAssetPrice;
	private IList? selectedAssetPrices;

	public AssetsPanelViewModel(DocumentViewModel documentViewModel)
	{
		this.RegisterDependentProperty(nameof(this.SelectedAsset), nameof(this.IsPricesGridVisible));
		this.AddAssetCommand = new AddAssetCommandImpl(this);
		this.DeleteAssetCommand = new DeleteAssetCommandImpl(this);
		this.DeletePriceCommand = new DeletePriceCommandImpl(this);
		this.documentViewModel = documentViewModel;
	}

	/// <summary>
	/// Occurs when a new asset is being interactively created.
	/// </summary>
	public event EventHandler? AddingNewAsset;

	public string Title => "Assets";

	public CommandBase AddAssetCommand { get; }

	public CommandBase DeleteAssetCommand { get; }

	public CommandBase DeletePriceCommand { get; }

	public string NameLabel => "_Name";

	public int NameMaxLength => 50;

	public string TickerSymbolLabel => "_Ticker";

	public int TickerSymbolMaxLength => 50;

	public string TypeLabel => "T_ype";

	public string CurrencySymbolLabel => "Currency symbol";

	public string CurrencyDecimalDigitsLabel => "Decimal digits";

	public string PriceGridWhenColumnHeader => "Date";

	public string PriceGridPriceColumnHeader => "Price";

	/// <summary>
	/// Gets or sets the selected asset.
	/// </summary>
	public AssetViewModel? SelectedAsset
	{
		get => this.selectedAsset;
		set
		{
			if (this.selectedAsset != value)
			{
				this.SetProperty(ref this.selectedAsset, value);
				this.FillAssetPrices();
			}
		}
	}

	public bool IsPricesGridVisible => this.SelectedAsset is object && this.SelectedAsset.Id != this.documentViewModel.MoneyFile.PreferredAssetId;

	public IReadOnlyList<AssetViewModel> Assets => this.assets;

	/// <summary>
	/// Gets the prices of the <see cref="SelectedAsset"/>.
	/// </summary>
	public IReadOnlyList<AssetPriceViewModel> AssetPrices => this.assetPrices;

	public AssetPriceViewModel? SelectedAssetPrice
	{
		get => this.selectedAssetPrice;
		set => this.SetProperty(ref this.selectedAssetPrice, value);
	}

	/// <summary>
	/// Gets or sets a collection of selected asset prices.
	/// </summary>
	/// <remarks>
	/// This is optional. When set, the <see cref="DeletePriceCommand"/> will use this collection as the set of prices to delete.
	/// When not set, the <see cref="SelectedAssetPrice"/> will be used by the <see cref="DeletePriceCommand"/>.
	/// </remarks>
	public IList? SelectedAssetPrices
	{
		get => this.selectedAssetPrices;
		set => this.SetProperty(ref this.selectedAssetPrices, value);
	}

	internal AssetViewModel? AddingAsset { get; set; }

	public AssetViewModel NewAsset(string name = "", string tickerSymbol = "")
	{
		this.AddingNewAsset?.Invoke(this, EventArgs.Empty);
		if (this.AddingAsset is object)
		{
			this.SelectedAsset = this.AddingAsset;
			return this.AddingAsset;
		}

		AssetViewModel newAssetViewModel = new(
			new Asset { Type = Asset.AssetType.Security },
			this.documentViewModel);

		this.assets.Add(newAssetViewModel);
		if (string.IsNullOrEmpty(name))
		{
			this.AddingAsset = newAssetViewModel;
			newAssetViewModel.NotifyWhenValid(s =>
			{
				if (this.AddingAsset == s)
				{
					this.AddingAsset = null;
				}
			});
		}
		else
		{
			newAssetViewModel.Name = name;
		}

		newAssetViewModel.TickerSymbol = tickerSymbol;
		this.SelectedAsset = newAssetViewModel;
		return newAssetViewModel;
	}

	public void DeleteAsset(AssetViewModel asset)
	{
		this.assets.Remove(asset);

		using IDisposable? transaction = this.documentViewModel.MoneyFile.UndoableTransaction($"Deleted asset \"{asset.Name}\".", asset.Model);
		this.documentViewModel.MoneyFile.Delete(asset.Model);

		if (this.SelectedAsset == asset)
		{
			this.SelectedAsset = null;
		}

		if (this.AddingAsset == asset)
		{
			this.AddingAsset = null;
		}
	}

	public AssetViewModel? FindAsset(int id) => this.assets?.FirstOrDefault(a => a.Id == id);

	public AssetViewModel? FindAsset(string name) => this.assets?.FirstOrDefault(a => a.Name == name);

	public void DeletePrice(AssetPriceViewModel value)
	{
		Requires.Argument(value.IsPersisted, nameof(value), "Cannot delete the volatile price.");
		this.assetPrices.Remove(value);
		this.documentViewModel.MoneyFile.Delete(value.Model);

		if (value == this.SelectedAssetPrice)
		{
			this.SelectedAssetPrice = null;
		}
	}

	internal void Add(AssetViewModel asset)
	{
		this.assets.Add(asset);
	}

	internal void ClearViewModel()
	{
		this.assets.Clear();
		this.SelectedAsset = null;
		this.SelectedAssetPrice = null;
		this.SelectedAssetPrices?.Clear();
	}

	private void FillAssetPrices()
	{
		this.assetPrices.Clear();
		if (this.selectedAsset is object)
		{
			IEnumerable<AssetPriceViewModel> prices =
				from assetPriceViewModel in this.documentViewModel.MoneyFile.AssetPrices
				where assetPriceViewModel.AssetId == this.selectedAsset.Id && assetPriceViewModel.ReferenceAssetId == this.documentViewModel.MoneyFile.PreferredAssetId
				orderby assetPriceViewModel.When descending
				select new AssetPriceViewModel(this.documentViewModel, assetPriceViewModel);
			this.assetPrices.AddRange(prices);

			// Add the placeholder row for manual entry of a new price point.
			if (this.selectedAsset.IsPersisted)
			{
				this.CreateVolatilePricePoint();
			}
			else
			{
				this.selectedAsset.Saved += this.SelectedAsset_Saved;
			}
		}
	}

	private void SelectedAsset_Saved(object? sender, EventArgs e)
	{
		if (sender is object && sender == this.SelectedAsset)
		{
			this.SelectedAsset.Saved -= this.SelectedAsset_Saved;
			this.CreateVolatilePricePoint();
		}
	}

	private void CreateVolatilePricePoint()
	{
		Verify.Operation(this.SelectedAsset is object, "Cannot generate volatile row without a selected asset.");

		// Always add one more "volatile" pricepoint as a placeholder to add new data.
		var volatileModel = new AssetPrice
		{
			When = DateTime.Today,
			AssetId = this.SelectedAsset.Id,
			ReferenceAssetId = this.documentViewModel.MoneyFile.PreferredAssetId,
		};
		AssetPriceViewModel volatileViewModel = new(this.documentViewModel, volatileModel);
		this.assetPrices.Add(volatileViewModel);
		volatileViewModel.Saved += this.VolatileAssetPrice_Saved;
	}

	private void VolatileAssetPrice_Saved(object? sender, EventArgs e)
	{
		AssetPriceViewModel? volatilePrice = (AssetPriceViewModel?)sender;
		Assumes.NotNull(volatilePrice);
		volatilePrice.Saved -= this.VolatileAssetPrice_Saved;

		// We need a new volatile row.
		this.CreateVolatilePricePoint();
	}

	private class AssetSort : IComparer<AssetViewModel>
	{
		internal static readonly AssetSort Instance = new AssetSort();

		private AssetSort()
		{
		}

		public int Compare(AssetViewModel? x, AssetViewModel? y)
		{
			if (x is null)
			{
				return y is null ? 0 : -1;
			}
			else if (y is null)
			{
				return 1;
			}

			int order = Utilities.CompareNullOrZeroComesLast(x.Id, y.Id);
			if (order != 0)
			{
				return order;
			}

			return StringComparer.CurrentCultureIgnoreCase.Compare(x.Name, y.Name);
		}
	}

	private class AssetPriceSort : IComparer<AssetPriceViewModel>
	{
		internal static readonly AssetPriceSort Instance = new AssetPriceSort();

		private AssetPriceSort()
		{
		}

		public int Compare(AssetPriceViewModel? x, AssetPriceViewModel? y)
		{
			if (x is null)
			{
				return y is null ? 0 : -1;
			}
			else if (y is null)
			{
				return 1;
			}

			int order = -Utilities.CompareNullOrZeroComesLast(x.Id, y.Id);
			if (order != 0)
			{
				return order;
			}

			return -StringComparer.CurrentCultureIgnoreCase.Compare(x.When, y.When);
		}
	}

	private class AddAssetCommandImpl : CommandBase
	{
		private readonly AssetsPanelViewModel viewModel;

		internal AddAssetCommandImpl(AssetsPanelViewModel viewModel)
		{
			this.viewModel = viewModel;
		}

		public string Caption => "Add new";

		protected override Task ExecuteCoreAsync(object? parameter = null, CancellationToken cancellationToken = default)
		{
			this.viewModel.NewAsset();
			return Task.CompletedTask;
		}
	}

	private class DeleteAssetCommandImpl : CommandBase
	{
		private readonly AssetsPanelViewModel viewModel;

		internal DeleteAssetCommandImpl(AssetsPanelViewModel viewModel)
		{
			this.viewModel = viewModel;
			viewModel.PropertyChanged += this.ViewModel_PropertyChanged;
		}

		public string Caption => "Delete";

		public override bool CanExecute(object? parameter = null) => base.CanExecute(parameter) && this.viewModel.SelectedAsset is object;

		protected override Task ExecuteCoreAsync(object? parameter = null, CancellationToken cancellationToken = default)
		{
			AssetViewModel asset = this.viewModel.SelectedAsset ?? throw new InvalidOperationException("Select an asset first.");

			this.viewModel.DeleteAsset(asset);
			return Task.CompletedTask;
		}

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(this.viewModel.SelectedAsset))
			{
				this.OnCanExecuteChanged();
			}
		}
	}

	private class DeletePriceCommandImpl : CommandBase
	{
		private readonly AssetsPanelViewModel viewModel;
		private INotifyCollectionChanged? subscribedSelectedPrices;

		internal DeletePriceCommandImpl(AssetsPanelViewModel viewModel)
		{
			this.viewModel = viewModel;
			this.viewModel.PropertyChanged += this.ViewModel_PropertyChanged;
			this.SubscribeToSelectionChanged();
		}

		public string Caption => "Delete";

		public override bool CanExecute(object? parameter = null) => base.CanExecute(parameter) && (this.viewModel.SelectedAssetPrice is { IsPersisted: true } || this.viewModel.SelectedAssetPrices?.Count > 0);

		protected override Task ExecuteCoreAsync(object? parameter = null, CancellationToken cancellationToken = default)
		{
			if (this.viewModel.SelectedAssetPrices is object)
			{
				using IDisposable? undo = this.viewModel.documentViewModel.MoneyFile.UndoableTransaction($"Deleting {this.viewModel.SelectedAssetPrices.Count} prices for \"{this.viewModel.SelectedAsset?.Name}\".", this.viewModel.SelectedAssetPrices.OfType<AssetPriceViewModel>().FirstOrDefault()?.Model);
				foreach (AssetPriceViewModel pricepoint in this.viewModel.SelectedAssetPrices.OfType<AssetPriceViewModel>().ToList())
				{
					if (pricepoint.IsPersisted)
					{
						this.viewModel.DeletePrice(pricepoint);
					}
				}
			}
			else if (this.viewModel.SelectedAssetPrice is { IsPersisted: true })
			{
				using IDisposable? undo = this.viewModel.documentViewModel.MoneyFile.UndoableTransaction($"Deleting \"{this.viewModel.SelectedAsset?.Name}\" price for {this.viewModel.SelectedAssetPrice.When}.", this.viewModel.SelectedAssetPrice.Model);
				this.viewModel.DeletePrice(this.viewModel.SelectedAssetPrice);
			}

			return Task.CompletedTask;
		}

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(this.viewModel.SelectedAssetPrices))
			{
				this.SubscribeToSelectionChanged();
			}
			else if (e.PropertyName is nameof(this.viewModel.SelectedAssetPrice))
			{
				this.OnCanExecuteChanged();
			}
		}

		private void SubscribeToSelectionChanged()
		{
			if (this.subscribedSelectedPrices is object)
			{
				this.subscribedSelectedPrices.CollectionChanged -= this.SelectedPrices_Changed;
			}

			this.subscribedSelectedPrices = this.viewModel.SelectedAssetPrices as INotifyCollectionChanged;
			if (this.subscribedSelectedPrices is object)
			{
				this.subscribedSelectedPrices.CollectionChanged += this.SelectedPrices_Changed;
			}
		}

		private void SelectedPrices_Changed(object? sender, NotifyCollectionChangedEventArgs e) => this.OnCanExecuteChanged();
	}
}
