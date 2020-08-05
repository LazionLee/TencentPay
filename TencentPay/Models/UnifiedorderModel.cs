using Senparc.Weixin.TenPay;
using System.ComponentModel.DataAnnotations;

namespace TencentPay.Models
{
    public class UnifiedorderModel
    {
        ///
        ///以下为必填
        ///


        [Required]
        public string OpenId { get; set; }//用户标识

        [Required]
        public string Body { get; set; }//商品描述
        [Required]
        public int TotalFee { get; set; }//标价金额
        [Required]
        public string OutTradeNo { get; set; }//商户订单号
        [Required]
        public string SpbillCreateIP { get; set; }//终端IP
        [Required]
        public string NotifyUrl { get; set; }//通知地址

        //以下为选填

        public string TimeStart { get; set; }
        public string TimeExpire { get; set; }
        public string GoodsTag { get; set; }

        public TenPayV3Type TradeType { get; set; }
        public bool LimitPay { get; set; }


        //public string SubOpenid { get; set; }
        public string ProductId { get; set; }

        public string FeeType { get; set; }
        public string ProfitSharing { get; set; }
        public string Attach { get; set; }
        public string Detail { get; set; }

        public string SignType { get; set; }
        public string NonceStr { get; set; }
        public string DeviceInfo { get; set; }
        //public string SubMchId { get; set; }
        //public string SubAppId { get; set; }
        //public string MchId { get; set; }
        //public string AppId { get; set; }

        //public string Key { get; set; }
    }
}
