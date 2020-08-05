using System.Threading.Tasks;

namespace TencentPay.Services
{
    public interface IWxLogin
    {
        Task<string> SendCodeForOpenId(string appid, string secret, string code, string type = "authorization_code");


    }
}
