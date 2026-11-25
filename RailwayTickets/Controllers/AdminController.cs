using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RailwayTickets.Data;
using RailwayTickets.Models;
using Route = RailwayTickets.Models.Route;

namespace RailwayTickets.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly RailwayDbContext _context;

        public AdminController(RailwayDbContext context)
        {
            _context = context;
        }

        // Головна сторінка адміністратора
        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.TripsCount = _context.Trips.Count();
            ViewBag.TrainsCount = _context.Trains.Count();
            ViewBag.RoutesCount = _context.Routes.Count();
            ViewBag.TicketsCount = _context.Tickets.Count();

            return View();
        }

        [HttpGet]
        public IActionResult OperationLog(long? before)
        {
            var rows = _context.Database.SqlQuery<RailwayTickets.ViewModels.OperationLogRow>(
                $"SELECT Id, OccurredUtc, Actor, EntityName, EntityId, Operation FROM OperationLog");
            if (before.HasValue) rows = rows.Where(row => row.Id < before.Value);
            return View(rows.OrderByDescending(row => row.Id).Take(100).ToList());
        }

        // Список рейсів
        [HttpGet]
        public IActionResult Trips()
        {
            var trips = _context.Trips
                .Include(t => t.Train)
                .Include(t => t.Route)
                .ToList();

            return View(trips);
        }

        // Список поїздів
        [HttpGet]
        public IActionResult Trains()
        {
            var trains = _context.Trains
                .Include(t => t.Carriages)
                .ToList();

            return View(trains);
        }

        // Список маршрутів
        [HttpGet]
        public IActionResult Routes()
        {
            var routes = _context.Routes.ToList();

            return View(routes);
        }

        // Додавання рейсу
        [HttpGet]
        public IActionResult CreateTrip()
        {
            PrepareLists();

            return View(new Trip
            {
                DepartureDate = DateTime.Today.AddDays(1),
                ArrivalDate = DateTime.Today.AddDays(1).AddHours(3)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateTrip(Trip model)
        {
            Train? train = _context.Trains
                .FirstOrDefault(t => t.Id == model.TrainId);

            Route? route = _context.Routes
                .FirstOrDefault(r => r.Id == model.RouteId);

            if (train == null)
            {
                ModelState.AddModelError(
                    nameof(model.TrainId),
                    Ui.Pick("Оберіть потяг.", "Choose a train."));
            }

            if (route == null)
            {
                ModelState.AddModelError(
                    nameof(model.RouteId),
                    Ui.Pick("Оберіть маршрут.", "Choose a route."));
            }

            if (model.DepartureDate <= DateTime.Now)
            {
                ModelState.AddModelError(nameof(model.DepartureDate),
                    Ui.Pick("Дата відправлення повинна бути в майбутньому.", "Departure must be in the future."));
            }

            if (model.ArrivalDate <= model.DepartureDate)
            {
                ModelState.AddModelError(
                    nameof(model.ArrivalDate),
                    Ui.Pick("Час прибуття повинен бути пізніше часу відправлення.", "Arrival must follow departure."));
            }

            if (model.BasePrice <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.BasePrice),
                    Ui.Pick("Вартість повинна бути більшою за 0.", "Fare must be greater than 0."));
            }

            if (!ModelState.IsValid)
            {
                PrepareLists();
                return View(model);
            }

            // Id вручну не задаємо — його створює SQL Server
            model.Train = null!;
            model.Route = null!;

            _context.Trips.Add(model);
            _context.SaveChanges();

            TempData["Success"] =
                Ui.Pick("Новий рейс успішно додано.", "Trip added.");

            return RedirectToAction(nameof(Trips));
        }
        [HttpGet]
        public IActionResult Carriages()
        {
            var carriages = _context.Carriages
                .Include(c => c.Train)
                .ToList();

            return View(carriages);
        }
        [HttpGet]
        public IActionResult CreateCarriage()
        {
            ViewBag.Trains = _context.Trains.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCarriage(Carriage model)
        {
            ModelState.Remove("Train");
            ValidateCarriage(model);
            if (!ModelState.IsValid)
            {
                ViewBag.Trains = _context.Trains.ToList();
                return View(model);
            }

            _context.Carriages.Add(model);
            _context.SaveChanges();
            return RedirectToAction(nameof(Carriages));
        }

        [HttpGet]
        public IActionResult EditCarriage(int id)
        {
            var carriage = _context.Carriages.Find(id);
            if (carriage == null) return NotFound();

            ViewBag.Trains = _context.Trains.ToList();
            return View(carriage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditCarriage(Carriage model)
        {
            var carriage = _context.Carriages.Find(model.Id);
            if (carriage == null) return NotFound();

            ModelState.Remove("Train");
            ValidateCarriage(model);

            if (_context.Tickets.Any(t =>
                t.Trip.TrainId == carriage.TrainId &&
                t.CarriageNumber == carriage.CarriageNumber &&
                t.Status != "Скасовано" && t.Status != "Повернено" &&
                (t.SeatNumber > model.SeatCount ||
                 model.CarriageNumber != carriage.CarriageNumber ||
                 model.TrainId != carriage.TrainId)))
                ModelState.AddModelError("", Ui.Pick("Не можна змінити вагон або прибрати місця з чинними квитками.", "A carriage or seat with active tickets cannot be removed."));

            if (!ModelState.IsValid)
            {
                ViewBag.Trains = _context.Trains.ToList();
                return View(model);
            }

            carriage.CarriageNumber = model.CarriageNumber;
            carriage.CarriageType = model.CarriageType;
            carriage.SeatCount = model.SeatCount;
            carriage.TrainId = model.TrainId;

            _context.SaveChanges();
            return RedirectToAction(nameof(Carriages));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCarriage(int id)
        {
            var carriage = _context.Carriages.Find(id);
            if (carriage == null) return NotFound();

            if (_context.Tickets.Any(t => t.Trip.TrainId == carriage.TrainId &&
                t.CarriageNumber == carriage.CarriageNumber))
            {
                TempData["Error"] = Ui.Pick("Вагон має пов'язані квитки.", "This carriage has linked tickets.");
                return RedirectToAction(nameof(Carriages));
            }

            _context.Carriages.Remove(carriage);
            _context.SaveChanges();

            return RedirectToAction(nameof(Carriages));
        }


        private void ValidateCarriage(Carriage model)
        {
            if (model.SeatCount < 1 || model.CarriageNumber < 1)
                ModelState.AddModelError("", Ui.Pick("Номер вагона й кількість місць мають бути додатними.", "Carriage number and seat count must be positive."));
            if (!_context.Trains.Any(t => t.Id == model.TrainId))
                ModelState.AddModelError(nameof(model.TrainId), Ui.Pick("Оберіть наявний потяг.", "Choose an existing train."));
            if (_context.Carriages.Any(c => c.TrainId == model.TrainId &&
                c.CarriageNumber == model.CarriageNumber && c.Id != model.Id))
                ModelState.AddModelError(nameof(model.CarriageNumber), Ui.Pick("Цей номер вагона вже зайнятий у потязі.", "This carriage number is already used on the train."));
        }

        // Редагування розкладу
        [HttpGet]
        public IActionResult EditTrip(int id)
        {
            Trip? trip = _context.Trips
                .Include(t => t.Train)
                .Include(t => t.Route)
                .FirstOrDefault(t => t.Id == id);

            if (trip == null)
                return NotFound();

            PrepareLists();

            return View(trip);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditTrip(Trip model)
        {
            Trip? trip = _context.Trips
                .FirstOrDefault(t => t.Id == model.Id);

            if (trip == null)
                return NotFound();

            Train? train = _context.Trains
                .FirstOrDefault(t => t.Id == model.TrainId);

            Route? route = _context.Routes
                .FirstOrDefault(r => r.Id == model.RouteId);

            if (train == null)
            {
                ModelState.AddModelError(
                    nameof(model.TrainId),
                    Ui.Pick("Оберіть потяг.", "Choose a train."));
            }

            if (route == null)
            {
                ModelState.AddModelError(
                    nameof(model.RouteId),
                    Ui.Pick("Оберіть маршрут.", "Choose a route."));
            }

            if (model.DepartureDate <= DateTime.Now)
            {
                ModelState.AddModelError(nameof(model.DepartureDate),
                    Ui.Pick("Дата відправлення повинна бути в майбутньому.", "Departure must be in the future."));
            }

            if (model.ArrivalDate <= model.DepartureDate)
            {
                ModelState.AddModelError(
                    nameof(model.ArrivalDate),
                    Ui.Pick("Час прибуття повинен бути пізніше часу відправлення.", "Arrival must follow departure."));
            }

            if (model.BasePrice <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.BasePrice),
                    Ui.Pick("Вартість повинна бути більшою за 0.", "Fare must be greater than 0."));
            }

            if (_context.Tickets.Any(t => t.TripId == model.Id) &&
                (trip.DepartureDate != model.DepartureDate ||
                 trip.ArrivalDate != model.ArrivalDate ||
                 trip.TrainId != model.TrainId ||
                 trip.RouteId != model.RouteId))
            {
                ModelState.AddModelError("", Ui.Pick("Не можна змінювати розклад, потяг або маршрут рейсу, на який уже є квитки.", "The timetable cannot be changed, train or trip route, with existing tickets."));
            }

            if (!ModelState.IsValid)
            {
                PrepareLists();
                return View(model);
            }

            trip.TripNumber = model.TripNumber;
            trip.DepartureDate = model.DepartureDate;
            trip.ArrivalDate = model.ArrivalDate;
            trip.BasePrice = model.BasePrice;
            trip.TrainId = model.TrainId;
            trip.RouteId = model.RouteId;

            _context.SaveChanges();

            TempData["Success"] =
                Ui.Pick("Розклад рейсу успішно оновлено.", "Trip timetable updated.");

            return RedirectToAction(nameof(Trips));
        }

        // Підготовка списків поїздів та маршрутів
        private void PrepareLists()
        {
            ViewBag.Trains = _context.Trains.ToList();
            ViewBag.Routes = _context.Routes.ToList();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteTrip(int id)
        {
            Trip? trip = _context.Trips
                .FirstOrDefault(t => t.Id == id);

            if (trip == null)
                return NotFound();

            bool hasTickets = _context.Tickets
                .Any(t => t.TripId == id);

            if (hasTickets)
            {
                TempData["Error"] =
                    Ui.Pick("Неможливо видалити рейс, оскільки на нього вже існують квитки.", "Cannot delete this trip, because tickets already exist.");

                return RedirectToAction(nameof(Trips));
            }

            _context.Trips.Remove(trip);
            _context.SaveChanges();

            TempData["Success"] = Ui.Pick("Рейс успішно видалено.", "Trip deleted.");

            return RedirectToAction(nameof(Trips));
        }

        // ДОДАВАННЯ
        [HttpGet]
        public IActionResult CreateRoute()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateRoute(Route route)
        {
            if (!ModelState.IsValid)
                return View(route);

            _context.Routes.Add(route);
            _context.SaveChanges();

            return RedirectToAction(nameof(Routes));
        }

        // РЕДАГУВАННЯ
        [HttpGet]
        public IActionResult EditRoute(int id)
        {
            var route = _context.Routes.Find(id);
            if (route == null) return NotFound();

            return View(route);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditRoute(Route model)
        {
            var route = _context.Routes.Find(model.Id);
            if (route == null) return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            route.RouteNumber = model.RouteNumber;
            route.Name = model.Name;
            route.StartStation = model.StartStation;
            route.EndStation = model.EndStation;

            _context.SaveChanges();

            return RedirectToAction(nameof(Routes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteRoute(int id)
        {
            var route = _context.Routes.Find(id);
            if (route == null) return NotFound();

            if (_context.Trips.Any(t => t.RouteId == id))
            {
                TempData["Error"] = Ui.Pick("Маршрут використовується в рейсах.", "This route has scheduled trips.");
                return RedirectToAction(nameof(Routes));
            }

            _context.Routes.Remove(route);
            _context.SaveChanges();

            return RedirectToAction(nameof(Routes));
        }

        [HttpGet]
        public IActionResult CreateTrain() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateTrain(Train model)
        {
            if (!ModelState.IsValid) return View(model);

            _context.Trains.Add(model);
            _context.SaveChanges();
            return RedirectToAction(nameof(Trains));
        }

        [HttpGet]
        public IActionResult EditTrain(int id)
        {
            var train = _context.Trains.Find(id);
            return train == null ? NotFound() : View(train);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditTrain(Train model)
        {
            var train = _context.Trains.Find(model.Id);
            if (train == null) return NotFound();
            if (!ModelState.IsValid) return View(model);

            train.TrainNumber = model.TrainNumber;
            train.Type = model.Type;
            train.CarriageCount = model.CarriageCount;

            _context.SaveChanges();
            return RedirectToAction(nameof(Trains));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteTrain(int id)
        {
            var train = _context.Trains.Find(id);
            if (train == null) return NotFound();

            if (_context.Trips.Any(t => t.TrainId == id) ||
                _context.Carriages.Any(c => c.TrainId == id))
            {
                TempData["Error"] = Ui.Pick("Потяг має пов'язані рейси або вагони.", "This train has linked trips or carriages.");
                return RedirectToAction(nameof(Trains));
            }

            _context.Trains.Remove(train);
            _context.SaveChanges();
            return RedirectToAction(nameof(Trains));
        }

        [HttpGet]
        public IActionResult Employees()
        {
            return View(_context.Employees.ToList());
        }

        [HttpGet]
        public IActionResult CreateEmployee() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateEmployee(string fullName, string login,
            string password, string role)
        {
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrWhiteSpace(password) ||
                (role != "Cashier" && role != "Administrator"))
            {
                ModelState.AddModelError("", Ui.Pick("Заповніть усі поля.", "Complete all fields."));
                return View();
            }

            if (_context.Employees.Any(e => e.Login == login))
            {
                ModelState.AddModelError("", Ui.Pick("Такий логін уже існує.", "This username is already taken."));
                return View();
            }

            Employee employee = role == "Administrator"
                ? new Administrator()
                : new Cashier();

            employee.FullName = fullName;
            employee.Login = login;
            employee.Password = Access.Hash(password);

            _context.Employees.Add(employee);
            _context.SaveChanges();

            return RedirectToAction(nameof(Employees));
        }

        [HttpGet]
        public IActionResult EditEmployee(int id)
        {
            var employee = _context.Employees.Find(id);
            return employee == null ? NotFound() : View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditEmployee(int id, string fullName,
            string login, string password)
        {
            var employee = _context.Employees.Find(id);
            if (employee == null) return NotFound();

            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", Ui.Pick("Заповніть усі поля.", "Complete all fields."));
                return View(employee);
            }

            if (_context.Employees.Any(e => e.Login == login && e.Id != id))
            {
                ModelState.AddModelError("", Ui.Pick("Такий логін уже існує.", "This username is already taken."));
                return View(employee);
            }

            employee.FullName = fullName;
            employee.Login = login;
            employee.Password = Access.Hash(password);

            _context.SaveChanges();
            return RedirectToAction(nameof(Employees));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteEmployee(int id)
        {
            var employee = _context.Employees.Find(id);
            if (employee == null) return NotFound();

            if (employee.Id == User.AccountId())
                return Conflict(Ui.Pick("Неможливо видалити власний обліковий запис.", "You cannot delete your own account."));
            if (employee is Cashier &&
                (_context.Tickets.Any(t => t.CashierId == id) ||
                 _context.ReturnRequests.Any(r => r.CashierId == id)))
            {
                TempData["Error"] = Ui.Pick("Касир має пов'язані записи.", "This cashier has linked records.");
                return RedirectToAction(nameof(Employees));
            }

            _context.Employees.Remove(employee);
            _context.SaveChanges();

            return RedirectToAction(nameof(Employees));
        }

    }
}