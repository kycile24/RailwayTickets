using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RailwayTickets.Data;

namespace RailwayTickets.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class QueriesController : Controller
    {
        private readonly RailwayDbContext _context;

        public QueriesController(RailwayDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(DateTime? dateFrom, DateTime? dateTo)
        {
            DateTime from = dateFrom ?? DateTime.Today.AddMonths(-1);
            DateTime to = dateTo ?? DateTime.Today.AddMonths(1);

            // 4. Загальна сума квитків за вибраний період
            ViewBag.TotalIncome = _context.Tickets
                .Where(t =>
                    t.Trip.DepartureDate >= from &&
                    t.Trip.DepartureDate < to.AddDays(1) &&
                    (t.Status == "Оплачено" ||
                     t.Status == "Оформлено"))
                .Select(t => t.Price).AsEnumerable().Sum();

            ViewBag.DateFrom = from.ToString("yyyy-MM-dd");
            ViewBag.DateTo = to.ToString("yyyy-MM-dd");

            return View();
        }
    }
}