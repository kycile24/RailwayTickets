using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RailwayTickets.Data;
using RailwayTickets.Models;

namespace RailwayTickets.Controllers
{
    [Authorize(Roles = "Administrator,Cashier")]
    public class PassengersController : Controller
    {
        private readonly RailwayDbContext _context;

        public PassengersController(RailwayDbContext context)
        {
            _context = context;
        }

        // Перегляд списку всіх пасажирів
        [HttpGet]
        public IActionResult Index()
        {
            var passengers = _context.Passengers.ToList();

            return View(passengers);
        }

        // Перегляд інформації про конкретного пасажира
        [HttpGet]
        public IActionResult Details(int id)
        {
            Passenger? passenger = _context.Passengers
                .FirstOrDefault(p => p.Id == id);

            if (passenger == null)
                return NotFound();

            ViewBag.Tickets = _context.Tickets
                .Include(t => t.Trip)
                    .ThenInclude(t => t.Route)
                .Where(t => t.PassengerId == id)
                .ToList();

            return View(passenger);
        }
        // Відкриття форми додавання пасажира
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // Збереження нового пасажира
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("FullName,Document,Email,Phone")] Passenger passenger)
        {
            if (!ModelState.IsValid)
                return View(passenger);

            _context.Passengers.Add(passenger);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }
       [HttpGet]
        public IActionResult Edit(int id)
        {
            var passenger = _context.Passengers.Find(id);

            if (passenger == null)
                return NotFound();

            return View(passenger);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Passenger passenger)
        {
            if (!ModelState.IsValid)
                return View(passenger);

            var existing = _context.Passengers.Find(passenger.Id);
            if (existing == null) return NotFound();
            existing.FullName = passenger.FullName;
            existing.Document = passenger.Document;
            existing.Email = passenger.Email;
            existing.Phone = passenger.Phone;
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var passenger = _context.Passengers.Find(id);

            if (passenger == null)
                return NotFound();

            if (_context.Tickets.Any(t => t.PassengerId == id) ||
                _context.ReturnRequests.Any(r => r.PassengerId == id))
            {
                TempData["Error"] = "Неможливо видалити пасажира, який має квитки або заявки на повернення.";
                return RedirectToAction(nameof(Index));
            }

            _context.Passengers.Remove(passenger);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }
    }
}