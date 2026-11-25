namespace RailwayTickets.Models
{
    public class Cashier : Employee
    {
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

        public void IssueTicket()
        {
        }

        public void ReturnTicket()
        {
        }

        public void CheckAvailability()
        {
        }
        public void ProcessReturnRequest() { }
    }
}