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
                    Console.WriteLine("�������ã�appsettings.json��");
                    
                    Console.WriteLine($"1.�豸��(DeviceInfo):{configuration["DeviceInfo"]}");
                    Console.WriteLine($"2.С����ID(TenPayV3_AppId):{configuration["SenparcWeixinSetting:TenPayV3_AppId"]}");
                    Console.WriteLine($"3.С�����ܳ�(AppSecret):{configuration["SenparcWeixinSetting:TenPayV3_AppSecret"]}");
                    Console.WriteLine($"4.�̻���(TenPayV3_MchId):{configuration["SenparcWeixinSetting:TenPayV3_MchId"]}");
                    Console.WriteLine($"5.�̻�֧����Կ(TenPayV3_Key):{configuration["SenparcWeixinSetting:TenPayV3_Key"]}");
                    Console.WriteLine($"6.΢��֧��֤��λ�ã�����·����:TenPayV3_CertPath:{configuration["SenparcWeixinSetting:TenPayV3_CertPath"]}");
                    Console.WriteLine($"7.΢��֧��֤������(CertSecret):{configuration["SenparcWeixinSetting:TenPayV3_CertSecret"]}");
                   // Console.WriteLine($"TenPayV3_TenpayNotify:{configuration["SenparcWeixinSetting:TenPayV3_TenpayNotify"]}");
                    Console.WriteLine($"8.С����֧����ɺ�Ļص�����ҳ��(����)(TenPayV3_WxOpenTenpayNotify):{configuration["SenparcWeixinSetting:TenPayV3_WxOpenTenpayNotify"]}");
                    Console.WriteLine($"9.(С�����˿���ɺ�Ļص�����ҳ�棬�Ǳ���)TenPayV3_WxOpenTenpayRefundNotify:{configuration["SenparcWeixinSetting:TenPayV3_WxOpenTenpayRefundNotify"]}");
                   
                   
                       
                       
                    


                    // Use the context here
                }
                catch (Exception ex)
                {
                    Console.WriteLine("����������ڴ���");
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
