using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Senparc.CO2NET.Extensions;
using Senparc.Weixin;
using Senparc.Weixin.Exceptions;
using Senparc.Weixin.TenPay;
using Senparc.Weixin.TenPay.V3;
using Senparc.Weixin.WxOpen.AdvancedAPIs.Sns;
using TencentPay.Models;
using TencentPay.Services;

namespace TencentPay.Controllers
{
    [Route("api/TenPay")]
    [ApiController]
    public class TenPayController : ControllerBase
    {

        private readonly IConfiguration Configuration;
        private readonly IServiceProvider _serviceProvider;

        public ITenPyConfigRead TenPyConfigRead { get; }
        public IWxLogin WxLogin { get; }

        public TenPayController(IConfiguration configuration,
                                ITenPyConfigRead tenPyConfigRead,
                                IHttpClientFactory httpClientFactory,
                                IWxLogin wxLogin,
                                IServiceProvider serviceProvider
                                )
        {
            Configuration = configuration;
            TenPyConfigRead = tenPyConfigRead;
            WxLogin = wxLogin;
            _serviceProvider = serviceProvider;
        }
        [HttpPost("Uorder")]
        public async Task<IActionResult> Unifiedorder(UnifiedorderModel unifiedorder)
        {
            string appid = TenPyConfigRead.AppId;
            string scret = TenPyConfigRead.AppSecret;
            DateTimeOffset? timestart = null;
            DateTime? timeexpire = null;
            string feetype = "CNY";

            //      var result = await WxLogin.SendCodeForOpenId(appid, scret, code);
            //     WxLoginModel loginModel= JsonSerializer.Deserialize<WxLoginModel>(result); 
            if (string.IsNullOrWhiteSpace(unifiedorder.NotifyUrl))
            {
                unifiedorder.NotifyUrl = TenPyConfigRead.TenPayV3_WxOpenNotify;
            }
            if (string.IsNullOrWhiteSpace(unifiedorder.NonceStr))
            {
                unifiedorder.NonceStr = TenPayV3Util.GetNoncestr();
            }
            if (!string.IsNullOrWhiteSpace(unifiedorder.TimeStart))
            {
                timestart = DateTimeOffset.Parse(unifiedorder.TimeStart);
            }
            if (!string.IsNullOrWhiteSpace(unifiedorder.TimeExpire))
            {
                timeexpire = DateTime.Parse(unifiedorder.TimeExpire);
            }
            if (!string.IsNullOrWhiteSpace(unifiedorder.FeeType))
            {
                feetype = unifiedorder.FeeType;
            }


            var xmlDataInfo = new TenPayV3UnifiedorderRequestData(appId: TenPyConfigRead.AppId,
                                                                  mchId: TenPyConfigRead.MchId,
                                                                  body: unifiedorder.Body,
                                                                  outTradeNo: unifiedorder.OutTradeNo,
                                                                  totalFee: unifiedorder.TotalFee,
                                                                  spbillCreateIp: unifiedorder.SpbillCreateIP,
                                                                  notifyUrl: unifiedorder.NotifyUrl,
                                                                  tradeType: TenPayV3Type.JSAPI,
                                                                  openid: unifiedorder.OpenId,
                                                                  key: TenPyConfigRead.Key,
                                                                  nonceStr: unifiedorder.NonceStr,
                                                                  deviceInfo: unifiedorder.DeviceInfo,
                                                                  timeStart: timestart,
                                                                  timeExpire: timeexpire,
                                                                  detail: unifiedorder.Detail,
                                                                  attach: unifiedorder.Attach,
                                                                  feeType: feetype,
                                                                  goodsTag: unifiedorder.GoodsTag,
                                                                  productId: unifiedorder.ProductId,
                                                                  limitPay: unifiedorder.LimitPay);

            var result = await TenPayV3.UnifiedorderAsync(xmlDataInfo);//调用统一订单接口
            
            return Ok(new
            {               
                respond = result,
                request = unifiedorder
            });
        }


        [HttpPost("OrderQuery")]
        public async Task<IActionResult> OrderQuery(OrderQueryModel orderQuery)
        {
            
            if (string.IsNullOrWhiteSpace(orderQuery.NonceStr))
            {
                orderQuery.NonceStr = TenPayV3Util.GetNoncestr();
            }
            if (string.IsNullOrWhiteSpace(orderQuery.OutTradeNo) && string.IsNullOrWhiteSpace(orderQuery.TransactionId))
            {
                return BadRequest("需要OutTradeNo,TransactionId之一");
            }

            TenPayV3OrderQueryRequestData datainfo = new TenPayV3OrderQueryRequestData(
                TenPyConfigRead.AppId,
                TenPyConfigRead.MchId,
                orderQuery.TransactionId,
                orderQuery.NonceStr,
                orderQuery.OutTradeNo,
                TenPyConfigRead.Key);           
            var result = await TenPayV3.OrderQueryAsync(datainfo);
            
            //string openid = res.Element("xml").Element("sign").Value;
            return Ok(new
            {
                respond = result,
                request = datainfo
            });
        }

