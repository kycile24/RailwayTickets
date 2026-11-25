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
                TempData["Error"] = Ui.Pick("Бронювання на рейс, що вже відправився, неможливе.", "Book a seat, that has already departed, unavailable.");
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
                ModelState.AddModelError("", Ui.Pick("Рейс уже відправився. Бронювання неможливе.", "The trip has already departed. Booking is unavailable."));

            Carriage? carriage = trip.Train.Carriages
                .FirstOrDefault(c =>
                    c.CarriageNumber == model.CarriageNumber);

            if (carriage == null)
            {
                ModelState.AddModelError(
                    nameof(model.CarriageNumber),
                    Ui.Pick("Обраного вагона не існує.", "Selected carriage does not exist."));
            }
            else
            {
                if (model.SeatNumber < 1 ||
                    model.SeatNumber > carriage.SeatCount)
                {
                    ModelState.AddModelError(
                        nameof(model.SeatNumber),
                        Ui.Pick($"У цьому вагоні доступні місця від 1 до {carriage.SeatCount}.", $"Available seats on this carriage range from 1 to {carriage.SeatCount}."));
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
                        Ui.Pick("Це місце вже заброньовано.", "This seat has already been reserved."));
                }
            }

            Passenger? accountPassenger = null;
            if (User.IsInRole("Client"))
                accountPassenger = _context.Passengers.Find(User.AccountId());
            else if (!string.IsNullOrWhiteSpace(model.ClientLogin))
            {
                accountPassenger = _context.Passengers.SingleOrDefault(p => p.AccountLogin == model.ClientLogin.Trim());
                if (accountPassenger == null || accountPassenger.Document != model.Document?.Trim())
                    ModelState.AddModelError(nameof(model.ClientLogin), Ui.Pick("Логін або документ клієнта не збігаються.", "Passenger login or document does not match."));
            }
            if (!string.IsNullOrWhiteSpace(model.StudentCard) &&
                !System.Text.RegularExpressions.Regex.IsMatch(model.StudentCard.Trim(), @"^[\p{L}\d-]{5,24}$"))
                ModelState.AddModelError(nameof(model.StudentCard), Ui.Pick("Номер студентського: 5–24 літери, цифри або дефіс.", "Student ID number: 5–24 letters, digits or hyphens."));
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
                ModelState.AddModelError("TicketNumber", Ui.Pick("Такий номер квитка вже існує.", "This ticket number already exists."));

            if (model.Status != "Заброньовано")
                ModelState.AddModelError("Status", Ui.Pick("Новий квиток створюється як бронювання. Оформлення виконує касир.", "New tickets start as reservations. Tickets are issued by a cashier."));
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
                TempData["Error"] = Ui.Pick("Редагування заблоковане через касову операцію, остаточний статус або активну заявку.", "Editing is locked because of a cash transaction, final status or active request.");
                return RedirectToAction(nameof(Index));
            }

            if (model.Status == "Повернено")
                ModelState.AddModelError(nameof(model.Status), Ui.Pick("Повернення оформлюється тільки через заявку.", "Submit a request to receive a refund."));

            ModelState.Remove("Passenger");
            ModelState.Remove("Trip");
            ModelState.Remove("Cashier");

            if (_context.Tickets.Any(t =>
                t.TicketNumber == model.TicketNumber && t.Id != model.Id))
                ModelState.AddModelError("TicketNumber", Ui.Pick("Такий номер квитка вже існує.", "This ticket number already exists."));

            if (model.Status != ticket.Status && !(ticket.Status == "Заброньовано" && model.Status == "Скасовано"))
                ModelState.AddModelError("Status", Ui.Pick("Використайте касове оформлення або заявку на повернення.", "Use the ticket desk or submit a refund request."));
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
                    Ui.Pick("Неможливо видалити квиток, який має заявки на повернення.", "Cannot delete this ticket, with refund requests.");
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
                ModelState.AddModelError("TripId", Ui.Pick("Оберіть існуючий рейс.", "Choose an existing trip."));
                return;
            }

            if (trip.DepartureDate <= DateTime.Now)
            {
                ModelState.AddModelError("TripId", Ui.Pick("Рейс уже відправився. Оберіть майбутній рейс.", "The trip has already departed. Choose an upcoming trip."));
                return;
            }

            if (model.Price <= 0)
                ModelState.AddModelError("Price", Ui.Pick("Ціна повинна бути більшою за нуль.", "Price must be greater than zero."));

            if (excludeId == null && model.Status == "Повернено")
                ModelState.AddModelError("Status", Ui.Pick("Не можна створити вже повернений квиток.", "A new ticket cannot be marked as refunded."));

            var carriage = trip.Train.Carriages
                .FirstOrDefault(c => c.CarriageNumber == model.CarriageNumber);

            if (carriage == null)
            {
                ModelState.AddModelError("CarriageNumber", Ui.Pick("Такого вагона немає у потязі.", "No such carriage on this train."));
                return;
            }

            if (model.SeatNumber < 1 || model.SeatNumber > carriage.SeatCount)
            {
                ModelState.AddModelError("SeatNumber",
                    Ui.Pick($"Номер місця має бути від 1 до {carriage.SeatCount}.", $"Seat number must be between 1 to {carriage.SeatCount}."));
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
                    ModelState.AddModelError("SeatNumber", Ui.Pick("Це місце вже зайняте.", "This seat is already taken."));
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