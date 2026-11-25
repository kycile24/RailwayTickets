using System.ComponentModel.DataAnnotations;

namespace RailwayTickets.Models
{
    public class Ticket
    {
        public string? StudentCard { get; set; }
        public bool Linen { get; set; }
        public bool Drink { get; set; }
        public int Id { get; set; }

        [Required]
        public string TicketNumber { get; set; } = string.Empty;

        public int SeatNumber { get; set; }

        public decimal Price { get; set; }

        public int CarriageNumber { get; set; }

        [Required]
        public string Status { get; set; } = "Заброньовано";

        public int PassengerId { get; set; }

        public Passenger? Passenger { get; set; }

        public int TripId { get; set; }

        public Trip? Trip { get; set; }

        public int? CashierId { get; set; }

        public Cashier? Cashier { get; set; }
    }
}