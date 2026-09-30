using CalculatriceMaui.Models;
using CalculatriceMaui.ViewModels;

namespace EngineTests;

internal static class Program
{
    private static int _passed;
    private static readonly List<string> Failures = new();

    private static void Check(string label, object? actual, object? expected)
    {
        string a = actual?.ToString() ?? "null";
        string e = expected?.ToString() ?? "null";
        if (a == e)
        {
            _passed++;
        }
        else
        {
            Failures.Add($"ÉCHEC {label} : obtenu « {a} » au lieu de « {e} »");
        }
    }

    private static void CheckTrue(string label, bool condition) =>
        Check(label, condition, true);

    /// <summary>Simule une suite de touches sur une calculatrice neuve.</summary>
    private static CalculatorViewModel Type(params string[] keys)
    {
        CalculatorViewModel vm = new();
        foreach (string key in keys)
        {
            switch (key)
            {
                case ",":
                    vm.InputDecimal();
                    break;
                case "=":
                    vm.ApplyEquals();
                    break;
                case "AC":
                    vm.ClearAll();
                    break;
                case "CE":
                    vm.ClearEntry();
                    break;
                case "⌫":
                    vm.Backspace();
                    break;
                case "±":
                    vm.ToggleSign();
                    break;
                case "%":
                    vm.Percent();
                    break;
                case "√":
                    vm.ApplyUnary("√");
                    break;
                case "x²":
                    vm.ApplyUnary("x²");
                    break;
                case "1/x":
                    vm.ApplyUnary("1/x");
                    break;
                case "M+":
                    vm.MemoryAdd();
                    break;
                case "M-":
                    vm.MemorySubtract();
                    break;
                case "MR":
                    vm.MemoryRecall();
                    break;
                case "MC":
                    vm.MemoryClear();
                    break;
                case "+":
                case "−":
                case "×":
                case "÷":
                case "^":
                    vm.SetOperator(key);
                    break;
                default:
                    vm.InputDigit(key);
                    break;
            }
        }

        return vm;
    }

