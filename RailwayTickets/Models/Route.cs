using System.ComponentModel.DataAnnotations;

namespace RailwayTickets.Models
{
    public class Route
    {
        public int Id { get; set; }

        [Required]
        public string RouteNumber { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string StartStation { get; set; } = string.Empty;

        [Required]
        public string EndStation { get; set; } = string.Empty;

        public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    }
}