using System.ComponentModel.DataAnnotations;

namespace RailwayTickets.Models
{
    public class Passenger
    {
        public string? AccountLogin { get; set; }
        public string? PasswordHash { get; set; }
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Document { get; set; } = string.Empty;

        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

        public ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();

        public void SearchTicket() { }
        public void BookTicket() { }
        public void ViewHistory() { }
        public void CreateReturnRequest() { }


    }
}