using System.ComponentModel.DataAnnotations;

namespace RailwayTickets.Models
{
    public class Train
    {
        public int Id { get; set; }

        [Required]
        public string TrainNumber { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty;

        public int CarriageCount { get; set; }

        public ICollection<Carriage> Carriages { get; set; } = new List<Carriage>();

        public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    }
}