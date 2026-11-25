using System.ComponentModel.DataAnnotations;

namespace RailwayTickets.Models
{
    public class Carriage
    {
        public int Id { get; set; }

        public int CarriageNumber { get; set; }

        [Required]
        public string CarriageType { get; set; } = string.Empty;

        public int SeatCount { get; set; }

        public int TrainId { get; set; }

        public Train? Train { get; set; }
    }
}