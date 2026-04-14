using MassTransit;
using System.Reflection;

namespace Core.Infrastructure.Extensions;

public static class NameFormatterExtensions
{
    public static string ToKebabCaseString(this MemberInfo member)
        => KebabCaseEndpointNameFormatter.Instance.SanitizeName(member.Name);

    public static string ToKebabCaseString(this string @string)
        => KebabCaseEndpointNameFormatter.Instance.SanitizeName(@string);
}

public class KebabCaseEntityNameFormatter : IEntityNameFormatter
{
    public string FormatEntityName<T>()
        => typeof(T).ToKebabCaseString();
}