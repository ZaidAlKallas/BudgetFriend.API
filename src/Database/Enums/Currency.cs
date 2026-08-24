using System.Text.Json.Serialization;

namespace BudgetFriend.API.Database.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Currency
{
    USD,
    EUR,
    GBP,
    TRY,
    SAR,
    SYP,
    AED,
    CHF
}
