using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TencentPay.Services
{
    public class TenPyConfigRead:ITenPyConfigRead
    {
       // public IConfiguration Configuration { get; }
        public TenPyConfigRead(IConfiguration configuration)
        {
            DeviceInfo= configuration["DeviceInfo"];
            AppId = configuration["SenparcWeixinSetting:TenPayV3_AppId"];
            AppSecret = configuration["SenparcWeixinSetting:TenPayV3_AppSecret"];
            MchId = configuration["SenparcWeixinSetting:TenPayV3_MchId"];
            Key = configuration["SenparcWeixinSetting:TenPayV3_Key"];
            CertPath = configuration["SenparcWeixinSetting:TenPayV3_CertPath"];
            CertSecret = configuration["SenparcWeixinSetting:TenPayV3_CertSecret"];
            TenPayV3Notify = configuration["SenparcWeixinSetting:TenPayV3_TenPayV3Notify"];
            TenPayV3_WxOpenNotify = configuration["SenparcWeixinSetting:TenPayV3_WxOpenNotify"];
            TenPayV3_WxOpenTenpayRefundNotify = configuration["SenparcWeixinSetting:TenPayV3_WxOpenTenpayRefundNotify"];
        }
        public string DeviceInfo { get;  }
        public string AppId { get; }
        /// <summary>
        /// 第三方用户唯一凭证密钥，即appsecret
        /// </summary>
        public string AppSecret { get; }
        /// <summary>
        /// 商户ID
        /// </summary>
        public string MchId { get; }
        /// <summary>
        /// 商户支付密钥Key。登录微信商户后台，进入栏目【账户设置】【密码安全】【API 安全】【API 密钥】
        /// </summary>
        public string Key { get; }
        /// <summary>
        /// 微信支付证书位置（物理路径），在 .NET Core 下执行 TenPayV3InfoCollection.Register() 方法会为 HttpClient 自动添加证书
        /// </summary>
        public string CertPath { get; }
        /// <summary>
        /// 微信支付证书密码
        /// </summary>
        public string CertSecret { get; }
        /// <summary>
        /// 支付完成后的回调处理页面
        /// </summary>
        public string TenPayV3Notify { get; } // = "http://localhost/payNotifyUrl.aspx";
        /// <summary>
        /// 小程序支付完成后的回调处理页面
        /// </summary>
        public string TenPayV3_WxOpenNotify { get; }

        /// <summary>
        /// 小程序退款完成后的回调处理页面
        /// </summary>
        public string TenPayV3_WxOpenTenpayRefundNotify { get; }

        

        /// <summary>
        /// 服务商模式下，特约商户的开发配置中的AppId
        /// </summary>
        public string Sub_AppId { get; }
        /// <summary>
        /// 服务商模式下，特约商户的开发配置中的AppSecret
        /// </summary>
        public string Sub_AppSecret { get; }
        /// <summary>
        /// 服务商模式下，特约商户的商户Id
        /// </summary>
        public string Sub_MchId { get; }
        
    }
}
