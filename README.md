<div align="center">

# RailwayTickets

### Railway booking and cashier operations in one ASP.NET Core MVC application

Search routes, reserve seats, manage ticket returns, and review cashier activity through role-based workspaces.

**ASP.NET Core 8 · C# · Entity Framework Core · SQL Server**

[Українська версія](README.uk.md)

</div>

---

## Overview

RailwayTickets is a university project built around a railway ticket workflow. Passengers can find trips and manage bookings; cashiers process ticket sales and return requests; administrators maintain route data and review operational records.

The app uses ASP.NET Core MVC with server-rendered Razor views, Entity Framework Core, and SQL Server. Authorization is enforced on controller actions, while the interface exposes tools for the signed-in role.

## Features

- Search trips by stations and departure date.
- View trip details and reserve a carriage seat.
- Manage passenger tickets and submit return requests.
- Process ticket sales and cashier shifts.
- Maintain trains, carriages, routes, trips, and staff accounts.
- Record cash operations and administrative changes.
- Seed realistic sample data in Development only.

## Technology

| Area | Tools |
| --- | --- |
| Application | ASP.NET Core MVC, .NET 8, C# |
| Data | Entity Framework Core 8, SQL Server, EF Core migrations |
| UI | Razor, Bootstrap, custom responsive CSS |
| Authentication | Cookie authentication, role-based authorization, ASP.NET password hashing |

## Run locally

### Requirements

- .NET 8 SDK
- SQL Server or SQL Server LocalDB
- Visual Studio 2022 or a .NET-capable editor

### Start

1. Clone the repository and open `RailwayTickets.sln`.
2. Set the connection string with User Secrets. For LocalDB, run from the repository root:

   ```bash
   dotnet user-secrets init --project RailwayTickets/RailwayTickets.csproj
   dotnet user-secrets set "ConnectionStrings:RailwayConnection" "Server=(localdb)\\MSSQLLocalDB;Database=RailwayTicketsDb;Trusted_Connection=True;TrustServerCertificate=True;" --project RailwayTickets/RailwayTickets.csproj
   ```

3. Start the app:

   ```bash
   dotnet run --project RailwayTickets/RailwayTickets.csproj
   ```

The application applies EF Core migrations at startup. Development mode also creates demonstration data. Configure the connection string for the database engine available on your machine.

Before first startup, set `Bootstrap__AdminPassword` in your environment. To populate a local Development database, also set `Demo__Password` to a separate password of at least eight characters.

## Project structure

```text
RailwayTickets/
├── RailwayTickets.sln
├── RailwayTickets/
│   ├── Controllers/       MVC endpoints and role checks
│   ├── Data/              EF Core context, migrations, and Development seed
│   ├── Models/            Domain entities
│   ├── Views/             Razor pages
│   └── wwwroot/            CSS, JavaScript, and static assets
```

## Notes

- This is an educational project, not a production railway reservation service.
- Production deployment requires a managed SQL Server connection, a strong bootstrap administrator password, HTTPS, and production review of operational and privacy requirements.

## Other projects

- [Warehouse Management System](https://github.com/kycile24/wms-api) — ASP.NET Core API for inventory, orders, and warehouse workflows.
- [TaskFlow API](https://github.com/kycile24/TaskFlow.Api) — .NET 8 task-management API with layered services and repositories.

## License

No open-source license has been selected yet. All rights remain with the author unless a license is added.

The Render demo uses SQLite with fictional trips and passengers. Replacing the container resets its data.
