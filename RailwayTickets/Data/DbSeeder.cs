using RailwayTickets.Models;
using Route = RailwayTickets.Models.Route;

namespace RailwayTickets.Data
{
    public static class DbSeeder
    {
        public static void Seed(RailwayDbContext context, string demoPassword)
        {
            if (!context.Passengers.Any() && !context.Trains.Any() &&
                !context.Routes.Any() && !context.Employees.Any())
                SeedInitialData(context, demoPassword);

            SeedPracticeTrip(context, demoPassword);
        }

        private static void SeedPracticeTrip(RailwayDbContext context, string demoPassword)
        {
            const string tripNumber = "141К-01";
            if (context.Trips.Any(t => t.TripNumber == tripNumber || t.TripNumber == "DEMO-KYIV-LVIV-01"))
                return;

            string login = "client1";
            int suffix = 2;
            while (context.Passengers.Any(p => p.AccountLogin == login))
                login = $"client{suffix++}";

            var client = new Passenger
            {
                FullName = "Демо особа 1", AccountLogin = login,
                PasswordHash = Access.Hash(demoPassword), Document = "DEMO-DOC-1",
                Phone = "DEMO-001", Email = "demo1@example.invalid"
            };
            var cashier = context.Cashiers.FirstOrDefault(c => c.Login == "cashier1")
                ?? context.Cashiers.OrderBy(c => c.Id).FirstOrDefault();
            if (cashier == null)
            {
                string staffLogin = "cashier1";
                int staffSuffix = 2;
                while (context.Employees.Any(e => e.Login == staffLogin))
                    staffLogin = $"cashier{staffSuffix++}";
                cashier = new Cashier { FullName = "Демо особа 2", Login = staffLogin,
                    Password = Access.Hash(demoPassword) };
                context.Cashiers.Add(cashier);
            }

            var train = new Train { TrainNumber = "141К", Type = "Пасажирський", CarriageCount = 1 };
            train.Carriages.Add(new Carriage { CarriageNumber = 1, CarriageType = "Купе", SeatCount = 36 });
            var route = new Route { RouteNumber = "КЛ-14", Name = "Київ — Львів",
                StartStation = "Київ", EndStation = "Львів" };
            DateTime departure = DateTime.Today.AddDays(7).AddHours(9);
            var trip = new Trip { TripNumber = tripNumber, Train = train, Route = route,
                BasePrice = 650m, DepartureDate = departure, ArrivalDate = departure.AddHours(7) };

            var tickets = new List<Ticket>();
            for (int seat = 1; seat <= 3; seat++)
            {
                bool issued = seat > 1;
                tickets.Add(new Ticket { TicketNumber = $"КВ-141-{seat:000}", Trip = trip,
                    Passenger = client, Cashier = issued ? cashier : null, CarriageNumber = 1,
                    SeatNumber = seat, Price = issued ? trip.BasePrice : trip.BasePrice * 0.5m + 85m,
                    StudentCard = issued ? null : "КВ104582", Linen = !issued, Drink = !issued, Status = issued ? "Оформлено" : "Заброньовано" });
            }
            context.Tickets.AddRange(tickets);
            context.SaveChanges();

            var now = DateTime.Now;
            var shift = new CashShift { CashierId = cashier.Id, Opened = now.AddHours(-2),
                Closed = now.AddMinutes(-15),
                SeatBalance = $"{tripNumber}: усього 36, броні 1, оформлено 2, вільно 33" };
            foreach (var ticket in tickets.Skip(1))
                shift.Entries.Add(new CashEntry { TicketId = ticket.Id, ActorId = cashier.Id,
                    Recorded = now.AddHours(-1), PreviousStatus = "Заброньовано",
                    NewStatus = "Оформлено", Amount = ticket.Price });
            context.CashShifts.Add(shift);
            context.ReturnRequests.Add(new ReturnRequest { TicketId = tickets[2].Id,
                PassengerId = client.Id, RequestDate = now, Status = "Очікує", Reason = "Зміна планів поїздки" });
            context.SaveChanges();
        }

