using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TencentPay.Models;
using TencentPay.Services;

namespace TencentPay.Services
{
    public  class WxLogin:IWxLogin
    {
        private readonly IHttpClientFactory _clientFactory;

        public WxLogin(IHttpClientFactory httpClientFactory)
        {
            _clientFactory = httpClientFactory;
        }

        public  async Task<string> SendCodeForOpenId(string appid,string secret,string code,string type = "authorization_code")
        {

            string url = $"https://api.weixin.qq.com/sns/jscode2session?appid={appid}&secret={secret}&js_code={code}&grant_type=authorization_code";
            var client = _clientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get,
            url);
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                 return await response.Content.ReadAsStringAsync();
               // code2SessionReturnModel returnModel = JsonSerializer.Deserialize<code2SessionReturnModel>(responsestring);
                
                //Branches = await JsonSerializer.DeserializeAsync
                //    <IEnumerable<GitHubBranch>>(responseStream);
            }
            return null;

        }
    }
}
