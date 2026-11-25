using System.ComponentModel.DataAnnotations;

namespace RailwayTickets.ViewModels
{
    public class BookTicketViewModel
    {
        public string? ClientLogin { get; set; }
        public string? StudentCard { get; set; }
        public bool Linen { get; set; }
        public bool Drink { get; set; }
        public int TripId { get; set; }

        public string TripNumber { get; set; } = "";

        public string RouteName { get; set; } = "";

        public DateTime DepartureDate { get; set; }

        public decimal Price { get; set; }


        [Required(ErrorMessage = "Вкажіть номер вагона")]
        [Range(1, int.MaxValue)]
        [Display(Name = "Номер вагона")]
        public int CarriageNumber { get; set; }


        [Required(ErrorMessage = "Вкажіть номер місця")]
        [Range(1, int.MaxValue)]
        [Display(Name = "Номер місця")]
        public int SeatNumber { get; set; }


        [Required(ErrorMessage = "Вкажіть ПІБ пасажира")]
        [Display(Name = "ПІБ")]
        public string FullName { get; set; } = "";


        [Required(ErrorMessage = "Вкажіть номер телефону")]
        [Display(Name = "Телефон")]
        public string Phone { get; set; } = "";


        [EmailAddress(ErrorMessage = "Некоректна електронна адреса")]
        [Display(Name = "Email")]
        public string? Email { get; set; }


        [Required(ErrorMessage = "Вкажіть документ")]
        [Display(Name = "Документ")]
        public string Document { get; set; } = "";
    }
}