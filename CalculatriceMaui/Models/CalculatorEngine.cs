using System.Globalization;

namespace CalculatriceMaui.Models;

/// <summary>Opérations binaires gérées par la calculatrice.</summary>
public enum CalculatorOperator
{
    None,
    Add,
    Subtract,
    Multiply,
    Divide,
    Power
}

/// <summary>Opérations appliquées immédiatement à la valeur courante.</summary>
public enum UnaryOperator
{
    Negate,
    SquareRoot,
    Square,
    Reciprocal,
    Percent
}

/// <summary>Un calcul terminé, conservé dans l'historique.</summary>
public sealed record HistoryEntry(string Expression, string Result, DateTime Timestamp);

/// <summary>
/// Résultat d'une action utilisateur : succès, message d'information ou erreur.
/// </summary>
public readonly record struct CalculatorAction(bool Success, bool IsError, string Message, HistoryEntry? Record)
{
    public static CalculatorAction Ok(HistoryEntry? record = null) => new(true, false, string.Empty, record);

    public static CalculatorAction Info(string message) => new(true, false, message, null);

    public static CalculatorAction Failure(string message) => new(false, true, message, null);
}

/// <summary>
/// Moteur de calcul, totalement indépendant de l'interface (testable sans MAUI).
/// Il gère la saisie, les opérations, la mémoire, les cas limites et le formatage.
/// </summary>
public sealed class CalculatorEngine
{
    public const int MaxDigits = 12;
    public const int MaxHistorySize = 25;

    /// <summary>Au-delà de cette magnitude, on considère que l'application ne peut pas l'afficher.</summary>
    private const double MaxMagnitude = 1e12;

    private string _currentEntry = "0";
    private string _leftText = "0";
    private double _accumulator;
    private CalculatorOperator _pendingOperator = CalculatorOperator.None;
    private double? _lastRightOperand;
    private CalculatorOperator _lastOperator = CalculatorOperator.None;
    private bool _replaceEntry = true;
    private bool _hasError;
    private string _errorMessage = string.Empty;

    /// <summary>Signe en attente : appliqué au prochain nombre saisi (2 × ± 5).</summary>
    private bool _pendingSign;

    /// <summary>Vrai quand l'affichage contient un résultat et non une saisie en cours.</summary>
    private bool _entryIsResult;

    /// <summary>Texte actuellement saisi (affiché dans l'écran principal).</summary>
    public string CurrentEntry => _currentEntry;

    /// <summary>Opération en cours affichée au-dessus du résultat (ex. « 12 × 4 »).</summary>
    public string Expression { get; private set; } = string.Empty;

    /// <summary>Valeur de la case mémoire.</summary>
    public double? Memory { get; private set; }

    public bool HasMemory => Memory.HasValue;

    /// <summary>Vrai si la dernière action a produit une erreur bloquante.</summary>
    public bool HasError => _hasError;

    // ------------------------------------------------------------------ saisie

    public CalculatorAction InputDigit(char digit)
    {
        if (!char.IsDigit(digit))
        {
            return CalculatorAction.Failure($"Le caractère « {digit} » n'est pas un chiffre.");
        }

        if (!PrepareForInput())
        {
            return CalculatorAction.Failure(_errorMessage);
        }

        _entryIsResult = false;

        if (_replaceEntry || _currentEntry == "0")
        {
            _currentEntry = (_pendingSign ? "-" : string.Empty) + digit.ToString(CultureInfo.InvariantCulture);
            _pendingSign = false;
            _replaceEntry = false;
        }
        else
        {
            if (CountSignificantDigits(_currentEntry) >= MaxDigits)
            {
                return CalculatorAction.Info($"Saisie limitée à {MaxDigits} chiffres significatifs.");
            }

            _currentEntry += digit;
        }

        UpdateExpression();
        return CalculatorAction.Ok();
    }

    public CalculatorAction InputDecimal()
    {
        if (!PrepareForInput())
        {
            return CalculatorAction.Failure(_errorMessage);
        }

        _entryIsResult = false;

        if (_replaceEntry)
        {
            _currentEntry = (_pendingSign ? "-" : string.Empty) + "0,";
            _pendingSign = false;
            _replaceEntry = false;
        }
        else if (!_currentEntry.Contains(','))
        {
            _currentEntry += ",";
        }

        UpdateExpression();
        return CalculatorAction.Ok();
    }

