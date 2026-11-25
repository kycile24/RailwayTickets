using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using RailwayTickets.Data;

namespace RailwayTickets.Migrations;

[DbContext(typeof(RailwayDbContext))]
[Migration("20261102180000_CashOperations")]
public class CashOperations : Migration
{
    protected override void Up(MigrationBuilder migration)
    {
        migration.Sql("UPDATE Tickets SET Status = N'Оплачено' WHERE Status = N'Продано';");
        migration.Sql("UPDATE t SET CarriageNumber = 1 FROM Tickets t JOIN Trips tr ON tr.Id=t.TripId WHERE t.CarriageNumber=0 AND EXISTS (SELECT 1 FROM Carriages c WHERE c.TrainId=tr.TrainId AND c.CarriageNumber=1 AND t.SeatNumber BETWEEN 1 AND c.SeatCount);");
        migration.Sql("ALTER TABLE Tickets ALTER COLUMN TicketNumber nvarchar(100) NOT NULL;");
        migration.Sql("ALTER TABLE Tickets ALTER COLUMN Status nvarchar(24) NOT NULL;");
        migration.Sql("ALTER TABLE ReturnRequests ALTER COLUMN Status nvarchar(24) NOT NULL;");
        migration.Sql("ALTER TABLE Employees ALTER COLUMN Login nvarchar(100) NOT NULL;");
        migration.Sql("ALTER TABLE Passengers ADD AccountLogin nvarchar(100) NULL, PasswordHash nvarchar(max) NULL;");
        migration.Sql("ALTER TABLE Tickets ADD StudentCard nvarchar(max) NULL, Linen bit NOT NULL CONSTRAINT DF_Tickets_Linen DEFAULT 0, Drink bit NOT NULL CONSTRAINT DF_Tickets_Drink DEFAULT 0;");
        migration.Sql("ALTER TABLE Tickets ADD CONSTRAINT CK_Ticket_Status CHECK (Status IN (N'Заброньовано',N'Оформлено',N'Оплачено',N'Повернено',N'Скасовано'));");
        migration.Sql("CREATE UNIQUE INDEX IX_Tickets_TicketNumber ON Tickets(TicketNumber);");
        migration.Sql("CREATE UNIQUE INDEX IX_Employees_Login ON Employees(Login);");
        migration.Sql("CREATE UNIQUE INDEX IX_Passengers_AccountLogin ON Passengers(AccountLogin) WHERE AccountLogin IS NOT NULL;");
        migration.Sql("CREATE UNIQUE INDEX IX_Carriages_TrainId_CarriageNumber ON Carriages(TrainId,CarriageNumber);");
        migration.Sql("CREATE UNIQUE INDEX IX_Tickets_TripId_CarriageNumber_SeatNumber ON Tickets(TripId,CarriageNumber,SeatNumber) WHERE Status IN (N'Заброньовано',N'Оформлено',N'Оплачено');");
        migration.Sql("DROP INDEX IX_ReturnRequests_TicketId ON ReturnRequests;");
        migration.Sql("CREATE UNIQUE INDEX IX_ReturnRequests_TicketId ON ReturnRequests(TicketId) WHERE Status IN (N'Очікує',N'Схвалено');");
        migration.Sql("CREATE TABLE CashShifts(Id int IDENTITY PRIMARY KEY, CashierId int NOT NULL REFERENCES Employees(Id), Opened datetime2 NOT NULL, Closed datetime2 NULL, Approved datetime2 NULL, ApprovedBy int NULL REFERENCES Employees(Id), SeatBalance nvarchar(max) NULL);");
        migration.Sql("CREATE TABLE CashEntries(Id int IDENTITY PRIMARY KEY, CashShiftId int NOT NULL REFERENCES CashShifts(Id), TicketId int NOT NULL REFERENCES Tickets(Id), ActorId int NOT NULL REFERENCES Employees(Id), ReturnRequestId int NULL REFERENCES ReturnRequests(Id), Recorded datetime2 NOT NULL, PreviousStatus nvarchar(max) NOT NULL, NewStatus nvarchar(max) NOT NULL, Amount decimal(18,2) NOT NULL, ReversesId int NULL REFERENCES CashEntries(Id), Reason nvarchar(max) NULL);");
        migration.Sql("CREATE UNIQUE INDEX IX_CashShifts_CashierId ON CashShifts(CashierId) WHERE Closed IS NULL;");
        migration.Sql("CREATE UNIQUE INDEX IX_CashEntries_ReversesId ON CashEntries(ReversesId) WHERE ReversesId IS NOT NULL;");
        migration.Sql("CREATE INDEX IX_CashEntries_CashShiftId ON CashEntries(CashShiftId);");
        migration.Sql("CREATE INDEX IX_CashEntries_ActorId ON CashEntries(ActorId);");
        migration.Sql("CREATE INDEX IX_CashEntries_ReturnRequestId ON CashEntries(ReturnRequestId);");
        migration.Sql("CREATE INDEX IX_CashEntries_TicketId ON CashEntries(TicketId);");
        migration.Sql("CREATE INDEX IX_CashShifts_ApprovedBy ON CashShifts(ApprovedBy);");
    }
    protected override void Down(MigrationBuilder migration)
    {
        migration.Sql("DROP TABLE CashEntries;");
        migration.Sql("DROP TABLE CashShifts;");
        migration.Sql("DROP INDEX IX_Carriages_TrainId_CarriageNumber ON Carriages;");
        migration.Sql("DROP INDEX IX_Passengers_AccountLogin ON Passengers;");
        migration.Sql("DROP INDEX IX_Employees_Login ON Employees;");
        migration.Sql("DROP INDEX IX_Tickets_TicketNumber ON Tickets;");
        migration.Sql("DROP INDEX IX_Tickets_TripId_CarriageNumber_SeatNumber ON Tickets;");
        migration.Sql("DROP INDEX IX_ReturnRequests_TicketId ON ReturnRequests;");
        migration.Sql("CREATE INDEX IX_ReturnRequests_TicketId ON ReturnRequests(TicketId);");
        migration.Sql("ALTER TABLE Tickets DROP CONSTRAINT CK_Ticket_Status, DF_Tickets_Drink, DF_Tickets_Linen;");
        migration.Sql("ALTER TABLE Tickets DROP COLUMN StudentCard, Linen, Drink;");
        migration.Sql("ALTER TABLE Passengers DROP COLUMN PasswordHash, AccountLogin;");
        migration.Sql("ALTER TABLE Employees ALTER COLUMN Login nvarchar(max) NOT NULL;");
        migration.Sql("ALTER TABLE Tickets ALTER COLUMN TicketNumber nvarchar(max) NOT NULL;");
        migration.Sql("ALTER TABLE Tickets ALTER COLUMN Status nvarchar(max) NOT NULL;");
        migration.Sql("ALTER TABLE ReturnRequests ALTER COLUMN Status nvarchar(max) NOT NULL;");
    }
}
