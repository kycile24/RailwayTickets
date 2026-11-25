using System.ComponentModel.DataAnnotations;

namespace RailwayTickets.Models
{
    public class ReturnRequest
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        public int PassengerId { get; set; }
        public Passenger? Passenger { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        public DateTime RequestDate { get; set; } = DateTime.Now;

        [Required]
        public string Status { get; set; } = "Очікує";

        public int? CashierId { get; set; }
        public Cashier? Cashier { get; set; }
    }
}