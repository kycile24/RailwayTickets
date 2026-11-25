using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RailwayTickets.Data;
using RailwayTickets.Models;

namespace RailwayTickets.Controllers
{
    [Authorize(Roles = "Cashier")]
    public class CashierController : Controller
    {
        private readonly RailwayDbContext _context;

        public CashierController(RailwayDbContext context)
        {
            _context = context;
        }

        // Робоче місце касира
        [HttpGet]
        public IActionResult Index()
        {
            var tickets = _context.Tickets
                .Include(t => t.Passenger)
                .Include(t => t.Trip)
                    .ThenInclude(t => t.Route)
                .Include(t => t.Cashier)
                .ToList();

            return View(tickets);
        }

        // Оформлення заброньованого квитка
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IssueTicket(int id)
        {
            Ticket? ticket = _context.Tickets.Include(t => t.Trip)
                .FirstOrDefault(t => t.Id == id);

            if (ticket == null)
                return NotFound();

            if (ticket.Status == "Заброньовано")
            {
                Cashier? cashier = _context.Cashiers.Find(User.AccountId());

                if (cashier != null)
                {
                    if (ticket.Trip!.DepartureDate <= DateTime.Now) return Conflict(Ui.Pick("Рейс уже відправився.", "The trip has already departed."));
                    if (!CashDesk.Record(_context, ticket, cashier.Id, "Оформлено", ticket.Price))
                    {
                        TempData["ShiftError"] = Ui.Pick("Перед оформленням квитка відкрийте касову зміну.", "Open a shift before issuing a ticket.");
                        return RedirectToAction("Index", "Register");
                    }
                    ticket.CashierId = cashier.Id;

                    _context.SaveChanges();
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // Скасування квитка
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelTicket(int id)
        {
            Ticket? ticket = _context.Tickets.Include(t => t.Trip)
                .FirstOrDefault(t => t.Id == id);

            if (ticket == null)
                return NotFound();

            if (ticket.Status == "Заброньовано")
            {
                ticket.Status = "Скасовано";
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}