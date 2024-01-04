using System.ComponentModel.DataAnnotations;

namespace Proiect.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }
        public bool? IsCart { get; set; } // If is cart then can be edited by the user, else it can be cancelled if early enough
        public DateTime? Date { get; set; }

        public float? Price { get; set; }

        public string? UserId { get; set; } // cheie externa(al cui uitlizator e)
        public virtual ApplicationUser? User { get; set; }


       

        // O comandă poate avea mai multe produse
        public virtual ICollection<ProductOrder>? ProductOrders { get; set; }
    }
}
