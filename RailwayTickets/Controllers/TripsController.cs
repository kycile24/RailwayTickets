using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RailwayTickets.Data;
using RailwayTickets.Models;

namespace RailwayTickets.Controllers
{
    public class TripsController : Controller
    {
        private readonly RailwayDbContext _context;

        public TripsController(RailwayDbContext context)
        {
            _context = context;
        }

        // GET: /Trips
        // Перегляд та фільтрація рейсів
        [HttpGet]
        public IActionResult Index(
            string? startStation,
            string? endStation,
            DateTime? departureDate)
        {
            // Отримуємо рейси разом з маршрутом, потягом і вагонами
            var query = _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Train)
                    .ThenInclude(t => t.Carriages)
                .AsQueryable();

            // Фільтрація за станцією відправлення
            if (!string.IsNullOrWhiteSpace(startStation))
            {
                string station = Ui.StationKey(startStation);
                query = query.Where(t => t.Route.StartStation.Contains(station) || t.Route.StartStation.Contains(startStation));
            }

            // Фільтрація за станцією прибуття
            if (!string.IsNullOrWhiteSpace(endStation))
            {
                string station = Ui.StationKey(endStation);
                query = query.Where(t => t.Route.EndStation.Contains(station) || t.Route.EndStation.Contains(endStation));
            }

            // Фільтрація за датою відправлення
            if (departureDate.HasValue)
            {
                DateTime date = departureDate.Value.Date;
                DateTime nextDate = date.AddDays(1);

                query = query.Where(t =>
                    t.DepartureDate >= date &&
                    t.DepartureDate < nextDate);
            }

            ViewBag.StartStation = startStation;
            ViewBag.EndStation = endStation;

            ViewBag.DepartureDate =
                departureDate?.ToString("yyyy-MM-dd");

            List<Trip> trips = query.ToList();

            return View(trips);
        }

        // GET: /Trips/Details/1
        // Детальна інформація про рейс
        [HttpGet]
        public IActionResult Details(int id)
        {
            Trip? trip = _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Train)
                    .ThenInclude(t => t.Carriages)
                .FirstOrDefault(t => t.Id == id);

            if (trip == null)
                return NotFound();

            return View(trip);
        }
    }
}