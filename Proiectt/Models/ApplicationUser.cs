using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proiect.Models
{
    public class ApplicationUser : IdentityUser
    {
       
        public virtual ICollection<Product>? Products { get; set; }


        public virtual ICollection<Order>? Orders { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }


        [NotMapped]
        public IEnumerable<SelectListItem>? AllRoles { get; set; }

        public virtual ICollection<Review>? Reviews { get; set; }

    }
}
