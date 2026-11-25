using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RailwayTickets.Data;
using RailwayTickets.Models;
using RailwayTickets.ViewModels;

namespace RailwayTickets.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly RailwayDbContext _context;

        public TicketsController(RailwayDbContext context)
        {
            _context = context;
        }

        // Список усіх квитків
        [HttpGet]
        public IActionResult Index()
        {
            var tickets = _context.Tickets
                .Where(t => !User.IsInRole("Client") || t.PassengerId == User.AccountId())
                .Include(t => t.Passenger)
                .Include(t => t.Trip)
                    .ThenInclude(t => t.Route)
                .Include(t => t.Cashier)
                .ToList();

            return View(tickets);
        }

        // GET: /Tickets/Book?tripId=1
        [HttpGet]
        [Authorize(Roles = "Client,Cashier")]
        public IActionResult Book(int tripId)
        {
            Trip? trip = _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Train)
                    .ThenInclude(t => t.Carriages)
                .FirstOrDefault(t => t.Id == tripId);

            if (trip == null)
                return NotFound();

            if (trip.DepartureDate <= DateTime.Now)
            {
                TempData["Error"] = "Бронювання на рейс, що вже відправився, неможливе.";
                return RedirectToAction("Index", "Trips");
            }

            BookTicketViewModel model = new BookTicketViewModel
            {
                TripId = trip.Id,
                TripNumber = trip.TripNumber,
                RouteName = trip.Route.Name,
                DepartureDate = trip.DepartureDate,
                Price = trip.BasePrice
            };

            if (User.IsInRole("Client"))
            {
                var owner = _context.Passengers.Find(User.AccountId())!;
                model.FullName = owner.FullName;
                model.Phone = owner.Phone;
                model.Email = owner.Email;
                model.Document = owner.Document;
            }
            ViewBag.Carriages = trip.Train.Carriages;
            return View(model);
        }

        // POST: /Tickets/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Client,Cashier")]
        public IActionResult Book(BookTicketViewModel model)
        {
            Trip? trip = _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Train)
                    .ThenInclude(t => t.Carriages)
                .FirstOrDefault(t => t.Id == model.TripId);

            if (trip == null)
                return NotFound();

            if (trip.DepartureDate <= DateTime.Now)
                ModelState.AddModelError("", "Рейс уже відправився. Бронювання неможливе.");

            Carriage? carriage = trip.Train.Carriages
                .FirstOrDefault(c =>
                    c.CarriageNumber == model.CarriageNumber);

            if (carriage == null)
            {
                ModelState.AddModelError(
                    nameof(model.CarriageNumber),
                    "Обраного вагона не існує.");
            }
            else
            {
                if (model.SeatNumber < 1 ||
                    model.SeatNumber > carriage.SeatCount)
                {
                    ModelState.AddModelError(
                        nameof(model.SeatNumber),
                        $"У цьому вагоні доступні місця від 1 до {carriage.SeatCount}.");
                }

                // LINQ-запит до БД:
                // перевірка, чи не зайняте місце
                bool seatBusy = _context.Tickets.Any(t =>
                    t.TripId == model.TripId &&
                    t.CarriageNumber == model.CarriageNumber &&
                    t.SeatNumber == model.SeatNumber &&
                    t.Status != "Скасовано" &&
                    t.Status != "Повернено");

                if (seatBusy)
                {
                    ModelState.AddModelError(
                        nameof(model.SeatNumber),
                        "Це місце вже заброньовано.");
                }
            }

            Passenger? accountPassenger = null;
            if (User.IsInRole("Client"))
                accountPassenger = _context.Passengers.Find(User.AccountId());
            else if (!string.IsNullOrWhiteSpace(model.ClientLogin))
            {
                accountPassenger = _context.Passengers.SingleOrDefault(p => p.AccountLogin == model.ClientLogin.Trim());
                if (accountPassenger == null || accountPassenger.Document != model.Document?.Trim())
                    ModelState.AddModelError(nameof(model.ClientLogin), "Логін або документ клієнта не збігаються.");
            }
            if (!string.IsNullOrWhiteSpace(model.StudentCard) &&
                !System.Text.RegularExpressions.Regex.IsMatch(model.StudentCard.Trim(), @"^[\p{L}\d-]{5,24}$"))
                ModelState.AddModelError(nameof(model.StudentCard), "Номер студентського: 5–24 літери, цифри або дефіс.");
            if (!ModelState.IsValid)
            {
                model.TripNumber = trip.TripNumber;
                model.RouteName = trip.Route.Name;
                model.DepartureDate = trip.DepartureDate;
                model.Price = trip.BasePrice;

                ViewBag.Carriages = trip.Train.Carriages;

                return View(model);
            }

            Passenger passenger = accountPassenger ?? new Passenger
            {
                FullName = model.FullName,
                Phone = model.Phone,
                Email = model.Email ?? "",
                Document = model.Document
            };



            Ticket ticket = new Ticket
            {
                TicketNumber =
                    $"TK-{Guid.NewGuid():N}",

                CarriageNumber = model.CarriageNumber,
                SeatNumber = model.SeatNumber,
                Price = decimal.Round(trip.BasePrice * (string.IsNullOrWhiteSpace(model.StudentCard) ? 1m : 0.5m)
                    + (model.Linen ? 60m : 0m) + (model.Drink ? 25m : 0m), 2),
                StudentCard = model.StudentCard?.Trim(), Linen = model.Linen, Drink = model.Drink,
                Status = "Заброньовано",

                Passenger = passenger,
                TripId = trip.Id,

                CashierId = null
            };

            _context.Tickets.Add(ticket);
            _context.SaveChanges();

            return RedirectToAction(
                nameof(Details),
                new { id = ticket.Id });
        }

        // GET: /Tickets/Details/1
        [HttpGet]
        public IActionResult Details(int id)
        {
            Ticket? ticket = _context.Tickets
                .Include(t => t.Passenger)
                .Include(t => t.Trip)
                    .ThenInclude(t => t.Route)
                .Include(t => t.Trip)
                    .ThenInclude(t => t.Train)
                .Include(t => t.Cashier)
                .FirstOrDefault(t => t.Id == id);

            if (ticket == null || (User.IsInRole("Client") && ticket.PassengerId != User.AccountId()))
                return NotFound();

            return View(ticket);
        }

        // ДОДАВАННЯ КВИТКА
        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public IActionResult Create()
        {
            PrepareTicketLists();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public IActionResult Create(Ticket model)
        {
            ModelState.Remove("Passenger");
            ModelState.Remove("Trip");
            ModelState.Remove("Cashier");

            if (_context.Tickets.Any(t => t.TicketNumber == model.TicketNumber))
                ModelState.AddModelError("TicketNumber", "Такий номер квитка вже існує.");

            if (model.Status != "Заброньовано")
                ModelState.AddModelError("Status", "Новий квиток створюється як бронювання. Оформлення виконує касир.");
            ValidateTicketSeat(model);

            if (!ModelState.IsValid)
            {
                PrepareTicketLists();
                return View(model);
            }

            _context.Tickets.Add(model);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // РЕДАГУВАННЯ КВИТКА
        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public IActionResult Edit(int id)
        {
            var ticket = _context.Tickets.Find(id);
            if (ticket == null) return NotFound();

            PrepareTicketLists();
            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public IActionResult Edit(Ticket model)
        {
            var ticket = _context.Tickets.Find(model.Id);
            if (ticket == null) return NotFound();

            if (_context.CashEntries.Any(e => e.TicketId == ticket.Id) || ticket.Status == "Повернено" || ticket.Status == "Скасовано" ||
                _context.ReturnRequests.Any(r => r.TicketId == ticket.Id &&
                    (r.Status == "Очікує" || r.Status == "Схвалено")))
            {
                TempData["Error"] = "Редагування заблоковане через касову операцію, остаточний статус або активну заявку.";
                return RedirectToAction(nameof(Index));
            }

            if (model.Status == "Повернено")
                ModelState.AddModelError(nameof(model.Status), "Повернення оформлюється тільки через заявку.");

            ModelState.Remove("Passenger");
            ModelState.Remove("Trip");
            ModelState.Remove("Cashier");

            if (_context.Tickets.Any(t =>
                t.TicketNumber == model.TicketNumber && t.Id != model.Id))
                ModelState.AddModelError("TicketNumber", "Такий номер квитка вже існує.");

            if (model.Status != ticket.Status && !(ticket.Status == "Заброньовано" && model.Status == "Скасовано"))
                ModelState.AddModelError("Status", "Використайте касове оформлення або заявку на повернення.");
            ValidateTicketSeat(model, model.Id);

            if (!ModelState.IsValid)
            {
                PrepareTicketLists();
                return View(model);
            }

            ticket.TicketNumber = model.TicketNumber;
            ticket.CarriageNumber = model.CarriageNumber;
            ticket.SeatNumber = model.SeatNumber;
            ticket.Price = model.Price;
            ticket.Status = model.Status;
            ticket.PassengerId = model.PassengerId;
            ticket.TripId = model.TripId;
            ticket.CashierId = model.CashierId;

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // ВИДАЛЕННЯ КВИТКА
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public IActionResult Delete(int id)
        {
            var ticket = _context.Tickets.Find(id);
            if (ticket == null) return NotFound();

            if (_context.ReturnRequests.Any(r => r.TicketId == id))
            {
                TempData["Error"] =
                    "Неможливо видалити квиток, який має заявки на повернення.";
                return RedirectToAction(nameof(Index));
            }

            _context.Tickets.Remove(ticket);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // ПЕРЕВІРКА МІСЦЯ
        private void ValidateTicketSeat(Ticket model, int? excludeId = null)
        {
            var trip = _context.Trips
                .Include(t => t.Train)
                    .ThenInclude(t => t.Carriages)
                .FirstOrDefault(t => t.Id == model.TripId);

            if (trip == null)
            {
                ModelState.AddModelError("TripId", "Оберіть існуючий рейс.");
                return;
            }

            if (trip.DepartureDate <= DateTime.Now)
            {
                ModelState.AddModelError("TripId", "Рейс уже відправився. Оберіть майбутній рейс.");
                return;
            }

            if (model.Price <= 0)
                ModelState.AddModelError("Price", "Ціна повинна бути більшою за нуль.");

            if (excludeId == null && model.Status == "Повернено")
                ModelState.AddModelError("Status", "Не можна створити вже повернений квиток.");

            var carriage = trip.Train.Carriages
                .FirstOrDefault(c => c.CarriageNumber == model.CarriageNumber);

            if (carriage == null)
            {
                ModelState.AddModelError("CarriageNumber", "Такого вагона немає у потязі.");
                return;
            }

            if (model.SeatNumber < 1 || model.SeatNumber > carriage.SeatCount)
            {
                ModelState.AddModelError("SeatNumber",
                    $"Номер місця має бути від 1 до {carriage.SeatCount}.");
                return;
            }

            if (model.Status != "Скасовано" && model.Status != "Повернено")
            {
                bool occupied = _context.Tickets.Any(t =>
                    t.TripId == model.TripId &&
                    t.CarriageNumber == model.CarriageNumber &&
                    t.SeatNumber == model.SeatNumber &&
                    t.Id != excludeId &&
                    t.Status != "Скасовано" &&
                    t.Status != "Повернено");

                if (occupied)
                    ModelState.AddModelError("SeatNumber", "Це місце вже зайняте.");
            }
        }

        // СПИСКИ ДЛЯ ФОРМ
        private void PrepareTicketLists()
        {
            ViewBag.Passengers = _context.Passengers.ToList();
            ViewBag.Trips = _context.Trips.ToList();
            ViewBag.Cashiers = _context.Cashiers.ToList();
        }

    }
}