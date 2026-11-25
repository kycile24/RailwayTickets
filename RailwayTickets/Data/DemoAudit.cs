using Microsoft.EntityFrameworkCore;

namespace RailwayTickets.Data;

internal static class DemoAudit
{
    public static void Initialize(RailwayDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS OperationLog (
              Id INTEGER PRIMARY KEY, OccurredUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
              Actor TEXT NOT NULL, EntityName TEXT NOT NULL, EntityId INTEGER NOT NULL, Operation TEXT NOT NULL)
            """);
        foreach (var table in new[] { "Employees","Passengers","Routes","Trains","Carriages","Trips","Tickets","ReturnRequests","CashShifts","CashEntries" })
        foreach (var (action, record, label) in new[] { ("INSERT","NEW","CREATE"), ("UPDATE","NEW","UPDATE"), ("DELETE","OLD","DELETE") })
        {
            var sql = $"CREATE TRIGGER IF NOT EXISTS audit_{table}_{action} AFTER {action} ON {table} BEGIN INSERT INTO OperationLog (Actor,EntityName,EntityId,Operation) VALUES (railway_actor(),'{table}',{record}.Id,'{label}'); END;";
            db.Database.ExecuteSqlRaw(sql);
        }
    }
}