    public CalculatorAction ToggleSign()
    {
        if (!PrepareForInput())
        {
            return CalculatorAction.Failure(_errorMessage);
        }

        // Cas 1 : l'écran affiche un résultat -> on le nie directement.
        if (_entryIsResult)
        {
            string negated = _currentEntry.StartsWith('-') ? _currentEntry[1..] : "-" + _currentEntry;
            _currentEntry = negated == "0" ? "0" : negated;
            return CalculatorAction.Ok();
        }

        // Cas 2 : un opérateur vient d'être saisi -> le signe concerne la future saisie.
        if (_replaceEntry)
        {
            _pendingSign = !_pendingSign;
            UpdateExpression();
            return CalculatorAction.Ok();
        }

        // Cas 3 : la saisie est en cours -> on insère ou retire le signe.
        // Sur « 0 », le signe est mis en attente pour ne pas afficher « -0 ».
        if (_currentEntry == "0")
        {
            _pendingSign = !_pendingSign;
        }
        else
        {
            _currentEntry = _currentEntry.StartsWith('-') ? _currentEntry[1..] : "-" + _currentEntry;
        }

        UpdateExpression();
        return CalculatorAction.Ok();
    }

    public CalculatorAction Backspace()
    {
        // Après « = » ou après un opérateur, la saisie courante est déjà validée :
        // il n'y a rien à effacer.
        if (_replaceEntry)
        {
            return CalculatorAction.Ok();
        }

        _entryIsResult = false;
        _pendingSign = false;

        if (_currentEntry.Length <= 1 || (_currentEntry.Length == 2 && _currentEntry.StartsWith("-")))
        {
            _currentEntry = "0";
        }
        else
        {
            _currentEntry = _currentEntry[..^1];
            if (_currentEntry.EndsWith(','))
            {
                _currentEntry = _currentEntry[..^1];
            }
        }

        UpdateExpression();
        return CalculatorAction.Ok();
    }

    /// <summary>Remise à zéro totale (touche AC).</summary>
    public CalculatorAction ClearAll()
    {
        _currentEntry = "0";
        _leftText = "0";
        _accumulator = 0;
        _pendingOperator = CalculatorOperator.None;
        _lastRightOperand = null;
        _lastOperator = CalculatorOperator.None;
        _replaceEntry = true;
        _entryIsResult = false;
        _pendingSign = false;
        _hasError = false;
        _errorMessage = string.Empty;
        Expression = string.Empty;
        return CalculatorAction.Ok();
    }

    /// <summary>Efface seulement la saisie courante (touche CE).</summary>
    public CalculatorAction ClearEntry()
    {
        _currentEntry = "0";
        _replaceEntry = true;
        _entryIsResult = false;
        _pendingSign = false;
        _hasError = false;
        _errorMessage = string.Empty;
        UpdateExpression();
        return CalculatorAction.Ok();
    }

    // ------------------------------------------------------------- opérations

    public CalculatorAction SetOperator(CalculatorOperator op)
    {
        if (op == CalculatorOperator.None)
        {
            return CalculatorAction.Failure("Opération inconnue.");
        }

        if (_hasError)
        {
            ClearAll();
        }

        // Un opérateur saisi deux fois de suite remplace le premier (12 + ×  → 12 ×).
        if (_pendingOperator != CalculatorOperator.None && !_replaceEntry)
        {
            if (!TryApply(_pendingOperator, _accumulator, ValueOf(_currentEntry), out double preview, out string error))
            {
                return SetError(error);
            }

            _accumulator = preview;
            _currentEntry = Format(preview);
            _leftText = _currentEntry;
        }
        else
        {
            _accumulator = ValueOf(_currentEntry);
            _leftText = Format(_accumulator);
        }

        _pendingOperator = op;
        _replaceEntry = true;
        _entryIsResult = false;
        _pendingSign = false;
        _lastRightOperand = null;
        _lastOperator = CalculatorOperator.None;
        UpdateExpression();
        return CalculatorAction.Ok();
    }

    public CalculatorAction ApplyEquals()
    {
        if (_hasError)
        {
            ClearAll();
            return CalculatorAction.Ok();
        }

        CalculatorOperator op;
        double left;
        double right;
        string leftText;

        if (_pendingOperator == CalculatorOperator.None)
        {
            // Répétition du dernier calcul : 2 + 3 = puis = donne 8.
            if (_lastRightOperand is null || _lastOperator == CalculatorOperator.None)
            {
                // « = » sur un simple nombre : la valeur est confirmée, la saisie
                // suivante repart de zéro.
                _replaceEntry = true;
                _entryIsResult = true;
                return CalculatorAction.Ok();
            }

            op = _lastOperator;
            left = ValueOf(_currentEntry);
            leftText = Format(left);
            right = _lastRightOperand.Value;
        }
        else
        {
            op = _pendingOperator;
            left = _accumulator;
            leftText = _leftText;
            right = _replaceEntry ? _accumulator : ValueOf(_currentEntry);
        }

        if (!TryApply(op, left, right, out double result, out string error))
        {
            return SetError(error);
        }

        Expression = $"{leftText} {Symbol(op)} {Format(right)} =";
        _currentEntry = Format(result);
        _replaceEntry = true;
        _entryIsResult = true;
        _pendingSign = false;
        _pendingOperator = CalculatorOperator.None;
        _lastOperator = op;
        _lastRightOperand = right;

        var record = new HistoryEntry(Expression, _currentEntry, DateTime.Now);
        return CalculatorAction.Ok(record);
    }

