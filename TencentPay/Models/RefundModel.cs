using System.ComponentModel.DataAnnotations;

namespace TencentPay.Models
{
    public class RefundModel
    {
        public string TransactionId { get; set; }
        public string OutTradeNo { get; set; }
        public string NonceStr { get; set; }
        public string SignType { get; set; }
        public string DeviceInfo { get; set; }
        public string OutRefundNo { get; set; }
        [Required]
        public int TotalFee { get; set; }
        [Required]
        public int RefundFee { get; set; }
        public string RefundFeeType { get; set; }
        public string OpUserId { get; set; }
        public string RefundAccount { get; set; }
        public string NotifyUrl { get; set; }
    }
}
