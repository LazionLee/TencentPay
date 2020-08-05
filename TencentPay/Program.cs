using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace TencentPay
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
           


            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;

                try
                {
                    var configuration = services.GetRequiredService<IConfiguration>();
                    Console.WriteLine("载入配置（appsettings.json）");
                    
                    Console.WriteLine($"1.设备号(DeviceInfo):{configuration["DeviceInfo"]}");
                    Console.WriteLine($"2.小程序ID(TenPayV3_AppId):{configuration["SenparcWeixinSetting:TenPayV3_AppId"]}");
                    Console.WriteLine($"3.小程序密匙(AppSecret):{configuration["SenparcWeixinSetting:TenPayV3_AppSecret"]}");
                    Console.WriteLine($"4.商户号(TenPayV3_MchId):{configuration["SenparcWeixinSetting:TenPayV3_MchId"]}");
                    Console.WriteLine($"5.商户支付密钥(TenPayV3_Key):{configuration["SenparcWeixinSetting:TenPayV3_Key"]}");
                    Console.WriteLine($"6.微信支付证书位置（物理路径）:TenPayV3_CertPath:{configuration["SenparcWeixinSetting:TenPayV3_CertPath"]}");
                    Console.WriteLine($"7.微信支付证书密码(CertSecret):{configuration["SenparcWeixinSetting:TenPayV3_CertSecret"]}");
                   // Console.WriteLine($"TenPayV3_TenpayNotify:{configuration["SenparcWeixinSetting:TenPayV3_TenpayNotify"]}");
                    Console.WriteLine($"8.小程序支付完成后的回调处理页面(必须)(TenPayV3_WxOpenTenpayNotify):{configuration["SenparcWeixinSetting:TenPayV3_WxOpenTenpayNotify"]}");
                    Console.WriteLine($"9.(小程序退款完成后的回调处理页面，非必须)TenPayV3_WxOpenTenpayRefundNotify:{configuration["SenparcWeixinSetting:TenPayV3_WxOpenTenpayRefundNotify"]}");
                   
                   
                       
                       
                    


                    // Use the context here
                }
                catch (Exception ex)
                {
                    Console.WriteLine("配置载入存在错误");
                    Console.WriteLine(ex);

                }
            }


            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)

                .ConfigureWebHostDefaults(webBuilder =>
                {
                    
                    
                    webBuilder
                     .UseStartup<Startup>();

                    
                });
    }
}
