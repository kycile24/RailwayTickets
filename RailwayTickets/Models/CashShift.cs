namespace RailwayTickets.Models;

public class CashShift
{
    public int Id { get; set; }
    public int CashierId { get; set; }
    public DateTime Opened { get; set; } = DateTime.Now;
    public DateTime? Closed { get; set; }
    public DateTime? Approved { get; set; }
    public int? ApprovedBy { get; set; }
    public string? SeatBalance { get; set; }
    public List<CashEntry> Entries { get; set; } = new();
}

public class CashEntry
{
    public int Id { get; set; }
    public int CashShiftId { get; set; }
    public int TicketId { get; set; }
    public int? ReturnRequestId { get; set; }
    public DateTime Recorded { get; set; } = DateTime.Now;
    public string PreviousStatus { get; set; } = "";
    public string NewStatus { get; set; } = "";
    public decimal Amount { get; set; }
    public int? ReversesId { get; set; }
    public string? Reason { get; set; }
    public int ActorId { get; set; }
}
