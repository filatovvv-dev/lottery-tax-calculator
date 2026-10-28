using System;

namespace LotteryTax;

public readonly record struct TaxResult(
    decimal Amount, decimal Tax, decimal Net, int Rate, string Explanation);

public static class TaxCalculator
{
    // Пороговые суммы и прогрессивная шкала из таблицы
    public static TaxResult Calculate(decimal amount)
    {
        decimal tax;
        int rate;
        string explanation;

        if (amount <= 0m)
        {
            return new TaxResult(0m, 0m, 0m, 0,
                "Выигрыша нет — налог не начисляется.");
        }

        if (amount <= 2_400_000m)
        {
            rate = 13;
            tax = amount * 0.13m;
            explanation = "13 % со всей суммы выигрыша.";
        }
        else if (amount <= 5_000_000m)
        {
            rate = 15;
            tax = 312_000m + (amount - 2_400_000m) * 0.15m;
            explanation = "312 000 ₽ + 15 % с суммы, превышающей 2 400 000 ₽.";
        }
        else if (amount <= 20_000_000m)
        {
            rate = 18;
            tax = 702_000m + (amount - 5_000_000m) * 0.18m;
            explanation = "702 000 ₽ + 18 % с суммы, превышающей 5 000 000 ₽.";
        }
        else if (amount <= 50_000_000m)
        {
            rate = 20;
            tax = 3_402_000m + (amount - 20_000_000m) * 0.20m;
            explanation = "3 402 000 ₽ + 20 % с суммы, превышающей 20 000 000 ₽.";
        }
        else
        {
            rate = 22;
            tax = 9_402_000m + (amount - 50_000_000m) * 0.22m;
            explanation = "9 402 000 ₽ + 22 % с суммы, превышающей 50 000 000 ₽.";
        }

        // НДФЛ по ст. 52 НК РФ округляется до полного рубля
        tax = Math.Round(tax, 0, MidpointRounding.AwayFromZero);

        return new TaxResult(amount, tax, amount - tax, rate, explanation);
    }
}