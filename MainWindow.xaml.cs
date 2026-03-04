using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Kalkulajda
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Internal state
        private string _currentInput = "0";            // uses '.' as decimal separator internally
        private double? _accumulator = null;           // stored left operand
        private string _pendingOperator = null;       // "+", "-", "x", "÷"
        private bool _isNewEntry = true;               // whether next digit starts a new input

        public MainWindow()
        {
            InitializeComponent();
            UpdateDisplays();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;
            var cmd = (btn.Content ?? string.Empty).ToString();

            switch (cmd)
            {
                // Digits
                case "0":
                case "1":
                case "2":
                case "3":
                case "4":
                case "5":
                case "6":
                case "7":
                case "8":
                case "9":
                    AppendDigit(cmd);
                    break;

                case "00":
                    AppendDoubleZero();
                    break;

                // Decimal separator (display uses ',' but we store '.')
                case ",":
                case ".":
                    AppendDecimal();
                    break;

                // Basic operators
                case "+":
                case "-":
                case "x":
                case "÷":
                    ApplyOperator(cmd);
                    break;

                case "=":
                    CalculateResult();
                    break;

                // Clear / delete
                case "C":
                    ClearAll();
                    break;

                case "CE":
                    ClearEntry();
                    break;

                case "Del.":
                    Backspace();
                    break;

                // Functions
                case "+/-":
                    ToggleSign();
                    break;

                case "%":
                    Percent();
                    break;

                case "1/x":
                    Reciprocal();
                    break;

                case "sqr()":
                    Square();
                    break;

                case "√":
                    SquareRoot();
                    break;

                default:
                    // Unknown command - ignore
                    break;
            }

            UpdateDisplays();
        }

        private void AppendDigit(string digit)
        {
            if (_isNewEntry || _currentInput == "0")
            {
                _currentInput = digit;
                _isNewEntry = false;
            }
            else
            {
                _currentInput += digit;
            }
        }

        private void AppendDoubleZero()
        {
            if (_isNewEntry || _currentInput == "0")
            {
                _currentInput = "0";
                _isNewEntry = false;
            }
            else
            {
                _currentInput += "00";
            }
        }

        private void AppendDecimal()
        {
            if (_isNewEntry)
            {
                _currentInput = "0.";
                _isNewEntry = false;
                return;
            }

            if (!_currentInput.Contains("."))
            {
                _currentInput += ".";
            }
        }

        private double ParseCurrent()
        {
            // parse using invariant culture where '.' is decimal separator
            double val;
            if (double.TryParse(_currentInput, NumberStyles.Number, CultureInfo.InvariantCulture, out val))
                return val;

            return 0.0;
        }

        private void ApplyOperator(string op)
        {
            var value = ParseCurrent();

            if (_accumulator == null)
            {
                _accumulator = value;
            }
            else if (!_isNewEntry || _pendingOperator != null)
            {
                // chain calculation
                _accumulator = Compute(_accumulator.Value, value, _pendingOperator);
            }

            _pendingOperator = op;
            _isNewEntry = true;
        }

        private void CalculateResult()
        {
            if (_pendingOperator == null)
            {
                // nothing to calculate - keep current input
                return;
            }

            var right = ParseCurrent();
            if (_accumulator == null) _accumulator = 0.0;

            var result = Compute(_accumulator.Value, right, _pendingOperator);
            _currentInput = FormatDoubleForInternal(result);
            _accumulator = null;
            _pendingOperator = null;
            _isNewEntry = true;
        }

        private double Compute(double left, double right, string op)
        {
            try
            {
                switch (op)
                {
                    case "+":
                        return left + right;
                    case "-":
                        return left - right;
                    case "x":
                        return left * right;
                    case "÷":
                        return right == 0 ? double.NaN : left / right;
                    default:
                        return right;
                }
            }
            catch
            {
                return double.NaN;
            }
        }

        private void ClearAll()
        {
            _currentInput = "0";
            _accumulator = null;
            _pendingOperator = null;
            _isNewEntry = true;
        }

        private void ClearEntry()
        {
            _currentInput = "0";
            _isNewEntry = true;
        }

        private void Backspace()
        {
            if (_isNewEntry)
            {
                _currentInput = "0";
                _isNewEntry = true;
                return;
            }

            if (_currentInput.Length <= 1)
            {
                _currentInput = "0";
                _isNewEntry = true;
            }
            else
            {
                _currentInput = _currentInput.Substring(0, _currentInput.Length - 1);
            }
        }

        private void ToggleSign()
        {
            var val = ParseCurrent();
            val = -val;
            _currentInput = FormatDoubleForInternal(val);
        }

        private void Percent()
        {
            var val = ParseCurrent();

            if (_accumulator != null && _pendingOperator != null)
            {
                // like Windows Calculator: percent is relative to accumulator
                val = _accumulator.Value * val / 100.0;
            }
            else
            {
                val = val / 100.0;
            }

            _currentInput = FormatDoubleForInternal(val);
            _isNewEntry = true;
        }

        private void Reciprocal()
        {
            var val = ParseCurrent();
            if (val == 0)
            {
                _currentInput = "Cannot divide by zero";
                _isNewEntry = true;
                return;
            }

            val = 1.0 / val;
            _currentInput = FormatDoubleForInternal(val);
            _isNewEntry = true;
        }

        private void Square()
        {
            var val = ParseCurrent();
            val = val * val;
            _currentInput = FormatDoubleForInternal(val);
            _isNewEntry = true;
        }

        private void SquareRoot()
        {
            var val = ParseCurrent();
            if (val < 0)
            {
                _currentInput = "NaN";
            }
            else
            {
                val = Math.Sqrt(val);
                _currentInput = FormatDoubleForInternal(val);
            }

            _isNewEntry = true;
        }

        private static string FormatDoubleForInternal(double value)
        {
            // Use invariant so internal representation uses '.' as decimal separator
            return value.ToString("G15", CultureInfo.InvariantCulture);
        }

        private void UpdateDisplays()
        {
            // Expression (small display) shows accumulator + operator when present
            if (_accumulator != null && _pendingOperator != null)
            {
                TxtExpression.Text = FormatForUser(_accumulator.Value) + " " + _pendingOperator;
            }
            else
            {
                TxtExpression.Text = string.Empty;
            }

            // Result display: show current input, but formatted for user's decimal separator
            TxtResult.Text = FormatForUserString(_currentInput);
        }

        private static string FormatForUser(double value)
        {
            // Format using current culture so decimal separator matches user's settings
            return value.ToString("G15", CultureInfo.CurrentCulture);
        }

        private static string FormatForUserString(string internalNumber)
        {
            // If it's a text like error messages, return as-is
            if (internalNumber == "Cannot divide by zero" || internalNumber == "NaN")
                return internalNumber;

            // Replace invariant '.' with current culture separator for display
            var decimalSep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            if (decimalSep != ".")
                return internalNumber.Replace(".", decimalSep);

            return internalNumber;
        }
    }
}