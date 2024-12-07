using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebScraping.Models
{
    [Table("Company")]
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Href { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Website { get; set; }
        public string Pais { get; set; }
        public string Source { get; set; }
        public string NameContact { get; set; }
        public string Email { get; set; }
        public string Titlecontact { get; set; }
        public string Emailcontact { get; set; }
        public string Mobile { get; set; }

        [Column(TypeName = "date")] // Esto asegura que solo la fecha se almacene en la base de datos
        public DateTime Fecha { get; set; }

    }
}
