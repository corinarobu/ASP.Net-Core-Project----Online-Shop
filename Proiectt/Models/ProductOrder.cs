using System.ComponentModel.DataAnnotations.Schema;

namespace Proiect.Models
{
    public class ProductOrder
    {
       
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int? ProductId { get; set; }
        public int? OrderId { get; set; }

       
        public virtual Product? Product { get; set; }
        public virtual Order? Order { get; set; }
       
    }
}
