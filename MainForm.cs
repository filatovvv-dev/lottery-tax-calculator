using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace LotteryTax;

public class MainForm : Form
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");
    private const char Nbsp = '\u00A0';       // неразрывный пробел
    private const string Rub = "\u20BD";      // ₽

    private readonly TextBox _input      = new();
    private readonly Label   _valRate    = new();
    private readonly Label   _valTax     = new();
    private readonly Label   _valNet     = new();
    private readonly Label   _valBracket = new();
    private bool _formatting;

    private readonly ToolTip _phraseTip = new();
    private TaxResult? _lastResult;

    public MainForm()
    {
        Text            = "Лотерея: сумма «на руки»";
        Font            = new Font("Segoe UI", 10F);
        StartPosition   = FormStartPosition.CenterScreen;
        ClientSize      = new Size(560, 380);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;

        // --- Ввод суммы ---
        var lblSum = new Label { Text = "Сумма выигрыша:", Location = new Point(20, 25), AutoSize = true };

        _input.Location   = new Point(170, 22);
        _input.Width      = 360;
        _input.TextAlign  = HorizontalAlignment.Left;   // выключка влево
        _input.TextChanged += Input_TextChanged;
        _input.KeyPress   += (_, e) =>
        {
            if (e.KeyChar == (char)Keys.Enter) { e.Handled = true; Calculate(); }
        };

        var btnCalc = new Button { Text = "Рассчитать", Location = new Point(170, 60), Size = new Size(150, 32) };
        btnCalc.Click += (_, _) => Calculate();

        var btnClear = new Button { Text = "Очистить", Location = new Point(330, 60), Size = new Size(120, 32) };
        btnClear.Click += (_, _) => { _input.Clear(); ClearResult(); _input.Focus(); };

        // --- Результаты ---
        var gb = new GroupBox { Text = "Результат расчёта", Location = new Point(20, 105), Size = new Size(510, 210) };

        int y = 25;
        AddRow(gb, "Ставка НДФЛ:",     _valRate, ref y);
        AddRow(gb, "Сумма НДФЛ:",      _valTax,  ref y);
        AddRow(gb, "Сумма «на руки»:", _valNet,  ref y);

        _valNet.Font      = new Font("Segoe UI", 12F, FontStyle.Bold);
        _valNet.ForeColor = Color.DarkGreen;
        _valNet.MouseEnter += ValNet_MouseEnter;
        _valNet.MouseLeave += (_, _) => _phraseTip.Hide(_valNet);

        var lblBracket = new Label { Text = "Порядок расчёта:", Location = new Point(15, 120), AutoSize = true };
        _valBracket.Location  = new Point(15, 145);
        _valBracket.Size      = new Size(480, 50);
        _valBracket.ForeColor = Color.DimGray;
        gb.Controls.Add(lblBracket);
        gb.Controls.Add(_valBracket);

        // --- Разделитель и подпись внизу ---
        var separator = new Panel
        {
            Location  = new Point(0, 335),
            Size      = new Size(560, 1),
            BackColor = Color.Gainsboro
        };
        var footer = new Label
        {
            Text      = "Расчет налога с выигрыша. Copyright (c) CoScore, 2026",
            TextAlign = ContentAlignment.MiddleCenter,
            Location  = new Point(0, 342),
            Size      = new Size(560, 30),
            ForeColor = Color.DimGray
        };

        Controls.AddRange(new Control[] { lblSum, _input, btnCalc, btnClear, gb, separator, footer });

        AcceptButton  = btnCalc;
        ActiveControl = _input;

        _phraseTip.InitialDelay = 300;
        _phraseTip.AutoPopDelay = 10_000;
        _phraseTip.ReshowDelay  = 100;
        _phraseTip.ShowAlways   = true;

        // Демонстрационное значение сразу в отформатированном виде
        _formatting  = true;
        _input.Text  = "1" + Nbsp + "000" + Nbsp + "000" + Nbsp + Rub;
        _formatting  = false;
    }

    // ── Форматирование ввода: 1234567 → «1 234 567 ₽» с NBSP ───────────────
    private void Input_TextChanged(object? sender, EventArgs e)
    {
        if (_formatting) return;

        string raw    = _input.Text;
        int    caret  = _input.SelectionStart;

        // Сколько цифр стоит до курсора — чтобы потом поставить его туда же
        int digitsBefore = 0;
        for (int i = 0; i < caret && i < raw.Length; i++)
            if (char.IsAsciiDigit(raw[i])) digitsBefore++;

        // Оставляем только цифры (это отсекает любые пробелы, ₽, буквы)
        string digits = new string(raw.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length > 15) digits = digits[..15];

        string formatted;
        if (digits.Length == 0)
        {
            formatted = string.Empty;
        }
        else
        {
            if (!long.TryParse(digits, out long number)) number = 0;
            formatted = number.ToString("N0", Ru)
                              .Replace(' ', Nbsp)          // обычный пробел → NBSP
                              .Replace('\u202F', Nbsp)     // узкий неразрывный → NBSP
                        + Nbsp + Rub;
        }

        if (formatted == raw) return;   // ничего переписывать не нужно

        _formatting = true;
        try
        {
            _input.Text = formatted;
            _input.SelectionStart = PositionAfterDigits(formatted, digitsBefore);
        }
        finally
        {
            _formatting = false;
        }
    }

    private static int PositionAfterDigits(string s, int n)
    {
        if (n <= 0) return 0;
        int seen = 0;
        for (int i = 0; i < s.Length; i++)
            if (char.IsAsciiDigit(s[i]) && ++seen == n) return i + 1;
        return s.Length;
    }

    private static void AddRow(GroupBox gb, string caption, Label value, ref int y)
    {
        var l = new Label { Text = caption, Location = new Point(15, y), AutoSize = true };
        value.Location = new Point(200, y);
        value.AutoSize = true;
        gb.Controls.Add(l);
        gb.Controls.Add(value);
        y += 28;
    }

    private void Calculate()
    {
        if (!TryParseMoney(_input.Text, out decimal amount) || amount < 0)
        {
            MessageBox.Show(this,
                "Введите сумму выигрыша — целое число рублей.",
                "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _input.Focus();
            _input.SelectAll();
            return;
        }

        var r = TaxCalculator.Calculate(amount);
        _lastResult = r;

        _valRate.Text    = r.Rate + " %";
        _valTax.Text     = Money(r.Tax) + Nbsp + Rub;
        _valNet.Text     = Money(r.Net) + Nbsp + Rub;
        _valBracket.Text = r.Explanation;
    }

    private void ClearResult()
    {
        _valRate.Text = _valTax.Text = _valNet.Text = _valBracket.Text = string.Empty;
        _lastResult = null;
        _phraseTip.Hide(_valNet);
    }

    private void ValNet_MouseEnter(object? sender, EventArgs e)
    {
        if (_lastResult is not { } r) return;
        _phraseTip.Show(TooltipPhrases.For(r.Net), _valNet,
                        _valNet.Width / 2, _valNet.Height, 10_000);
    }

    private static string Money(decimal v) =>
        v.ToString("N0", Ru).Replace(' ', Nbsp).Replace('\u202F', Nbsp);

    private static bool TryParseMoney(string? text, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var s = text
            .Replace(Nbsp.ToString(), "")
            .Replace(" ", "")
            .Replace(Rub, "")
            .Replace("руб.", "", StringComparison.OrdinalIgnoreCase)
            .Replace("руб",  "", StringComparison.OrdinalIgnoreCase);

        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}