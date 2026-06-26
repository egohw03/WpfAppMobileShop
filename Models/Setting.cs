using System.ComponentModel.DataAnnotations;

namespace WpfAppMobileShop.Models
{
    public class Setting
    {
        [Key]
        [StringLength(100)]
        public string Key { get; set; }

        [StringLength(500)]
        public string Value { get; set; }
    }
}