        private static void SeedInitialData(RailwayDbContext context, string demoPassword)
        {
            // Поезда
            var trains = new List<Train>
            {
                new Train { TrainNumber = "091К", Type = "Швидкий", CarriageCount = 10 },
                new Train { TrainNumber = "105К", Type = "Пасажирський", CarriageCount = 8 },
                new Train { TrainNumber = "743К", Type = "Інтерсіті", CarriageCount = 9 }
            };

            context.Trains.AddRange(trains);
            context.SaveChanges();

            // Маршруты
            var routes = new List<Route>
            {
                new Route
                {
                    RouteNumber = "R001",
                    Name = "Київ — Львів",
                    StartStation = "Київ",
                    EndStation = "Львів"
                },
                new Route
                {
                    RouteNumber = "R002",
                    Name = "Київ — Одеса",
                    StartStation = "Київ",
                    EndStation = "Одеса"
                },
                new Route
                {
                    RouteNumber = "R003",
                    Name = "Київ — Харків",
                    StartStation = "Київ",
                    EndStation = "Харків"
                }
            };

            context.Routes.AddRange(routes);
            context.SaveChanges();

            // Вагоны
            var carriages = new List<Carriage>
            {
                new Carriage { CarriageNumber = 1, CarriageType = "Купе", SeatCount = 36, TrainId = trains[0].Id },
                new Carriage { CarriageNumber = 2, CarriageType = "Плацкарт", SeatCount = 54, TrainId = trains[0].Id },
                new Carriage { CarriageNumber = 3, CarriageType = "Купе", SeatCount = 36, TrainId = trains[0].Id },

                new Carriage { CarriageNumber = 1, CarriageType = "Купе", SeatCount = 36, TrainId = trains[1].Id },
                new Carriage { CarriageNumber = 2, CarriageType = "Плацкарт", SeatCount = 54, TrainId = trains[1].Id },
                new Carriage { CarriageNumber = 3, CarriageType = "Люкс", SeatCount = 18, TrainId = trains[1].Id },

                new Carriage { CarriageNumber = 1, CarriageType = "Сидячий", SeatCount = 56, TrainId = trains[2].Id },
                new Carriage { CarriageNumber = 2, CarriageType = "Сидячий", SeatCount = 56, TrainId = trains[2].Id },
                new Carriage { CarriageNumber = 3, CarriageType = "Сидячий", SeatCount = 56, TrainId = trains[2].Id },
                new Carriage { CarriageNumber = 4, CarriageType = "Сидячий", SeatCount = 56, TrainId = trains[2].Id }
            };

            context.Carriages.AddRange(carriages);
            context.SaveChanges();

            // Пассажиры
            var passengers = new List<Passenger>
            {
                new Passenger { FullName = "Демо особа 3", Phone = "DEMO-002", Email = "demo2@example.invalid", Document = "DEMO-DOC-2" },
                new Passenger { FullName = "Демо особа 4", Phone = "DEMO-003", Email = "demo3@example.invalid", Document = "DEMO-DOC-3" },
                new Passenger { FullName = "Демо особа 5", Phone = "DEMO-004", Email = "demo4@example.invalid", Document = "DEMO-DOC-4" },
                new Passenger { FullName = "Демо особа 6", Phone = "DEMO-005", Email = "demo5@example.invalid", Document = "DEMO-DOC-5" },
                new Passenger { FullName = "Демо особа 7", Phone = "DEMO-006", Email = "demo6@example.invalid", Document = "DEMO-DOC-6" },
                new Passenger { FullName = "Демо особа 8", Phone = "DEMO-007", Email = "demo7@example.invalid", Document = "DEMO-DOC-7" },
                new Passenger { FullName = "Демо особа 9", Phone = "DEMO-008", Email = "demo8@example.invalid", Document = "DEMO-DOC-8" },
                new Passenger { FullName = "Демо особа 10", Phone = "DEMO-009", Email = "demo9@example.invalid", Document = "DEMO-DOC-9" },
                new Passenger { FullName = "Демо особа 11", Phone = "DEMO-010", Email = "demo10@example.invalid", Document = "DEMO-DOC-10" },
                new Passenger { FullName = "Демо особа 12", Phone = "DEMO-011", Email = "demo11@example.invalid", Document = "DEMO-DOC-11" }
            };

            context.Passengers.AddRange(passengers);
            context.SaveChanges();

            // Кассиры
            var cashiers = new List<Cashier>
            {
                new Cashier
                {
                    FullName = "Демо особа 13",
                    Login = "cashier1",
                    Password = Access.Hash(demoPassword)
                },
                new Cashier
                {
                    FullName = "Демо особа 14",
                    Login = "cashier2",
                    Password = Access.Hash(demoPassword)
                }
            };

            context.Cashiers.AddRange(cashiers);
            context.SaveChanges();

            // Рейсы
            var trips = new List<Trip>
            {
                new Trip
                {
                    TripNumber = "T001",
                    DepartureDate = DateTime.Today.AddDays(3).AddHours(8).AddMinutes(0),
                    ArrivalDate = DateTime.Today.AddDays(3).AddHours(15).AddMinutes(0),
                    BasePrice = 650,
                    TrainId = trains[0].Id,
                    RouteId = routes[0].Id
                },
                new Trip
                {
                    TripNumber = "T002",
                    DepartureDate = DateTime.Today.AddDays(4).AddHours(9).AddMinutes(0),
                    ArrivalDate = DateTime.Today.AddDays(4).AddHours(17).AddMinutes(0),
                    BasePrice = 720,
                    TrainId = trains[1].Id,
                    RouteId = routes[1].Id
                },
                new Trip
                {
                    TripNumber = "T003",
                    DepartureDate = DateTime.Today.AddDays(5).AddHours(7).AddMinutes(30),
                    ArrivalDate = DateTime.Today.AddDays(5).AddHours(12).AddMinutes(30),
                    BasePrice = 550,
                    TrainId = trains[2].Id,
                    RouteId = routes[2].Id
                }
            };

            context.Trips.AddRange(trips);
            context.SaveChanges();

            // Билеты
            var tickets = new List<Ticket>();

            for (int i = 0; i < 10; i++)
            {
                tickets.Add(new Ticket
                {
                    TicketNumber = $"TK{i + 1:000}",
                    SeatNumber = i + 1,
                    CarriageNumber = 1,
                    Price = 500 + i * 50,
                    Status = i < 8 ? "Оплачено" : "Заброньовано",
                    PassengerId = passengers[i].Id,
                    TripId = trips[i % trips.Count].Id,
                    CashierId = cashiers[i % cashiers.Count].Id
                });
            }

            context.Tickets.AddRange(tickets);
            context.SaveChanges();

            // Заявки на возврат
            context.ReturnRequests.AddRange(
                new ReturnRequest
                {
                    TicketId = tickets[1].Id,
                    PassengerId = passengers[1].Id,
                    Reason = "Зміна планів",
                    RequestDate = DateTime.Now,
                    Status = "Очікує"
                },
                new ReturnRequest
                {
                    TicketId = tickets[4].Id,
                    PassengerId = passengers[4].Id,
                    Reason = "Не можу здійснити поїздку",
                    RequestDate = DateTime.Now,
                    Status = "Очікує"
                }
            );

            context.SaveChanges();
        }
    }
}