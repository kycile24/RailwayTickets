using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RailwayTickets.Data;
using RailwayTickets.Models;
using RailwayTickets.Services;

namespace RailwayTickets.Controllers;

[Authorize(Roles = "Cashier,Administrator")]
public class RegisterController(RailwayDbContext db) : Controller
{
    public IActionResult Index()
    {
        int actor = User.AccountId();
        var shifts = db.CashShifts.Include(s => s.Entries).AsQueryable();
        if (User.IsInRole("Cashier")) shifts = shifts.Where(s => s.CashierId == actor);
        return View(shifts.OrderByDescending(s => s.Id).ToList());
    }

    [HttpPost, Authorize(Roles = "Cashier")]
    public IActionResult Open()
    {
        var id = User.AccountId();
        if (!db.CashShifts.Any(s => s.CashierId == id && s.Closed == null))
        {
            db.CashShifts.Add(new CashShift { CashierId = id });
            db.SaveChanges();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = "Cashier")]
    public IActionResult Close(int id)
    {
        var shift = db.CashShifts.SingleOrDefault(s => s.Id == id && s.CashierId == User.AccountId());
        if (shift == null) return NotFound();
        if (shift.Closed != null) return RedirectToAction(nameof(Index));
        var balances = db.Trips.Where(t => t.DepartureDate >= DateTime.Today ||
            db.CashEntries.Any(e => e.CashShiftId == id && db.Tickets.Any(k => k.Id == e.TicketId && k.TripId == t.Id)))
            .Select(t => new { t.TripNumber, Capacity = t.Train!.Carriages.Sum(c => (int?)c.SeatCount) ?? 0,
                Held = t.Tickets.Count(k => k.Status == "Заброньовано"),
                Sold = t.Tickets.Count(k => k.Status == "Оформлено" || k.Status == "Оплачено") }).ToList();
        if (balances.Any(b => b.Held + b.Sold > b.Capacity)) return Conflict("Баланс місць не збігається.");
        shift.SeatBalance = string.Join("\n", balances.Select(b =>
            $"{b.TripNumber}: усього {b.Capacity}, броні {b.Held}, оформлено {b.Sold}, вільно {b.Capacity-b.Held-b.Sold}"));
        shift.Closed = DateTime.Now;
        db.SaveChanges();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = "Administrator")]
    public IActionResult Approve(int id)
    {
        var shift = db.CashShifts.Find(id);
        if (shift == null) return NotFound();
        if (shift.Closed == null || shift.Approved != null) return Conflict("Потрібна закрита незатверджена зміна.");
        shift.Approved = DateTime.Now;
        shift.ApprovedBy = User.AccountId();
        db.SaveChanges();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = "Administrator")]
    public IActionResult Reverse(int id, string reason)
    {
        var entry = db.CashEntries.Find(id);
        if (entry == null) return NotFound();
        var shift = db.CashShifts.Find(entry.CashShiftId)!;
        var ticket = db.Tickets.Include(t => t.Trip).Single(t => t.Id == entry.TicketId);
        if (shift.Approved != null || entry.ReversesId != null || string.IsNullOrWhiteSpace(reason)
            || ticket.Status != entry.NewStatus || ticket.Trip!.DepartureDate <= DateTime.Now
            || db.CashEntries.Any(e => e.TicketId == ticket.Id && e.Id > id)
            || db.ReturnRequests.Any(r => r.TicketId == ticket.Id && r.Status == "Очікує"))
            return Conflict("Перевірте причину, стан квитка та звіту. Анулювати можна останню чинну операцію.");
        if (entry.ReturnRequestId is int requestId)
            db.ReturnRequests.Find(requestId)!.Status = "Очікує";
        if (entry.PreviousStatus == "Заброньовано") ticket.CashierId = null;
        ticket.Status = entry.PreviousStatus;
        db.CashEntries.Add(new CashEntry { CashShiftId = shift.Id, TicketId = ticket.Id, ActorId = User.AccountId(),
            PreviousStatus = entry.NewStatus, NewStatus = ticket.Status, Amount = -entry.Amount, ReversesId = id, Reason = reason.Trim() });
        db.SaveChanges();
        return RedirectToAction(nameof(Index));
    }
}
