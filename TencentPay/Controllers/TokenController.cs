using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Senparc.Weixin.MP;
using Senparc.Weixin.TenPay.V3;
using Senparc.Weixin.WxOpen.Entities.Request;
using TencentPay.Services;

namespace TencentPay.Controllers
{
    [Route("api")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        public static readonly string Token;//与微信公众账号后台的Token设置保持一致，区分大小写。
        public static readonly string EncodingAESKey;//与微信公众账号后台的EncodingAESKey设置保持一致，区分大小写。
        public static readonly string AppId;//与微信公众账号后台的AppId设置保持一致，区分大小写。
        private readonly IConfiguration _configuration;
        private readonly ITenPyConfigRead tenPyConfigRead;

        public TokenController(IConfiguration configuration,ITenPyConfigRead tenPyConfigRead)
        {
            _configuration = configuration;
            this.tenPyConfigRead = tenPyConfigRead;
        }
        [HttpGet]
        public IActionResult Index([FromQuery]string prepay_id)
        {
            var timeStamp = TenPayV3Util.GetTimestamp();
            var nonceStr = TenPayV3Util.GetNoncestr();
            var package = string.Format("prepay_id={0}", prepay_id);
            var paysign = TenPayV3.GetJsPaySign("wx40a04481d2e12c20", timeStamp, nonceStr, package, tenPyConfigRead.Key);

            return Ok(new { timeStamp, nonceStr, package, paysign });
        }

    }
}
