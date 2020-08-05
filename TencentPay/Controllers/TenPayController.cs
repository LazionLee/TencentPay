using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Utilities;
using Senparc.Weixin;
using Senparc.Weixin.Exceptions;
using Senparc.Weixin.TenPay;
using Senparc.Weixin.TenPay.V3;
using Senparc.Weixin.WxOpen.AdvancedAPIs.Sns;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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
        public IHttpClientFactory _httpClientFactory { get; }
        public IWxLogin WxLogin { get; }
        public ILoggerFactory _logger { get; }

        public TenPayController(IConfiguration configuration,
                                ITenPyConfigRead tenPyConfigRead,
                                IHttpClientFactory httpClientFactory,
                                IWxLogin wxLogin,
                                IServiceProvider serviceProvider,
                                ILoggerFactory logger
                                )
        {
            Configuration = configuration;
            TenPyConfigRead = tenPyConfigRead;
            _httpClientFactory = httpClientFactory;
            WxLogin = wxLogin;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        /// <summary>
        /// 微信后台验证地址（使用Get），微信后台的“接口配置信息”的Url填写如：http://sdk.weixin.senparc.com/weixin
        /// </summary>
        [HttpGet("Bind")]
        [ActionName("Index")]
        public ActionResult Get([FromQuery]Senparc.Weixin.WxOpen.Entities.Request.PostModel postModel, string echostr)
        {
            if (Senparc.Weixin.MP.CheckSignature.Check(postModel.Signature, postModel.Timestamp, postModel.Nonce, Configuration["SenparcWeixinSetting:Token"]))

                return Content(echostr); //返回随机字符串则表示验证通过

            else
            {
                return Content("failed:" + postModel.Signature + "," + Senparc.Weixin.MP.CheckSignature.GetSignature(postModel.Timestamp, postModel.Nonce, Configuration["SenparcWeixinSetting:Token"]) + "。" +
                    "如果你在浏览器中看到这句话，说明此地址可以被作为微信公众账号后台的Url，请注意保持Token一致。");
            }
        }

        //[HttpGet("Login")]
        //public IActionResult Login([FromQuery]string code)
        //{
        //    var sender = _httpClientFactory.CreateClient();
            
        //     var detail = SnsApi.JsCode2Json(TenPyConfigRead.AppId, TenPyConfigRead.AppSecret, code);
            
        //    return Ok(detail);
        //}
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
            var log = _logger.CreateLogger("统一下单");
            if(result.return_code=="FAIL")
            {
                log.LogError($"商家订单号(OutTradeNo):{unifiedorder.OutTradeNo}   通讯标记(return_code):{result.return_code}  {result.return_msg}");
            }
            if(result.result_code== "FAIL")
            {
                log.LogError($"商家订单号(OutTradeNo):{unifiedorder.OutTradeNo}   业务结果(result_code):{result.result_code}\n{result.err_code}:{result.err_code_des}");
            }
            else if(result.result_code == "SUCCESS")
            {
                log.LogInformation($"商家订单号(OutTradeNo):{unifiedorder.OutTradeNo}   业务结果(result_code):{result.result_code}");
            }
            var timeStamp = TenPayV3Util.GetTimestamp();
            
            var package = string.Format("prepay_id={0}", result.prepay_id);
            var paysign = TenPayV3.GetJsPaySign(TenPyConfigRead.AppId, timeStamp,unifiedorder.NonceStr, package, TenPyConfigRead.Key);

           // return Ok(new { timeStamp, nonceStr, package, paysign });
            return Ok(new
            {
                client = new
                {
                    timeStamp,
                    unifiedorder.NonceStr,
                    package,
                    paysign,
                    sign="MD5"
                },
                respond = result,
                request = unifiedorder
                
            });
        }
        [HttpGet("Uorder")]
        public async Task<IActionResult> UnifiedorderGet([FromQuery] UnifiedorderModel unifiedorder)
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
            var log = _logger.CreateLogger("统一下单");
            if (result.return_code == "FAIL")
            {
                log.LogError($"商家订单号(OutTradeNo):{unifiedorder.OutTradeNo}   通讯标记(return_code):{result.return_code}  {result.return_msg}");
            }
            if (result.result_code == "FAIL")
            {
                log.LogError($"商家订单号(OutTradeNo):{unifiedorder.OutTradeNo}   业务结果(result_code):{result.result_code}\n{result.err_code}:{result.err_code_des}");
            }
            else if (result.result_code == "SUCCESS")
            {
                log.LogInformation($"商家订单号(OutTradeNo):{unifiedorder.OutTradeNo}   业务结果(result_code):{result.result_code}");
            }
            var timeStamp = TenPayV3Util.GetTimestamp();

            var package = string.Format("prepay_id={0}", result.prepay_id);
            var paysign = TenPayV3.GetJsPaySign(TenPyConfigRead.AppId, timeStamp, unifiedorder.NonceStr, package, TenPyConfigRead.Key);

            // return Ok(new { timeStamp, nonceStr, package, paysign });
            return Ok(new
            {
                client = new
                {
                    timeStamp,
                    unifiedorder.NonceStr,
                    package,
                    paysign,
                    sign = "MD5"
                },
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
            orderQuery.SignType = "MD5";
            TenPayV3OrderQueryRequestData datainfo = new TenPayV3OrderQueryRequestData(
                TenPyConfigRead.AppId,
                TenPyConfigRead.MchId,
                orderQuery.TransactionId,
                orderQuery.NonceStr,
                orderQuery.OutTradeNo,
                TenPyConfigRead.Key);
            var result = await TenPayV3.OrderQueryAsync(datainfo);
            var log = _logger.CreateLogger("订单查询");
            if (result.return_code == "FAIL")
            {
                log.LogError($"商家订单号(OutTradeNo):{orderQuery.OutTradeNo}   通讯标记(return_code):{result.return_code}  {result.return_msg}");
            }
            if (result.result_code == "FAIL")
            {
                log.LogError($"商家订单号(OutTradeNo):{orderQuery.OutTradeNo}   业务结果(result_code):{result.result_code}\n{result.err_code}:{result.err_code_des}");
            }
            
            //string openid = res.Element("xml").Element("sign").Value;
            return Ok(new
            {
                respond = result,
                request = orderQuery
            });
        }
        [HttpGet("OrderQuery")]
        public async Task<IActionResult> OrderQueryGet([FromQuery] OrderQueryModel orderQuery)
        {

            if (string.IsNullOrWhiteSpace(orderQuery.NonceStr))
            {
                orderQuery.NonceStr = TenPayV3Util.GetNoncestr();
            }
            if (string.IsNullOrWhiteSpace(orderQuery.OutTradeNo) && string.IsNullOrWhiteSpace(orderQuery.TransactionId))
            {
                return BadRequest("需要OutTradeNo,TransactionId之一");
            }
            orderQuery.SignType = "MD5";
            TenPayV3OrderQueryRequestData datainfo = new TenPayV3OrderQueryRequestData(
                TenPyConfigRead.AppId,
                TenPyConfigRead.MchId,
                orderQuery.TransactionId,
                orderQuery.NonceStr,
                orderQuery.OutTradeNo,
                TenPyConfigRead.Key);
            var result = await TenPayV3.OrderQueryAsync(datainfo);
            var log = _logger.CreateLogger("订单查询");
            if (result.return_code == "FAIL")
            {
                log.LogError($"商家订单号(OutTradeNo):{orderQuery.OutTradeNo}   通讯标记(return_code):{result.return_code}  {result.return_msg}");
            }
            if (result.result_code == "FAIL")
            {
                log.LogError($"商家订单号(OutTradeNo):{orderQuery.OutTradeNo}   业务结果(result_code):{result.result_code}\n{result.err_code}:{result.err_code_des}");
            }

            //string openid = res.Element("xml").Element("sign").Value;
            return Ok(new
            {
                respond = result,
                request = orderQuery
            });
        }

        [HttpPost("CloseOrder")]
        public async Task<IActionResult> CloseOrder(OrderCloseModel orderClose)
        {

            if (string.IsNullOrWhiteSpace(orderClose.NonceStr))
            {
                orderClose.NonceStr = TenPayV3Util.GetNoncestr();
            }
            orderClose.SignType = "MD5";

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
            var log = _logger.CreateLogger("关闭订单");
            if (result.return_code == "FAIL")
            {
                log.LogError($"商家订单号(OutTradeNo):{orderClose.OutTradeNo}   通讯标记(return_code):{result.return_code}  {result.return_msg}");
            }
            if (result.result_code == "FAIL")
            {
                log.LogError($"商家订单号(OutTradeNo):{orderClose.OutTradeNo}   业务结果(result_code):{result.result_code}\n{result.err_code}:{result.err_code_des}");
            }
            else if (result.result_code == "SUCCESS")
            {
                log.LogInformation($"商家订单号(OutTradeNo):{orderClose.OutTradeNo}  业务结果(result_code):{result.result_code}");
            }

            //string openid = res.Element("xml").Element("sign").Value;
            return Ok(new
            {
                respond = result,
                request = orderClose
            });
        }
        [HttpGet("CloseOrder")]
        public async Task<IActionResult> CloseOrderGet([FromQuery] OrderCloseModel orderClose)
        {

            if (string.IsNullOrWhiteSpace(orderClose.NonceStr))
            {
                orderClose.NonceStr = TenPayV3Util.GetNoncestr();
            }

            orderClose.SignType = "MD5";
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
            var log = _logger.CreateLogger("关闭订单");
            if (result.return_code == "FAIL")
            {
                log.LogError($"商家订单号(OutTradeNo):{orderClose.OutTradeNo}   通讯标记(return_code):{result.return_code}  {result.return_msg}");
            }
            if (result.result_code == "FAIL")
            {
                log.LogError($"商家订单号(OutTradeNo):{orderClose.OutTradeNo}   业务结果(result_code):{result.result_code}\n{result.err_code}:{result.err_code_des}");
            }
            else if (result.result_code == "SUCCESS")
            {
                log.LogInformation($"商家订单号(OutTradeNo):{orderClose.OutTradeNo}  业务结果(result_code):{result.result_code}");
            }
            //string openid = res.Element("xml").Element("sign").Value;
            return Ok(new
            {
                respond = result,
                request = orderClose
            });
        }

        [HttpPost("Refund")]
        public async Task<IActionResult> Refund(RefundModel refund)
        {
            try
            {

                
             //   WeixinTrace.SendCustomLog("进入退款流程", "1");
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

                refund.SignType = "MD5";

                //      string outTradeNo = HttpContext.Session.GetString("BillNo");

              //  WeixinTrace.SendCustomLog("进入退款流程", "2 outTradeNo：" + refund.OutTradeNo);


                var dataInfo = new TenPayV3RefundRequestData(
                    appId: TenPyConfigRead.AppId,
                    mchId: TenPyConfigRead.MchId,
                    key: TenPyConfigRead.Key,
                    deviceInfo: refund.DeviceInfo,
                    nonceStr: refund.NonceStr,
                    transactionId: refund.TransactionId,
                    outTradeNo: refund.OutTradeNo,
                    outRefundNo: refund.OutRefundNo,
                    totalFee: refund.TotalFee,
                    refundFee: refund.RefundFee,
                    opUserId: refund.OpUserId,
                    refundAccount: refund.RefundAccount,
                    notifyUrl: refund.NotifyUrl);


                //#region 旧方法
                //var cert = @"D:\cert\apiclient_cert_SenparcRobot.p12";//根据自己的证书位置修改
                //var password = TenPayV3Info.MchId;//默认为商户号，建议修改
                //var result = TenPayV3.Refund(dataInfo, TenPyConfigRead.CertPath, Int32.Parse(TenPyConfigRead.CertSecret));
                //#endregion

                #region 新方法（Senparc.Weixin v6.4.4+）
                var result = await TenPayV3.RefundAsync(_serviceProvider, dataInfo);//证书地址、密码，在配置文件中设置，并在注册微信支付信息时自动记录
                #endregion

                //   WeixinTrace.SendCustomLog("进入退款流程", "3 Result：" + result.ToJson());
                //   ViewData["Message"] = $"退款结果：{result.result_code} {result.err_code_des}。您可以刷新当前页面查看最新结果。";
                var log = _logger.CreateLogger("申请退款");
                if (result.return_code == "FAIL")
                {
                    log.LogError($"退款单号(out_refund_no):{refund.OutRefundNo}   通讯标记(return_code):{result.return_code}  {result.return_msg}");
                }
                if (result.result_code == "FAIL")
                {
                    log.LogError($"退款单号(out_refund_no):{refund.OutRefundNo}   业务结果(result_code):{result.result_code}\n{result.err_code}:{result.err_code_des}");
                }
                else if (result.result_code == "SUCCESS")
                {
                    log.LogInformation($"退款单号(out_refund_no):{refund.OutRefundNo}  业务结果(result_code):{result.result_code}");
                }
                return Ok(new
                {
                    respond = result,
                    request = refund
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
        [HttpGet("Refund")]
        public async Task<IActionResult> RefundGet([FromQuery] RefundModel refund)
        {
            try
            {


              //  WeixinTrace.SendCustomLog("进入退款流程", "1");
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

               // WeixinTrace.SendCustomLog("进入退款流程", "2 outTradeNo：" + refund.OutTradeNo);

                refund.SignType = "MD5";

                var dataInfo = new TenPayV3RefundRequestData(
                    appId: TenPyConfigRead.AppId,
                    mchId: TenPyConfigRead.MchId,
                    key: TenPyConfigRead.Key,
                    deviceInfo: refund.DeviceInfo,
                    nonceStr: refund.NonceStr,
                    transactionId: refund.TransactionId,
                    outTradeNo: refund.OutTradeNo,
                    outRefundNo: refund.OutRefundNo,
                    totalFee: refund.TotalFee,
                    refundFee: refund.RefundFee,
                    opUserId: refund.OpUserId,
                    refundAccount: refund.RefundAccount,
                    notifyUrl: refund.NotifyUrl);


                //#region 旧方法
                //var cert = @"D:\cert\apiclient_cert_SenparcRobot.p12";//根据自己的证书位置修改
                //var password = TenPayV3Info.MchId;//默认为商户号，建议修改
                //var result = TenPayV3.Refund(dataInfo, TenPyConfigRead.CertPath, Int32.Parse(TenPyConfigRead.CertSecret));
                //#endregion

                #region 新方法（Senparc.Weixin v6.4.4+）
                var result = await TenPayV3.RefundAsync(_serviceProvider, dataInfo);//证书地址、密码，在配置文件中设置，并在注册微信支付信息时自动记录
                #endregion
                var log = _logger.CreateLogger("申请退款");
                if (result.return_code == "FAIL")
                {
                    log.LogError($"退款单号(out_refund_no):{refund.OutRefundNo}   通讯标记(return_code):{result.return_code}  {result.return_msg}");
                }
                if (result.result_code == "FAIL")
                {
                    log.LogError($"退款单号(out_refund_no):{refund.OutRefundNo}   业务结果(result_code):{result.result_code}\n{result.err_code}:{result.err_code_des}");
                }
                else if (result.result_code == "SUCCESS")
                {
                    log.LogInformation($"退款单号(out_refund_no):{refund.OutRefundNo}  业务结果(result_code):{result.result_code}");
                }
                // WeixinTrace.SendCustomLog("进入退款流程", "3 Result：" + result.ToJson());
                //   ViewData["Message"] = $"退款结果：{result.result_code} {result.err_code_des}。您可以刷新当前页面查看最新结果。";
                return Ok(new
                {
                    respond = result,
                    request = refund
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
        [HttpPost("RefundQuery")]
        public async Task<IActionResult> RefundQuery(RefundQueryModel refundQuery)
        {
            if (string.IsNullOrWhiteSpace(refundQuery.NonceStr))
            {
                refundQuery.NonceStr = TenPayV3Util.GetNoncestr();
            }
            if (string.IsNullOrWhiteSpace(refundQuery.OutTradeNo)
                && string.IsNullOrWhiteSpace(refundQuery.TransactionId)
                && string.IsNullOrWhiteSpace(refundQuery.OutRefundNo)
                && string.IsNullOrWhiteSpace(refundQuery.RefundId))
            {
                return BadRequest("需要OutTradeNo,TransactionId,OutRefundNo,RefundId之一");
            }

            refundQuery.SignType = "MD5";

            TenPayV3RefundQueryRequestData dataInfo = new TenPayV3RefundQueryRequestData(
                appId: TenPyConfigRead.AppId,
                mchId: TenPyConfigRead.MchId,
                key: TenPyConfigRead.Key,
                nonceStr: refundQuery.NonceStr,
                deviceInfo: refundQuery.DeviceInfo,
                transactionId: refundQuery.TransactionId,
                outTradeNo: refundQuery.OutTradeNo,
                outRefundNo: refundQuery.OutRefundNo,
                refundId: refundQuery.RefundId,
                offset: refundQuery.Offset
                );

            var result = await TenPayV3.RefundQueryAsync(dataInfo);
            var log = _logger.CreateLogger("查询退款");
            if (result.return_code == "FAIL")
            {
                log.LogError($"商家订单号(OutTradeNo):{refundQuery.OutTradeNo}   通讯标记(return_code):{result.return_code}  {result.return_msg}");
            }
            if (result.result_code == "FAIL")
            {
                log.LogError($"商家订单号(OutTradeNo):{refundQuery.OutTradeNo}   业务结果(result_code):{result.result_code}\n{result.err_code}:{result.err_code_des}");
            }
            else if (result.result_code == "SUCCESS")
            {
                log.LogInformation($"商家订单号(OutTradeNo):{refundQuery.OutTradeNo}  业务结果(result_code):{result.result_code}");
            }
            return Ok(new
            {
                respond = result,
                request = refundQuery
            });
        }
        [HttpGet("RefundQuery")]
        public async Task<IActionResult> RefundQueryGet([FromQuery] RefundQueryModel refundQuery)
        {
            if (!string.IsNullOrWhiteSpace(refundQuery.NonceStr))
            {
                refundQuery.NonceStr = TenPayV3Util.GetNoncestr();
            }
            if (string.IsNullOrWhiteSpace(refundQuery.OutTradeNo)
                && string.IsNullOrWhiteSpace(refundQuery.TransactionId)
                && string.IsNullOrWhiteSpace(refundQuery.OutRefundNo)
                && string.IsNullOrWhiteSpace(refundQuery.RefundId))
            {
                return BadRequest("需要OutTradeNo,TransactionId,OutRefundNo,RefundId之一");
            }
            refundQuery.SignType = "MD5";
            TenPayV3RefundQueryRequestData dataInfo = new TenPayV3RefundQueryRequestData(
                appId: TenPyConfigRead.AppId,
                mchId: TenPyConfigRead.MchId,
                key: TenPyConfigRead.Key,
                nonceStr: refundQuery.NonceStr,
                deviceInfo: refundQuery.DeviceInfo,
                transactionId: refundQuery.TransactionId,
                outTradeNo: refundQuery.OutTradeNo,
                outRefundNo: refundQuery.OutRefundNo,
                refundId: refundQuery.RefundId,
                offset: refundQuery.Offset
                );

            var result = await TenPayV3.RefundQueryAsync(dataInfo);
           
            var log = _logger.CreateLogger("查询退款");
            if (result.return_code == "FAIL")
            {
                log.LogError($"商家订单号(OutTradeNo):{refundQuery.OutTradeNo}   通讯标记(return_code):{result.return_code}  {result.return_msg}");
            }
            if (result.result_code == "FAIL")
            {
                log.LogError($"商家订单号(OutTradeNo):{refundQuery.OutTradeNo}   业务结果(result_code):{result.result_code}\n{result.err_code}:{result.err_code_des}");
            }
            else if (result.result_code == "SUCCESS")
            {
                log.LogInformation($"商家订单号(OutTradeNo):{refundQuery.OutTradeNo}  业务结果(result_code):{result.result_code}");
            }
            return Ok(new
            {
                respond = result,
                request = refundQuery
            });
        }

        #region 对账单

        /// <summary>
        /// 下载对账单
        /// </summary>
        /// <param name="date">日期，格式如：20170716</param>
        /// <returns></returns>
        [HttpPost("Dbill")]
        public ActionResult DownloadBill(DownloadBillModel downloadBill)
        {
            if (!Request.IsLocal())
            {
                return Forbid("无权访问！限本地访问");
            }
            if (string.IsNullOrWhiteSpace(downloadBill.NonceStr))
            {
                downloadBill.NonceStr = TenPayV3Util.GetNoncestr();
            }
            if (string.IsNullOrWhiteSpace(downloadBill.BillType))
            {
                downloadBill.BillType = "ALL";
            }
            if (string.IsNullOrWhiteSpace(downloadBill.DeviceInfo))
            {
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
        [HttpGet("Dbill")]
        public ActionResult DownloadBillGet([FromQuery]DownloadBillModel downloadBill)
        {
            //if (!Request.IsLocal())
            //{
            //    return Forbid("无权访问！");
            //}
            if (string.IsNullOrWhiteSpace(downloadBill.NonceStr))
            {
                downloadBill.NonceStr = TenPayV3Util.GetNoncestr();
            }
            if (string.IsNullOrWhiteSpace(downloadBill.BillType))
            {
                downloadBill.BillType = "ALL";
            }
            if (string.IsNullOrWhiteSpace(downloadBill.DeviceInfo))
            {
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
            
            var log = _logger.CreateLogger("关闭订单");
            log.LogInformation(downloadBill.BillDate);
            return Content(result);
        }

        #endregion



        /// <summary>
        /// JS-SDK支付回调地址（在统一下单接口中设置notify_url）
        /// </summary>
        /// <returns></returns>
        [HttpGet("[action]")]
        [HttpPost("[action]")]
        public ActionResult PayNotifyUrl()
        {
            try
            {
                ResponseHandler resHandler = new ResponseHandler(HttpContext);

                string return_code = resHandler.GetParameter("return_code");
                string return_msg = resHandler.GetParameter("return_msg");

                string res = null;

                resHandler.SetKey(TenPyConfigRead.Key);
                //验证请求是否从微信发过来（安全）
                if (resHandler.IsTenpaySign() && return_code.ToUpper() == "SUCCESS")
                {
                    res = "success";//正确的订单处理
                    //直到这里，才能认为交易真正成功了，可以进行数据库操作，但是别忘了返回规定格式的消息！
                }
                else
                {
                    res = "wrong";//错误的订单处理
                }

                

                #region 记录日志

                var logDir = ServerUtility.ContentRootMapPath(string.Format("~/App_Data/TenPayNotify/{0}", SystemTime.Now.ToString("yyyyMMdd")));
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                var logPath = Path.Combine(logDir, string.Format("{0}-{1}-{2}.txt", SystemTime.Now.ToString("yyyyMMdd"), SystemTime.Now.ToString("HHmmss"), Guid.NewGuid().ToString("n").Substring(0, 8)));

                using (var fileStream = System.IO.File.OpenWrite(logPath))
                {
                    var notifyXml = resHandler.ParseXML();
                    //fileStream.Write(Encoding.Default.GetBytes(res), 0, Encoding.Default.GetByteCount(res));

                    fileStream.Write(Encoding.Default.GetBytes(notifyXml), 0, Encoding.Default.GetByteCount(notifyXml));
                    fileStream.Close();
                }

                #endregion


                string xml = string.Format(@"<xml>
<return_code><![CDATA[{0}]]></return_code>
<return_msg><![CDATA[{1}]]></return_msg>
</xml>", return_code, return_msg);
                return Content(xml, "text/xml");
            }
            catch (Exception ex)
            {
                WeixinTrace.WeixinExceptionLog(new WeixinException(ex.Message, ex));
                throw;
            }
        }
    }
}
