using RailwayTickets.Models;
using RailwayTickets.Data;

namespace RailwayTickets.Services;

public static class CashDesk
{
    public static bool Record(RailwayDbContext db, Ticket ticket, int cashierId, string status, decimal amount, int? requestId = null)
    {
        var shift = db.CashShifts.SingleOrDefault(s => s.CashierId == cashierId && s.Closed == null);
        if (shift == null) return false;
        db.CashEntries.Add(new CashEntry { CashShiftId = shift.Id, TicketId = ticket.Id,
            ActorId = cashierId, PreviousStatus = ticket.Status, NewStatus = status,
            Amount = amount, ReturnRequestId = requestId });
        ticket.Status = status;
        return true;
    }
}