        [HttpPost("CloseOrder")]
        public async Task<IActionResult> CloseOrder(OrderCloseModel orderClose)
        {
            
            if (string.IsNullOrWhiteSpace(orderClose.NonceStr))
            {
                orderClose.NonceStr = TenPayV3Util.GetNoncestr();
            }
            

            //设置package订单参数
          //  TenPayV3CloseOrderRequestData datainfo

            TenPayV3CloseOrderRequestData datainfo = new TenPayV3CloseOrderRequestData(
                 TenPyConfigRead.AppId,
                 TenPyConfigRead.MchId,
                 orderClose.OutTradeNo,
                 TenPyConfigRead.Key,
                 orderClose.NonceStr
                 );
            var result = await TenPayV3.CloseOrderAsync(datainfo);

            //string openid = res.Element("xml").Element("sign").Value;
            return Ok(new
            {
                respond = result,
                request = datainfo
            });
        }

        [HttpGet("Refund")]
        public async Task<IActionResult> Refund(RefundModel refund)
        {
            try
            {
                

                WeixinTrace.SendCustomLog("进入退款流程", "1");
                if (string.IsNullOrWhiteSpace(refund.NonceStr))
                {
                    refund.NonceStr = TenPayV3Util.GetNoncestr();
                }
                if (string.IsNullOrWhiteSpace(refund.OpUserId))
                {
                    refund.OpUserId = TenPyConfigRead.MchId;
                }

                if (string.IsNullOrWhiteSpace(refund.OutTradeNo) && string.IsNullOrWhiteSpace(refund.TransactionId))
                {
                    return BadRequest("需要OutTradeNo,TransactionId之一");
                }

                //      string outTradeNo = HttpContext.Session.GetString("BillNo");

                WeixinTrace.SendCustomLog("进入退款流程", "2 outTradeNo：" + refund.OutTradeNo);
                

                var dataInfo = new TenPayV3RefundRequestData(
                    appId:TenPyConfigRead.AppId,
                    mchId:TenPyConfigRead.MchId,
                    key:TenPyConfigRead.Key,
                    deviceInfo:refund.DeviceInfo,
                    nonceStr:refund.NonceStr,
                    transactionId:refund.TransactionId,
                    outTradeNo:refund.OutTradeNo,
                    outRefundNo:refund.OutRefundNo,
                    totalFee:refund.TotalFee,
                    refundFee:refund.RefundFee,
                    opUserId:refund.OpUserId,
                    refundAccount:refund.RefundAccount,
                    notifyUrl:refund.NotifyUrl);


                //#region 旧方法
                //var cert = @"D:\cert\apiclient_cert_SenparcRobot.p12";//根据自己的证书位置修改
                //var password = TenPayV3Info.MchId;//默认为商户号，建议修改
                //var result = TenPayV3.Refund(dataInfo, TenPyConfigRead.CertPath, Int32.Parse(TenPyConfigRead.CertSecret));
                //#endregion

                #region 新方法（Senparc.Weixin v6.4.4+）
                var result = await TenPayV3.RefundAsync(_serviceProvider, dataInfo);//证书地址、密码，在配置文件中设置，并在注册微信支付信息时自动记录
                #endregion

                WeixinTrace.SendCustomLog("进入退款流程", "3 Result：" + result.ToJson());
             //   ViewData["Message"] = $"退款结果：{result.result_code} {result.err_code_des}。您可以刷新当前页面查看最新结果。";
                return Ok(new
                {
                    respond = result,
                    request = dataInfo
                });
                //return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                WeixinTrace.WeixinExceptionLog(new WeixinException(ex.Message, ex));

                throw;
            }

            #region 原始方法

            //RequestHandler packageReqHandler = new RequestHandler(null);

            //设置package订单参数
            //packageReqHandler.SetParameter("appid", TenPayV3Info.AppId);		 //公众账号ID
            //packageReqHandler.SetParameter("mch_id", TenPayV3Info.MchId);	     //商户号
            //packageReqHandler.SetParameter("out_trade_no", "124138540220170502163706139412"); //填入商家订单号
            ////packageReqHandler.SetParameter("out_refund_no", "");                //填入退款订单号
            //packageReqHandler.SetParameter("total_fee", "");                    //填入总金额
            //packageReqHandler.SetParameter("refund_fee", "100");                //填入退款金额
            //packageReqHandler.SetParameter("op_user_id", TenPayV3Info.MchId);   //操作员Id，默认就是商户号
            //packageReqHandler.SetParameter("nonce_str", nonceStr);              //随机字符串
            //string sign = packageReqHandler.CreateMd5Sign("key", TenPayV3Info.Key);
            //packageReqHandler.SetParameter("sign", sign);	                    //签名
            ////退款需要post的数据
            //string data = packageReqHandler.ParseXML();

            ////退款接口地址
            //string url = "https://api.mch.weixin.qq.com/secapi/pay/refund";
            ////本地或者服务器的证书位置（证书在微信支付申请成功发来的通知邮件中）
            //string cert = @"D:\cert\apiclient_cert_SenparcRobot.p12";
            ////私钥（在安装证书时设置）
            //string password = TenPayV3Info.MchId;
            //ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
            ////调用证书
            //X509Certificate2 cer = new X509Certificate2(cert, password, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);

            //#region 发起post请求
            //HttpWebRequest webrequest = (HttpWebRequest)HttpWebRequest.Create(url);
            //webrequest.ClientCertificates.Add(cer);
            //webrequest.Method = "post";

            //byte[] postdatabyte = Encoding.UTF8.GetBytes(data);
            //webrequest.ContentLength = postdatabyte.Length;
            //Stream stream;
            //stream = webrequest.GetRequestStream();
            //stream.Write(postdatabyte, 0, postdatabyte.Length);
            //stream.Close();

            //HttpWebResponse httpWebResponse = (HttpWebResponse)webrequest.GetResponse();
            //StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream());
            //string responseContent = streamReader.ReadToEnd();
            //#endregion

            //// var res = XDocument.Parse(responseContent);
            ////string openid = res.Element("xml").Element("out_refund_no").Value;
            //return Content("申请成功：<br>" + HttpUtility.RequestUtility.HtmlEncode(responseContent));

            #endregion

        }
        [HttpGet("RefundQuery")]
        public async Task<IActionResult> RefundQuery(RefundQueryModel refundQuery)
        {
            if (!string.IsNullOrWhiteSpace(refundQuery.NonceStr))
            {
                refundQuery.NonceStr= TenPayV3Util.GetNoncestr();
            }
            if (string.IsNullOrWhiteSpace(refundQuery.OutTradeNo) 
                && string.IsNullOrWhiteSpace(refundQuery.TransactionId)
                && string.IsNullOrWhiteSpace(refundQuery.OutRefundNo)
                && string.IsNullOrWhiteSpace(refundQuery.RefundId))
            {
                return BadRequest("需要OutTradeNo,TransactionId,OutRefundNo,RefundId之一");
            }

            TenPayV3RefundQueryRequestData dataInfo = new TenPayV3RefundQueryRequestData(
                appId: TenPyConfigRead.AppId,
                mchId: TenPyConfigRead.MchId,
                key: TenPyConfigRead.Key,
                nonceStr: refundQuery.NonceStr,
                deviceInfo: refundQuery.DeviceInfo,
                transactionId: refundQuery.TransactionId,
                outTradeNo: refundQuery.OutTradeNo,
                outRefundNo:refundQuery.OutRefundNo,
                refundId: refundQuery.RefundId,
                offset: refundQuery.Offset
                );

            var result =await TenPayV3.RefundQueryAsync(dataInfo);

            return Ok(new
            {
                respond = result,
                request = dataInfo
            });
        }

