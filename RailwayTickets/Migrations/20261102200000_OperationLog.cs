using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RailwayTickets.Data;

namespace RailwayTickets.Migrations;

[DbContext(typeof(RailwayDbContext))]
[Migration("20261102200000_OperationLog")]
public class OperationLog : Migration
{
    private static readonly string[] Tables =
    {
        "Employees", "Passengers", "Routes", "Trains", "Carriages", "Trips",
        "Tickets", "ReturnRequests", "CashShifts", "CashEntries"
    };

    protected override void Up(MigrationBuilder migration)
    {
        migration.Sql("""
            CREATE TABLE OperationLog (
                Id bigint IDENTITY PRIMARY KEY,
                OccurredUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
                Actor nvarchar(256) NOT NULL,
                EntityName nvarchar(64) NOT NULL,
                EntityId int NOT NULL,
                Operation varchar(6) NOT NULL
            );
            """);

        foreach (var table in Tables)
        {
            // One entry per affected row, including bulk and cascading writes.
            migration.Sql($"""
                CREATE TRIGGER [TR_{table}_Audit] ON [{table}]
                AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    INSERT INTO OperationLog (Actor, EntityName, EntityId, Operation)
                    SELECT COALESCE(CONVERT(nvarchar(256), SESSION_CONTEXT(N'RailwayActor')), ORIGINAL_LOGIN()),
                           N'{table}', COALESCE(i.Id, d.Id),
                           CASE WHEN d.Id IS NULL THEN 'CREATE' WHEN i.Id IS NULL THEN 'DELETE' ELSE 'UPDATE' END
                    FROM inserted i FULL OUTER JOIN deleted d ON i.Id = d.Id;
                END;
                """);
        }
    }

    protected override void Down(MigrationBuilder migration)
    {
        foreach (var table in Tables)
            migration.Sql($"DROP TRIGGER [TR_{table}_Audit];");
        migration.Sql("DROP TABLE OperationLog;");
    }
}
