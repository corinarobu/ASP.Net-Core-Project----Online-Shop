using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proiect.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Titlul produsului este obligatoriu")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Descrierea produsului este obligatorie")]
        [MinLength(10, ErrorMessage = "Descriere prea scurta")]
        [StringLength(300, ErrorMessage = "Descriere prea lunga")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Pretul produsului este obligatoriu")]
        public float? Price { get; set; }

        public int? Rating { get; set; }

        public string? Photo { get; set; }

        public DateTime? Date { get; set; }

        public bool? Active { get; set; }

        [Required(ErrorMessage = "Categoria este obligatorie")]
        public int? CategoryId { get; set; }
        public virtual Category? Category { get; set; }

        public string? ApplicationUserId { get; set; }
        public virtual ApplicationUser? ApplicationUser { get; set; }

        public virtual ICollection<Review>? Reviews { get; set; }

        public virtual ICollection<ProductOrder>? ProductOrders { get; set; }

        [NotMapped]
        public IEnumerable<SelectListItem>? Categ { get; set; }


 

    }

}
