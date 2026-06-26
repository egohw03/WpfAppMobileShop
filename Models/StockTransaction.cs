using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WpfAppMobileShop.Models
{
    public class StockTransaction
    {
        [Key]
        public int StockTransactionId { get; set; }

        public int ProductId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [StringLength(20)]
        public string Type { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        public int UserId { get; set; }

        public int? SupplierId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("SupplierId")]
        public virtual Supplier Supplier { get; set; }
    }
}
