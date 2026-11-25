using System.ComponentModel.DataAnnotations;

namespace RailwayTickets.Models
{
    public class Trip
    {
        public int Id { get; set; }

        [Required]
        public string TripNumber { get; set; } = string.Empty;

        public DateTime DepartureDate { get; set; }

        public DateTime ArrivalDate { get; set; }

        public decimal BasePrice { get; set; }

        public int TrainId { get; set; }

        public Train? Train { get; set; }

        public int RouteId { get; set; }

        public Route? Route { get; set; }

        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}