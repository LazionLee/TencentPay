using System.ComponentModel.DataAnnotations;

namespace TencentPay.Models
{
    public class OrderCloseModel
    {
        [Required]
        public string OutTradeNo { get; set; }
        public string NonceStr { get; set; }
        public string SignType { get; set; }
    }

}
