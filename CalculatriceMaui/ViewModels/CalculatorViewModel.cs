using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using CalculatriceMaui.Models;

namespace CalculatriceMaui.ViewModels;

/// <summary>
/// ViewModel de l'écran principal : relaie les événements du code-behind vers le
/// moteur de calcul et expose l'état à l'interface sous forme de propriétés
/// liables (pattern MVVM + INotifyPropertyChanged).
/// </summary>
public sealed class CalculatorViewModel : INotifyPropertyChanged
{
    private readonly CalculatorEngine _engine = new();
    private CancellationTokenSource? _statusCts;

    private string _display = "0";
    private string _expression = string.Empty;
    private string _memoryText = "M";
    private string _statusMessage = string.Empty;
    private bool _statusIsError;
    private bool _isStatusVisible;
    private bool _hasError;
    private bool _hasMemory;
    private bool _isHistoryVisible;

    public CalculatorViewModel()
    {
        RefreshDisplay();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Historique des calculs, du plus récent au plus ancien.</summary>
    public ObservableCollection<HistoryEntry> History { get; } = new();

    public string Display
    {
        get => _display;
        private set
        {
            if (_display == value)
            {
                return;
            }

            _display = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayFontSize));
        }
    }

    public string Expression
    {
        get => _expression;
        private set
        {
            if (_expression == value)
            {
                return;
            }

            _expression = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Taille de police adaptée à la longueur du nombre affiché.</summary>
    public double DisplayFontSize => _display.Length switch
    {
        <= 7 => 46,
        <= 10 => 38,
        <= 13 => 32,
        <= 16 => 27,
        _ => 22
    };

    public bool HasError
    {
        get => _hasError;
        private set
        {
            if (_hasError == value)
            {
                return;
            }

            _hasError = value;
            OnPropertyChanged();
        }
    }

    public bool HasMemory
    {
        get => _hasMemory;
        private set
        {
            if (_hasMemory == value)
            {
                return;
            }

            _hasMemory = value;
            OnPropertyChanged();
        }
    }

    public string MemoryText
    {
        get => _memoryText;
        private set
        {
            if (_memoryText == value)
            {
                return;
            }

            _memoryText = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public bool StatusIsError
    {
        get => _statusIsError;
        private set
        {
            if (_statusIsError == value)
            {
                return;
            }

            _statusIsError = value;
            OnPropertyChanged();
        }
    }

    public bool IsStatusVisible
    {
        get => _isStatusVisible;
        private set
        {
            if (_isStatusVisible == value)
            {
                return;
            }

            _isStatusVisible = value;
            OnPropertyChanged();
        }
    }

    public bool IsHistoryVisible
    {
        get => _isHistoryVisible;
        private set
        {
            if (_isHistoryVisible == value)
            {
                return;
            }

            _isHistoryVisible = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HistoryButtonText));
        }
    }

    public string HistoryButtonText => _isHistoryVisible ? "Fermer" : $"Historique ({History.Count})";

    // ------------------------------------------------------------- actions

    public void InputDigit(string digit)
    {
        if (string.IsNullOrEmpty(digit))
        {
            return;
        }

        Apply(_engine.InputDigit(digit[^1]));
    }

    public void InputDecimal() => Apply(_engine.InputDecimal());

    public void ToggleSign() => Apply(_engine.ToggleSign());

    public void Backspace() => Apply(_engine.Backspace());

    public void ClearAll()
    {
        _engine.ClearAll();
        RefreshDisplay();
    }

    public void ClearEntry() => Apply(_engine.ClearEntry());

    public void SetOperator(string symbol) => Apply(_engine.SetOperator(ParseOperator(symbol)));

    public void ApplyEquals() => Apply(_engine.ApplyEquals());

    public void ApplyUnary(string symbol) => Apply(_engine.ApplyUnary(ParseUnary(symbol)));

    public void Percent() => Apply(_engine.ApplyUnary(UnaryOperator.Percent));

    public void MemoryAdd() => Apply(_engine.MemoryAdd());

    public void MemorySubtract() => Apply(_engine.MemorySubtract());

    public void MemoryRecall() => Apply(_engine.MemoryRecall());

    public void MemoryClear() => Apply(_engine.MemoryClear());

    public void ToggleHistory() => IsHistoryVisible = !IsHistoryVisible;

    public void ClearHistory()
    {
        History.Clear();
        OnPropertyChanged(nameof(HistoryButtonText));
    }

    /// <summary>Réutilise le résultat d'un calcul de l'historique.</summary>
    public void UseHistoryValue(HistoryEntry entry)
    {
        if (entry is null || !double.TryParse(entry.Result.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
        {
            ShowStatus("Valeur d'historique illisible.", isError: true);
            return;
        }

        Apply(_engine.LoadValue(value));
    }

    // -------------------------------------------------------------- interne

    private void Apply(CalculatorAction action)
    {
        if (action.Record is not null)
        {
            History.Insert(0, action.Record);

            while (History.Count > CalculatorEngine.MaxHistorySize)
            {
                History.RemoveAt(History.Count - 1);
            }

            OnPropertyChanged(nameof(HistoryButtonText));
        }

        RefreshDisplay();

        if (!string.IsNullOrEmpty(action.Message))
        {
            ShowStatus(action.Message, action.IsError);
        }
    }

    private void RefreshDisplay()
    {
        HasError = _engine.CurrentEntry == "Erreur";
        Display = _engine.CurrentEntry;
        Expression = _engine.Expression;
        HasMemory = _engine.HasMemory;
        MemoryText = _engine.HasMemory ? $"M {CalculatorEngine.Format(_engine.Memory!.Value)}" : "M";
    }

    private async void ShowStatus(string message, bool isError)
    {
        _statusCts?.Cancel();
        _statusCts = new CancellationTokenSource();
        CancellationToken token = _statusCts.Token;

        StatusMessage = message;
        StatusIsError = isError;
        IsStatusVisible = true;

        try
        {
            await Task.Delay(isError ? 3200 : 2000, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!token.IsCancellationRequested)
        {
            IsStatusVisible = false;
        }
    }

    private static CalculatorOperator ParseOperator(string symbol) => symbol switch
    {
        "+" => CalculatorOperator.Add,
        "−" or "-" => CalculatorOperator.Subtract,
        "×" or "*" or "x" => CalculatorOperator.Multiply,
        "÷" or "/" => CalculatorOperator.Divide,
        "^" => CalculatorOperator.Power,
        _ => CalculatorOperator.None
    };

    private static UnaryOperator ParseUnary(string symbol) => symbol switch
    {
        "√" => UnaryOperator.SquareRoot,
        "x²" => UnaryOperator.Square,
        "1/x" => UnaryOperator.Reciprocal,
        "%" => UnaryOperator.Percent,
        "±" => UnaryOperator.Negate,
        _ => UnaryOperator.Negate
    };

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
