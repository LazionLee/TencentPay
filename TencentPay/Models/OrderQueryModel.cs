﻿namespace TencentPay.Models
{
    public class OrderQueryModel
    {

        public string TransactionId { get; set; }
        public string OutTradeNo { get; set; }
        public string NonceStr { get; set; }
        public string SignType { get; set; }
    }
}
