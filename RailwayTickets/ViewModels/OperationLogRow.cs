namespace RailwayTickets.ViewModels;

public class OperationLogRow
{
    public long Id { get; set; }
    public DateTime OccurredUtc { get; set; }
    public string Actor { get; set; } = "";
    public string EntityName { get; set; } = "";
    public int EntityId { get; set; }
    public string Operation { get; set; } = "";
}
