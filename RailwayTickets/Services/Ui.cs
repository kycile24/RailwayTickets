using System.Globalization;
namespace RailwayTickets.Services;
public static class Ui
{
    public static bool English => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";
    public static string Pick(string uk, string en) => English ? en : uk;
    public static string StationKey(string value) => value.Trim().ToLowerInvariant() switch
    {
        "kyiv" => "Київ", "lviv" => "Львів", "odesa" => "Одеса", "kharkiv" => "Харків", _ => value
    };
    public static string Station(string value) => English ? value switch
    {
        "Київ" => "Kyiv", "Львів" => "Lviv", "Одеса" => "Odesa", "Харків" => "Kharkiv", _ => value
    } : value;
    public static string Status(string? value) => !English ? value ?? "" : value switch
    {
        "Заброньовано" => "Reserved",
        "Скасовано" => "Cancelled",
        "Оформлено" => "Issued",
        "Оплачено" => "Paid",
        "Повернено" => "Refunded",
        "Очікує" => "Pending",
        "Схвалено" => "Approved",
        "Відхилено" => "Rejected",
        _ => value ?? ""
    };
}
