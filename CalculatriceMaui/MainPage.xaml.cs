using CalculatriceMaui.Models;
using CalculatriceMaui.ViewModels;

namespace CalculatriceMaui;

/// <summary>
/// Écran principal de la calculatrice.
/// Le code-behind sert de pont entre le XAML (gestionnaires d'événements) et le
/// ViewModel, et réorganise la Grid racine selon l'orientation de l'appareil.
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly CalculatorViewModel _viewModel = new();
    private bool _isLandscape;

    public MainPage()
    {
        InitializeComponent();
        BindingContext = _viewModel;
    }

    // ------------------------------------------------------- gestionnaires d'événements

    private void OnDigitClicked(object sender, EventArgs e) =>
        _viewModel.InputDigit(((Button)sender).Text);

    private void OnDecimalClicked(object sender, EventArgs e) => _viewModel.InputDecimal();

    private void OnOperatorClicked(object sender, EventArgs e) =>
        _viewModel.SetOperator(((Button)sender).Text);

    private void OnEqualsClicked(object sender, EventArgs e) => _viewModel.ApplyEquals();

    private void OnClearAllClicked(object sender, EventArgs e) => _viewModel.ClearAll();

    private void OnClearEntryClicked(object sender, EventArgs e) => _viewModel.ClearEntry();

    private void OnBackspaceClicked(object sender, EventArgs e) => _viewModel.Backspace();

    private void OnToggleSignClicked(object sender, EventArgs e) => _viewModel.ToggleSign();

    private void OnPercentClicked(object sender, EventArgs e) => _viewModel.Percent();

    // Opérations avancées
    private void OnSqrtClicked(object sender, EventArgs e) => _viewModel.ApplyUnary("√");

    private void OnSquareClicked(object sender, EventArgs e) => _viewModel.ApplyUnary("x²");

    private void OnReciprocalClicked(object sender, EventArgs e) => _viewModel.ApplyUnary("1/x");

    private void OnPowerClicked(object sender, EventArgs e) => _viewModel.SetOperator("^");

    // Mémoire
    private void OnMemoryAddClicked(object sender, EventArgs e) => _viewModel.MemoryAdd();

    private void OnMemorySubtractClicked(object sender, EventArgs e) => _viewModel.MemorySubtract();

    private void OnMemoryRecallClicked(object sender, EventArgs e) => _viewModel.MemoryRecall();

    private void OnMemoryClearClicked(object sender, EventArgs e) => _viewModel.MemoryClear();

    // Historique
    private void OnHistoryClicked(object sender, EventArgs e)
    {
        _viewModel.ToggleHistory();

        if (_viewModel.IsHistoryVisible)
        {
            DisplayPanel.ScrollToAsync(0, 0, animated: false);
        }
    }

    private void OnClearHistoryClicked(object sender, EventArgs e) => _viewModel.ClearHistory();

    private void OnHistoryItemTapped(object sender, TappedEventArgs e)
    {
        if (sender is BindableObject item && item.BindingContext is HistoryEntry entry)
        {
            _viewModel.UseHistoryValue(entry);
        }
    }

    // ------------------------------------------------------------ disposition adaptative

    private void OnPageSizeChanged(object sender, EventArgs e) =>
        ApplyOrientation(Width > 0 && Height > 0 && Width > Height);

    /// <summary>
    /// Portrait  : 3 lignes (en-tête / affichage / clavier).
    /// Paysage   : l'affichage passe à gauche, le clavier à droite.
    /// </summary>
    private void ApplyOrientation(bool landscape)
    {
        if (_isLandscape == landscape)
        {
            return;
        }

        _isLandscape = landscape;

        if (landscape)
        {
            SetRowDefinitions(GridLength.Auto, GridLength.Star);
            SetColumnDefinitions(
                new GridLength(1, GridUnitType.Star),
                new GridLength(1.2, GridUnitType.Star));

            RootGrid.ColumnSpacing = 12;

            Place(HeaderBar, 0, 0, 2);
            Place(DisplayPanel, 1, 0, 1);
            Place(KeypadPanel, 1, 1, 1);
            Place(StatusOverlay, 0, 0, 2);
        }
        else
        {
            SetRowDefinitions(
                GridLength.Auto,
                GridLength.Auto,
                GridLength.Star);

            SetColumnDefinitions(new GridLength(1, GridUnitType.Star));

            RootGrid.ColumnSpacing = 0;

            Place(HeaderBar, 0, 0, 1);
            Place(DisplayPanel, 1, 0, 1);
            Place(KeypadPanel, 2, 0, 1);
            Place(StatusOverlay, 0, 0, 1);
        }
    }

    private void SetRowDefinitions(params GridLength[] heights)
    {
        RootGrid.RowDefinitions.Clear();

        foreach (GridLength height in heights)
        {
            RootGrid.RowDefinitions.Add(new RowDefinition(height));
        }
    }

    private void SetColumnDefinitions(params GridLength[] widths)
    {
        RootGrid.ColumnDefinitions.Clear();

        foreach (GridLength width in widths)
        {
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(width));
        }
    }

    private static void Place(BindableObject view, int row, int column, int columnSpan)
    {
        Grid.SetRow(view, row);
        Grid.SetColumn(view, column);
        Grid.SetColumnSpan(view, columnSpan);
    }
}
