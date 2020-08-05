namespace TencentPay.Models
{
    public class RefundQueryModel
    {
        public string TransactionId { get; set; }
        public string OutTradeNo { get; set; }
        public string NonceStr { get; set; }
        public string SignType { get; set; }
        public string DeviceInfo { get; set; }
        public string OutRefundNo { get; set; }
        public string RefundId { get; set; }
        public int? Offset { get; set; }

    }
}
