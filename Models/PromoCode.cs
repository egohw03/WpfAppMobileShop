using System;
using System.ComponentModel.DataAnnotations;

namespace WpfAppMobileShop.Models
{
    public class PromoCode
    {
        [Key]
        public int PromoCodeId { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        [Required]
        [StringLength(20)]
        public string DiscountType { get; set; }

        [Required]
        public decimal DiscountValue { get; set; }

        public decimal MinOrderAmount { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
