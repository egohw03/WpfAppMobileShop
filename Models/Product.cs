using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WpfAppMobileShop.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; }

        public int CategoryId { get; set; }

        [StringLength(100)]
        public string Brand { get; set; }

        [StringLength(100)]
        public string Model { get; set; }

        [Required]
        public decimal Price { get; set; }

        public decimal CostPrice { get; set; }

        public int StockQuantity { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [StringLength(500)]
        public string ImageUrl { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }
    }
}
