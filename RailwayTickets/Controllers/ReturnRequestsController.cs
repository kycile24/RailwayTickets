using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RailwayTickets.Data;
using RailwayTickets.Models;

namespace RailwayTickets.Controllers
{
    [Authorize]
    public class ReturnRequestsController : Controller
    {
        private readonly RailwayDbContext _context;

        public ReturnRequestsController(RailwayDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var requests = _context.ReturnRequests
                .Where(r => !User.IsInRole("Client") || r.PassengerId == User.AccountId())
                .Include(r => r.Ticket)
                .Include(r => r.Passenger)
                .Include(r => r.Cashier)
                .ToList();

            return View(requests);
        }


        [HttpGet]
        public IActionResult Create(int? ticketId)
        {
            ViewBag.Tickets = _context.Tickets
         .Where(t => !User.IsInRole("Client") || t.PassengerId == User.AccountId())
         .Where(t => (t.Status == "Оформлено" || t.Status == "Оплачено") &&
                 t.Trip.DepartureDate > DateTime.Now &&
                 !_context.ReturnRequests.Any(r => r.TicketId == t.Id &&
                     (r.Status == "Очікує" || r.Status == "Схвалено")))
                            .ToList();

            if (ticketId.HasValue)
            {
                var ticket = _context.Tickets
                    .Include(t => t.Passenger)
                    .Include(t => t.Trip)
                    .FirstOrDefault(t => t.Id == ticketId.Value);

                if (ticket == null || (User.IsInRole("Client") && ticket.PassengerId != User.AccountId())) return NotFound();

                if ((ticket.Status != "Оформлено" && ticket.Status != "Оплачено") ||
                    ticket.Trip.DepartureDate <= DateTime.Now)
                {
                    TempData["Error"] =
                        "Повернення можливе лише для оформленого квитка.";

                    return RedirectToAction(nameof(Index));
                }

                return View(new ReturnRequest
                {
                    TicketId = ticket.Id,
                    PassengerId = ticket.PassengerId,
                    RequestDate = DateTime.Now,
                    Status = "Очікує"
                });
            }

            return View(new ReturnRequest
            {
                RequestDate = DateTime.Now,
                Status = "Очікує"
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(int ticketId, string reason)
        {
            Ticket? ticket = _context.Tickets
                .Include(t => t.Passenger)
                .Include(t => t.Trip)
                .FirstOrDefault(t => t.Id == ticketId);

            if (ticket == null || (User.IsInRole("Client") && ticket.PassengerId != User.AccountId()))
                return NotFound();

            if ((ticket.Status != "Оформлено" && ticket.Status != "Оплачено") ||
                ticket.Trip.DepartureDate <= DateTime.Now)
            {
                return RedirectToAction(
                    "Details",
                    "Tickets",
                    new { id = ticket.Id });
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] =
                    "Необхідно вказати причину повернення.";

                return RedirectToAction(
                    nameof(Create),
                    new { ticketId });
            }

            bool requestExists = _context.ReturnRequests.Any(r =>
                r.TicketId == ticketId &&
                (r.Status == "Очікує" || r.Status == "Схвалено"));

            if (requestExists)
            {
                TempData["Error"] =
                    "Для цього квитка вже існує заявка на повернення.";

                return RedirectToAction(
                    "Details",
                    "Tickets",
                    new { id = ticket.Id });
            }

            ReturnRequest request = new ReturnRequest
            {
                TicketId = ticket.Id,
                PassengerId = ticket.PassengerId,
                Reason = reason,
                RequestDate = DateTime.Now,
                Status = "Очікує",
                CashierId = null
            };

            _context.ReturnRequests.Add(request);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]


        [Authorize(Roles = "Cashier")]
        public IActionResult Approve(int id)
        {
            var request = _context.ReturnRequests
                .Include(r => r.Ticket)
                .ThenInclude(t => t.Trip)
                .FirstOrDefault(r => r.Id == id);

            if (request == null)
                return NotFound();

            if (request.Status != "Очікує")
            {
                TempData["Error"] = "Заявка вже оброблена.";
                return RedirectToAction(nameof(Index));
            }

            if (request.Ticket == null || request.Ticket.Trip == null)
                return NotFound();

            if (request.Ticket.Trip.DepartureDate <= DateTime.Now)
            {
                TempData["Error"] =
                    "Неможливо схвалити повернення: рейс уже відправився.";

                return RedirectToAction(nameof(Index));
            }

            if (request.Ticket.Status != "Оплачено" &&
                request.Ticket.Status != "Оформлено")
            {
                TempData["Error"] =
                    "Цей квиток уже повернено або скасовано.";

                return RedirectToAction(nameof(Index));
            }

            Cashier? cashier = _context.Cashiers.Find(User.AccountId());

            request.Status = "Схвалено";

            if (cashier != null)
                request.CashierId = cashier.Id;

            if (!CashDesk.Record(_context, request.Ticket, User.AccountId(), "Повернено", -request.Ticket.Price, request.Id))
            {
                TempData["ShiftError"] = "Для повернення відкрийте касову зміну.";
                return RedirectToAction("Index", "Register");
            }

            _context.SaveChanges();

            TempData["Success"] = "Повернення квитка схвалено.";

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Cashier")]
        public IActionResult Reject(int id)
        {
            ReturnRequest? request = _context.ReturnRequests
                .FirstOrDefault(r => r.Id == id);

            if (request == null)
                return NotFound();

            if (request.Status == "Очікує")
            {
                Cashier? cashier = _context.Cashiers.Find(User.AccountId());

                request.Status = "Відхилено";

                if (cashier != null)
                    request.CashierId = cashier.Id;

                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }

        // РЕДАГУВАННЯ ЗАЯВКИ
        [HttpGet]
        [Authorize(Roles = "Administrator,Cashier")]
        public IActionResult Edit(int id)
        {
            var request = _context.ReturnRequests.Find(id);
            if (request == null) return NotFound();

            ViewBag.Cashiers = _context.Cashiers.ToList();
            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Cashier")]
        public IActionResult Edit(int id, string reason, int? cashierId)
        {
            var request = _context.ReturnRequests.Find(id);
            if (request == null) return NotFound();

            if (request.Status != "Очікує")
            {
                TempData["Error"] = "Можна редагувати тільки заявки, що очікують розгляду.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                ModelState.AddModelError("", "Вкажіть причину повернення.");
                ViewBag.Cashiers = _context.Cashiers.ToList();
                return View(request);
            }

            if (cashierId.HasValue &&
                !_context.Cashiers.Any(c => c.Id == cashierId.Value))
            {
                ModelState.AddModelError("", "Касира не знайдено.");
                ViewBag.Cashiers = _context.Cashiers.ToList();
                return View(request);
            }

            request.Reason = reason;
            request.CashierId = cashierId;

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // ВИДАЛЕННЯ ЗАЯВКИ
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Cashier")]
        public IActionResult Delete(int id)
        {
            var request = _context.ReturnRequests.Find(id);
            if (request == null) return NotFound();

            if (request.Status == "Схвалено")
            {
                TempData["Error"] = "Не можна видалити схвалену заявку: квиток уже повернуто.";
                return RedirectToAction(nameof(Index));
            }

            _context.ReturnRequests.Remove(request);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

    }
}