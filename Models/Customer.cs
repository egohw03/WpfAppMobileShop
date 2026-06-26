using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WpfAppMobileShop.Models
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }

        [Required]
        [StringLength(200)]
        public string FullName { get; set; }

        [Required]
        [StringLength(20)]
        public string Phone { get; set; }

        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(500)]
        public string Address { get; set; }

        public DateTime CreatedDate { get; set; }

        public virtual ICollection<Order> Orders { get; set; }
    }
}
