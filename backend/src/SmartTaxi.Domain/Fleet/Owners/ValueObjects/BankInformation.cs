using System.Diagnostics.CodeAnalysis;
using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Owners.ValueObjects;

public sealed class BankInformation : ValueObject
{
    public string BankName { get; }
    public string AccountHolderName { get; }
    public string AccountNumber { get; }
    public string? SwiftOrBic { get; }

    private BankInformation(string bankName, string accountHolderName, string accountNumber, string? swiftOrBic)
    {
        BankName = bankName;
        AccountHolderName = accountHolderName;
        AccountNumber = accountNumber;
        SwiftOrBic = swiftOrBic;
    }

    public static BankInformation Create(string bankName, string accountHolderName, string accountNumber, string? swiftOrBic)
    {
        if (!TryCreate(bankName, accountHolderName, accountNumber, swiftOrBic, out var info, out var error))
        {
            throw new ArgumentException(error);
        }

        return info;
    }

    public static bool TryCreate(
        string? bankName, string? accountHolderName, string? accountNumber, string? swiftOrBic,
        [NotNullWhen(true)] out BankInformation? info, [NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrWhiteSpace(bankName) || string.IsNullOrWhiteSpace(accountHolderName)
            || string.IsNullOrWhiteSpace(accountNumber))
        {
            info = null;
            error = "Les informations bancaires sont incomplètes.";
            return false;
        }

        info = new BankInformation(bankName.Trim(), accountHolderName.Trim(), accountNumber.Trim(), swiftOrBic?.Trim());
        error = null;
        return true;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return BankName;
        yield return AccountHolderName;
        yield return AccountNumber;
        yield return SwiftOrBic;
    }
}
