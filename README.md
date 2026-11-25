# RailwayTickets

A railway booking application built with ASP.NET Core MVC. A passenger reserves a seat, a cashier issues the ticket, and an administrator reconciles the shift.

**[Explore the live timetable](https://railwaytickets-kycile24.onrender.com/Trips)** · [Українською](README.uk.md)

Use **УКР / EN** in the navigation bar to switch languages. Your browser remembers the selection.

## Try a booking

Open the Kyiv → Lviv trip to inspect its carriages, seats, and fares. To reserve a seat, register a passenger account using fictional details. The public demo uses SQLite and restores its sample schedule and tickets when Render replaces the container.

| Workspace | What you can do |
| --- | --- |
| Passenger | Find a trip, reserve a seat, view tickets, request a refund |
| Cashier | Issue tickets, review refund requests, close a cash shift |
| Administrator | Manage schedules and staff, approve shift reports, review the audit trail |

## How seat reservations stay consistent

A unique index allows only one active ticket per trip, carriage, and seat. Ticket changes and their associated cash entries commit in a single transaction. Controller actions enforce staff permissions on the server.

[Database constraints](RailwayTickets/Data/RailwayDbContext.cs) · [Transaction handling](RailwayTickets/Services/AtomicWrite.cs) · [Booking flow](RailwayTickets/Controllers/TicketsController.cs)

## Run locally

From the repository root, run these commands in Bash with Docker installed. Choose a password for the initial administrator. Open http://localhost:8080/Trips when the application starts.

```bash
docker build -t railwaytickets .
read -rs -p 'Admin password: ' Bootstrap__AdminPassword; echo
export Bootstrap__AdminPassword
docker run --rm -p 8080:10000 -e Bootstrap__AdminPassword railwaytickets
```

**Stack** · C# / .NET 10 · Razor MVC · EF Core · SQL Server / SQLite · Docker · Render

Related project · [Warehouse Management API](https://github.com/kycile24/wms-api)