    private static int Main()
    {
        // ---- opérations de base
        Check("12 + 4", Type("1", "2", "+", "4", "=").Display, "16");
        Check("12 − 4", Type("1", "2", "−", "4", "=").Display, "8");
        Check("12 × 4", Type("1", "2", "×", "4", "=").Display, "48");
        Check("12 ÷ 4", Type("1", "2", "÷", "4", "=").Display, "3");
        Check("7 ÷ 2", Type("7", "÷", "2", "=").Display, "3,5");
        Check("opération en cours affichée", Type("1", "2", "×", "4").Expression, "12 × 4");
        Check("expression après equals", Type("1", "2", "×", "4", "=").Expression, "12 × 4 =");

        // ---- décimaux
        Check("1,5 × 2", Type("1", ",", "5", "×", "2", "=").Display, "3");
        Check("0,1 + 0,2", Type("0", ",", "1", "+", "0", ",", "2", "=").Display, "0,3");
        Check("virgule unique", Type("1", ",", ",", "5").Display, "1,5");
        Check("virgule en tête", Type(",", "5").Display, "0,5");
        Check("zéros initiaux", Type("0", "0", "5").Display, "5");
        Check("1/3 arrondi", Type("1", "÷", "3", "=").Display, "0,3333333333");
        Check("virgule finale retirée par ⌫", Type("1", ",", "⌫").Display, "1");

        // ---- correction
        Check("⌫ efface un caractère", Type("1", "2", "3", "⌫").Display, "12");
        Check("⌫ sur nombre négatif", Type("±", "5", "⌫").Display, "0");
        Check("±", Type("4", "±").Display, "-4");
        Check("double ±", Type("4", "±", "±").Display, "4");
        Check("± sur 0", Type("0", "±").Display, "0");
        Check("± puis chiffre", Type("±", "5").Display, "-5");
        Check("± sur un résultat", Type("7", "=", "±").Display, "-7");
        Check("± puis nouveau chiffre", Type("7", "=", "±", "5").Display, "5");
        Check("opérateur puis ±", Type("2", "×", "±", "5", "=").Display, "-10");
        Check("opérateur puis ± affiché", Type("2", "×", "±").Expression, "2 × −");
        Check("CE conserve l'opérateur", Type("1", "2", "+", "5", "6", "CE", "7", "=").Display, "19");
        Check("AC remet tout à zéro", Type("1", "2", "×", "9", "AC").Display, "0");
        Check("AC vide l'expression", Type("1", "2", "×", "AC").Expression, string.Empty);

        // ---- opérateur remplacé
        Check("opérateur répété", Type("8", "+", "×", "2", "=").Display, "16");
        Check("enchaînement sans égal", Type("2", "+", "3", "×", "4", "=").Display, "20");
        Check("calcul en cours à l'opérateur", Type("2", "+", "3", "=").Display, "5");

        // ---- répétition du calcul
        Check("= répète (2+3)", Type("2", "+", "3", "=", "=").Display, "8");
        Check("= répète (2×3)", Type("2", "×", "3", "=", "=").Display, "18");
        Check("= sans opérateur", Type("7", "=").Display, "7");

        // ---- division par zéro
        CalculatorViewModel divZero = Type("8", "÷", "0", "=");
        CheckTrue("division par zéro signalée", divZero.HasError);
        Check("division par zéro : affichage", divZero.Display, "Erreur");
        CheckTrue("division par zéro : message", divZero.StatusIsError && divZero.StatusMessage.Length > 0);
        Check("saisie possible après erreur", Type("8", "÷", "0", "=", "5", "=").Display, "5");
        Check("opérateur possible après erreur", Type("8", "÷", "0", "=", "+", "5", "=").Display, "5");
        Check("1/x sur 0", Type("0", "1/x").HasError, true);
        Check("8 ÷ ( 0 = )", Type("8", "÷", "=").Display, "1");

        // ---- opérations avancées
        Check("racine carrée", Type("9", "√").Display, "3");
        Check("racine carrée négative", Type("4", "±", "√").HasError, true);
        Check("carré", Type("7", "x²").Display, "49");
        Check("inverse", Type("4", "1/x").Display, "0,25");
        Check("puissance", Type("2", "^", "1", "0", "=").Display, "1024");
        Check("puissance négative", Type("2", "^", "±", "3", "=").Display, "0,125");
        Check("puissance 0⁻¹", Type("0", "^", "±", "1", "=").HasError, true);
        Check("puissance fractionnaire négative", Type("4", "±", "^", "0", ",", "5", "=").HasError, true);

        // ---- pourcentage
        Check("10 % seul", Type("5", "0", "%").Display, "0,5");
        Check("200 + 10 %", Type("2", "0", "0", "+", "1", "0", "%", "=").Display, "220");
        Check("200 − 10 %", Type("2", "0", "0", "−", "1", "0", "%", "=").Display, "180");
        Check("200 × 10 %", Type("2", "0", "0", "×", "1", "0", "%", "=").Display, "20");

        // ---- limites
        CalculatorViewModel tooLong = Type("1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1");
        Check("saisie limitée à 12 chiffres", tooLong.Display, "111111111111");
        CheckTrue("message de saisie", tooLong.StatusMessage.Contains("12"));
        Check("dépassement de capacité", Type("9", "9", "9", "9", "9", "9", "9", "9", "9", "×", "9", "9", "9", "9", "9", "9", "9", "9", "9", "=").HasError, true);
        Check("notation scientifique", Type("1", "÷", "9", "9", "9", "9", "9", "9", "9", "9", "9", "9", "=").Display, "1E-10");

        // ---- formatage
        Check("format 0", CalculatorEngine.Format(0), "0");
        Check("format négatif zéro", CalculatorEngine.Format(-0.0), "0");
        Check("format 1/3", CalculatorEngine.Format(1d / 3d), "0,3333333333");
        Check("format 1,25", CalculatorEngine.Format(1.25), "1,25");
        Check("format 100", CalculatorEngine.Format(100d), "100");
        Check("format 1e13", CalculatorEngine.Format(1e13), "1E+13");
        Check("format 1e-12", CalculatorEngine.Format(1e-12), "1E-12");

        // ---- mémoire
        CalculatorViewModel mem = Type("5", "M+", "7", "M+", "MR");
        Check("M+ cumule", mem.Display, "12");
        Check("badge mémoire", mem.MemoryText, "M 12");
        CheckTrue("mémoire active", mem.HasMemory);
        Check("M− retranche", Type("5", "0", "M-", "MR").Display, "50");
        Check("M− sur mémoire vide", Type("7", "M-").MemoryText, "M 7");
        Check("MC vide", Type("5", "M+", "MC").HasMemory, false);
        Check("MR mémoire vide", Type("MR").StatusMessage, "La mémoire est vide.");

        // ---- historique
        CalculatorViewModel history = Type("1", "2", "×", "4", "=", "7", "+", "3", "=");
        Check("historique : nombre d'éléments", history.History.Count, 2);
        Check("historique : le plus récent en tête", history.History[0].Result, "10");
        Check("historique : expression", history.History[0].Expression, "7 + 3 =");
        Check("historique : le plus ancien", history.History[1].Result, "48");
        Check("historique : libellé du bouton", history.HistoryButtonText, "Historique (2)");
        history.UseHistoryValue(history.History[1]);
        Check("réutilisation d'un résultat", history.Display, "48");
        history.ClearHistory();
        Check("vidage de l'historique", history.History.Count, 0);

        // ---- historique borné
        CalculatorViewModel many = new();
        for (int i = 1; i <= 40; i++)
        {
            many.InputDigit("7");
            many.SetOperator("+");
            many.InputDigit("1");
            many.ApplyEquals();
        }

        Check("historique limité", many.History.Count, CalculatorEngine.MaxHistorySize);

        // ---- bascule portrait / paysage (simulée par les propriétés du ViewModel)
        CheckTrue("pas de division par zéro silencieuse", Type("1", "÷", "0", "=").StatusIsError);

        Console.WriteLine($"Tests réussis : {_passed}");
        if (Failures.Count > 0)
        {
            Console.WriteLine($"Échecs : {Failures.Count}");
            foreach (string failure in Failures)
            {
                Console.WriteLine(" - " + failure);
            }

            return 1;
        }

        Console.WriteLine("Tous les tests passent.");
        return 0;
    }
}