        #region 对账单

        /// <summary>
        /// 下载对账单
        /// </summary>
        /// <param name="date">日期，格式如：20170716</param>
        /// <returns></returns>
        public ActionResult DownloadBill(DownloadBillModel downloadBill)
        {
            if (!Request.IsLocal())
            {
                return Forbid("无权访问！");
            }
            if (string.IsNullOrWhiteSpace(downloadBill.NonceStr))
            {
                downloadBill.NonceStr = TenPayV3Util.GetNoncestr();
            }
            if (string.IsNullOrWhiteSpace(downloadBill.BillType))
            {
                downloadBill.BillType = "ALL";
            }
            if (string.IsNullOrWhiteSpace(downloadBill.DeviceInfo)){
                downloadBill.DeviceInfo = TenPyConfigRead.DeviceInfo;
            }
            TenPayV3DownloadBillRequestData data = new TenPayV3DownloadBillRequestData(
                appId: TenPyConfigRead.AppId,
                mchId: TenPyConfigRead.MchId,
                nonceStr: downloadBill.NonceStr,
                deviceInfo: downloadBill.DeviceInfo,
                billDate: downloadBill.BillDate,
                billType: downloadBill.BillType,
                key: TenPyConfigRead.Key
                );
         //   TenPayV3DownloadBillRequestData data = new TenPayV3DownloadBillRequestData(TenPayV3Info.AppId, TenPayV3Info.MchId, nonceStr, null, date, "ALL", TenPayV3Info.Key, null);
            var result = TenPayV3.DownloadBill(data);
            return Content(result);
        }

        #endregion


       

    }
}
