using System.Text.Json.Serialization;

namespace BudgetFriend.API.Database.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionType
{
    Income = 1,
    Expense = 2
}