    public CalculatorAction ApplyUnary(UnaryOperator op)
    {
        if (_hasError)
        {
            ClearAll();
            return CalculatorAction.Ok();
        }

        double value = ValueOf(_currentEntry);
        string before = Format(value);
        double result;

        switch (op)
        {
            case UnaryOperator.Negate:
                result = -value;
                break;

            case UnaryOperator.SquareRoot:
                if (value < 0)
                {
                    return SetError("Racine carrée impossible pour une valeur négative.");
                }

                result = Math.Sqrt(value);
                break;

            case UnaryOperator.Square:
                result = value * value;
                break;

            case UnaryOperator.Reciprocal:
                if (value == 0)
                {
                    return SetError("Division par zéro impossible.");
                }

                result = 1 / value;
                break;

            case UnaryOperator.Percent:
                // 200 + 10 %  →  200 + (200 × 10 / 100)
                result = _pendingOperator is CalculatorOperator.Add or CalculatorOperator.Subtract
                    ? _accumulator * value / 100d
                    : value / 100d;
                break;

            default:
                return CalculatorAction.Failure("Opération inconnue.");
        }

        if (!IsDisplayable(result))
        {
            return SetError("Résultat hors limites (dépassement de capacité).");
        }

        Expression = $"{Symbol(op)}({before}) =";
        _currentEntry = Format(result);

        // Le pourcentage est une valeur intermédiaire : « 200 + 10 % = » doit
        // donner 220, donc l opération en cours doit pouvoir l'utiliser.
        bool feedsPendingOperation = op == UnaryOperator.Percent && _pendingOperator != CalculatorOperator.None;
        _replaceEntry = !feedsPendingOperation;
        _entryIsResult = _replaceEntry;
        _pendingSign = false;
        _hasError = false;
        _errorMessage = string.Empty;

        var record = new HistoryEntry(Expression, _currentEntry, DateTime.Now);
        return CalculatorAction.Ok(record);
    }

    // ---------------------------------------------------------------- mémoire

    public CalculatorAction MemoryAdd() => Store(ValueOf(_currentEntry), 1);

    public CalculatorAction MemorySubtract() =>
        // Une mémoire vide se comporte comme M+ : on évite une mémoire négative inattendue.
        Store(ValueOf(_currentEntry), Memory is null ? 1 : -1);

    public CalculatorAction MemoryRecall()
    {
        if (Memory is null)
        {
            return CalculatorAction.Info("La mémoire est vide.");
        }

        if (_hasError)
        {
            ClearAll();
        }

        _currentEntry = Format(Memory.Value);
        _replaceEntry = false;
        _entryIsResult = false;
        _pendingSign = false;
        UpdateExpression();
        return CalculatorAction.Ok();
    }

    public CalculatorAction MemoryClear()
    {
        bool wasEmpty = Memory is null;
        Memory = null;
        return wasEmpty ? CalculatorAction.Info("La mémoire est déjà vide.") : CalculatorAction.Ok();
    }

    private CalculatorAction Store(double value, double sign)
    {
        if (!IsDisplayable(value))
        {
            return CalculatorAction.Failure("Valeur non enregistrable en mémoire.");
        }

        double updated = (Memory ?? 0d) + (sign * value);
        if (!IsDisplayable(updated))
        {
            return CalculatorAction.Failure("Mémoire saturée : valeur hors limites.");
        }

        Memory = updated;

        // L'écran montre la valeur mémorisée et la saisie suivante repart de zéro.
        _currentEntry = Format(Memory.Value);
        _replaceEntry = true;
        _entryIsResult = false;
        _pendingSign = false;
        _hasError = false;
        _errorMessage = string.Empty;
        UpdateExpression();
        return CalculatorAction.Ok();
    }

    /// <summary>Réutilise une valeur issue de l'historique comme nouvelle saisie.</summary>
    public CalculatorAction LoadValue(double value)
    {
        if (!IsDisplayable(value))
        {
            return CalculatorAction.Failure("Valeur hors limites.");
        }

        if (_hasError)
        {
            ClearAll();
        }

        _currentEntry = Format(value);
        _replaceEntry = false;
        _entryIsResult = false;
        _pendingSign = false;
        UpdateExpression();
        return CalculatorAction.Ok();
    }

    // --------------------------------------------------------------- interne

