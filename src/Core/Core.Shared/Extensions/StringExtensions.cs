using System.Text.Json;
using System.Text.RegularExpressions;

namespace Core.Shared.Extensions;

public static partial class StringExtensions
{
    [GeneratedRegex(@"[^0-9a-zA-Z\._@+]")]
    private static partial Regex RemoveSpecialCharactersRegex();

    [GeneratedRegex(@"[^0-9a-zA-Z_@]")]
    private static partial Regex RemoveNonAlphaNumericCharactersRegex();

    [GeneratedRegex(@"[^0-9]")]
    private static partial Regex OnlyDigitsRegex();

    public static string RemoveSpecialCharacters(this string input)
        => string.IsNullOrWhiteSpace(input)
            ? string.Empty
            : RemoveSpecialCharactersRegex().Replace(input, string.Empty);

    public static string RemoveNonAlphaNumericCharacters(this string input)
        => string.IsNullOrWhiteSpace(input)
            ? string.Empty
            : RemoveNonAlphaNumericCharactersRegex().Replace(input, string.Empty);

    public static string OnlyDigits(this string input)
        => string.IsNullOrWhiteSpace(input)
            ? string.Empty
            : OnlyDigitsRegex().Replace(input, string.Empty);

    public static bool IsValidCnpj(this string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            return false;

        cnpj = cnpj.OnlyDigits();

        if (cnpj.Length != 14)
            return false;

        if (cnpj.Distinct().Count() == 1)
            return false;

        var multiplier1 = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var multiplier2 = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var tempCnpj = cnpj.Substring(0, 12);
        var sum = 0;

        for (int i = 0; i < 12; i++)
            sum += int.Parse(tempCnpj[i].ToString()) * multiplier1[i];

        var rest = sum % 11;
        rest = rest < 2 ? 0 : 11 - rest;

        var digito = rest.ToString();

        tempCnpj += digito;
        sum = 0;

        for (int i = 0; i < 13; i++)
            sum += int.Parse(tempCnpj[i].ToString()) * multiplier2[i];

        rest = sum % 11;
        rest = rest < 2 ? 0 : 11 - rest;
        digito += rest.ToString();

        return cnpj.EndsWith(digito);
    }

    public static bool IsValidPostalCode(this string postalCode)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
            return false;

        postalCode = postalCode.OnlyDigits();

        if (postalCode.Length != 8)
            return false;

        if (postalCode.Distinct().Count() == 1)
            return false;

        return true;

    }

    public static bool IsValidPhoneNumber(this string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        phoneNumber = new string(phoneNumber.Where(char.IsDigit).ToArray());

        if (phoneNumber.Length < 9 || phoneNumber.Length > 11)
            return false;

        if (phoneNumber.Distinct().Count() == 1)
            return false;

        return true;
    }

    public static bool BeValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var document = JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}