    /// <summary>Remet l'état dans un état exploitable après une erreur bloquante.</summary>
    private bool PrepareForInput()
    {
        if (!_hasError)
        {
            return true;
        }

        _currentEntry = "0";
        _leftText = "0";
        _accumulator = 0;
        _pendingOperator = CalculatorOperator.None;
        _lastRightOperand = null;
        _lastOperator = CalculatorOperator.None;
        _replaceEntry = true;
        _entryIsResult = false;
        _pendingSign = false;
        _hasError = false;
        Expression = string.Empty;
        return true;
    }

    private CalculatorAction SetError(string message)
    {
        _hasError = true;
        _errorMessage = message;
        Expression = string.Empty;
        _pendingOperator = CalculatorOperator.None;
        _lastRightOperand = null;
        _lastOperator = CalculatorOperator.None;
        _currentEntry = "Erreur";
        _replaceEntry = true;
        _entryIsResult = false;
        _pendingSign = false;
        return CalculatorAction.Failure(message);
    }

    private void UpdateExpression()
    {
        if (_pendingOperator == CalculatorOperator.None)
        {
            Expression = string.Empty;
            return;
        }

        Expression = _replaceEntry
            ? $"{_leftText} {Symbol(_pendingOperator)} {(_pendingSign ? "− " : string.Empty)}".TrimEnd()
            : $"{_leftText} {Symbol(_pendingOperator)} {(_pendingSign ? "-" : string.Empty)}{_currentEntry}";
    }

    private static bool TryApply(CalculatorOperator op, double left, double right, out double result, out string error)
    {
        error = string.Empty;

        switch (op)
        {
            case CalculatorOperator.Add:
                result = left + right;
                break;

            case CalculatorOperator.Subtract:
                result = left - right;
                break;

            case CalculatorOperator.Multiply:
                result = left * right;
                break;

            case CalculatorOperator.Divide:
                if (right == 0)
                {
                    result = 0;
                    error = "Division par zéro impossible.";
                    return false;
                }

                result = left / right;
                break;

            case CalculatorOperator.Power:
                if (left == 0 && right < 0)
                {
                    result = 0;
                    error = "Puissance impossible : 0 élevé à une puissance négative.";
                    return false;
                }

                result = Math.Pow(left, right);
                break;

            default:
                result = 0;
                error = "Opération inconnue.";
                return false;
        }

        if (double.IsNaN(result))
        {
            result = 0;
            error = "Résultat indéfini pour ces valeurs.";
            return false;
        }

        if (!IsDisplayable(result))
        {
            result = 0;
            error = "Résultat hors limites (dépassement de capacité).";
            return false;
        }

        return true;
    }

    private static bool IsDisplayable(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value) && Math.Abs(value) <= MaxMagnitude;

    private static int CountSignificantDigits(string entry) =>
        entry.Replace("-", string.Empty).Replace(",", string.Empty).Length;

    private static double ValueOf(string entry)
    {
        string normalized = entry.Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            ? value
            : 0d;
    }

    /// <summary>Symbole lisible d'une opération.</summary>
    public static string Symbol(CalculatorOperator op) => op switch
    {
        CalculatorOperator.Add => "+",
        CalculatorOperator.Subtract => "−",
        CalculatorOperator.Multiply => "×",
        CalculatorOperator.Divide => "÷",
        CalculatorOperator.Power => "^",
        _ => string.Empty
    };

    public static string Symbol(UnaryOperator op) => op switch
    {
        UnaryOperator.Negate => "±",
        UnaryOperator.SquareRoot => "√",
        UnaryOperator.Square => "x²",
        UnaryOperator.Reciprocal => "1/x",
        UnaryOperator.Percent => "%",
        _ => string.Empty
    };

    /// <summary>
    /// Convertit un nombre en texte affichable : virgule décimale, pas de zéros
    /// superflus, notation scientifique pour les valeurs extrêmes.
    /// </summary>
    public static string Format(double value)
    {
        if (double.IsNaN(value))
        {
            return "Erreur";
        }

        if (double.IsInfinity(value))
        {
            return "∞";
        }

        if (value == 0)
        {
            return "0";
        }

        double absolute = Math.Abs(value);

        if (absolute >= 1e11 || absolute < 1e-9)
        {
            return value.ToString("0.####E+0", CultureInfo.InvariantCulture).Replace('.', ',');
        }

        int decimals = Math.Clamp(10 - (int)Math.Floor(Math.Log10(absolute)), 0, 15);
        double rounded = Math.Round(value, decimals);
        if (rounded == 0)
        {
            return "0";
        }

        string text = rounded.ToString("0.##########", CultureInfo.InvariantCulture).Replace('.', ',');
        return text == "-0" ? "0" : text;
    }
}
