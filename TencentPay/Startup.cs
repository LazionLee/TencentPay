using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.CO2NET.AspNet;
using Senparc.Weixin;
using Senparc.Weixin.Entities;
using Senparc.Weixin.RegisterServices;
using Senparc.Weixin.TenPay;
using System;
using TencentPay.Services;

namespace TencentPay
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddHttpClient();
            services.AddMemoryCache();//使用本地缓存必须添加
            services.AddSingleton<ITenPyConfigRead, TenPyConfigRead>();
            services.AddTransient<IWxLogin, WxLogin>();
            services.AddSenparcWeixinServices(Configuration)//Senparc.Weixin 注册（必须）
                                                            //  .AddSenparcWebSocket<CustomNetCoreWebSocketMessageHandler>() //Senparc.WebSocket 注册（按需）  -- DPBMARK WebSocket DPBMARK_END
                    ;
            services.AddSenparcWeixinServices(Configuration)//Senparc.Weixin 注册（必须）
                    ;
            services.AddCertHttpClient(Configuration["SenparcWeixinSetting:TenPayV3_MchId"] + "_", Configuration["SenparcWeixinSetting:TenPayV3_MchId"] + "", Configuration["SenparcWeixinSetting:TenPayV3_CertPath"] + "");

            //services.AddCertHttpClient("name", "pwd", "path");//此处可以添加更多 Cert 证书
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IOptions<SenparcSetting> senparcSetting, IOptions<SenparcWeixinSetting> senparcWeixinSetting)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();
            // 启动 CO2NET 全局注册，必须！
            // 关于 UseSenparcGlobal() 的更多用法见 CO2NET Demo：https://github.com/Senparc/Senparc.CO2NET/blob/master/Sample/Senparc.CO2NET.Sample.netcore3/Startup.cs
            var registerService = app.UseSenparcGlobal(env, senparcSetting.Value, globalRegister =>
            {
                #region CO2NET 全局配置

                #region 全局缓存配置（按需）

                //当同一个分布式缓存同时服务于多个网站（应用程序池）时，可以使用命名空间将其隔离（非必须）
                //   globalRegister.ChangeDefaultCacheNamespace("DefaultCO2NETCache");



                #endregion

                #region 注册日志（按需，建议）

                globalRegister.RegisterTraceLog(ConfigTraceLog);//配置TraceLog

                #endregion

                #region APM 系统运行状态统计记录配置

                //测试APM缓存过期时间（默认情况下可以不用设置）
                Senparc.CO2NET.APM.Config.EnableAPM = true;//默认已经为开启，如果需要关闭，则设置为 false
                Senparc.CO2NET.APM.Config.DataExpire = TimeSpan.FromMinutes(60);

                #endregion

                #endregion
            }, true)
                //使用 Senparc.Weixin SDK
                .UseSenparcWeixin(senparcWeixinSetting.Value, weixinRegister =>
                 {
                    #region 微信相关配置

                    /* 微信配置开始
                    * 
                    * 建议按照以下顺序进行注册，尤其须将缓存放在第一位！
                    */

                    #region 微信缓存（按需，必须放在配置开头，以确保其他可能依赖到缓存的注册过程使用正确的配置）
                    //注意：如果使用非本地缓存，而不执行本块注册代码，将会收到“当前扩展缓存策略没有进行注册”的异常



                    #endregion

                    #region 注册公众号或小程序（按需）

                    weixinRegister




                            //除此以外，仍然可以在程序任意地方注册公众号或小程序：
                            //AccessTokenContainer.Register(appId, appSecret, name);//命名空间：Senparc.Weixin.MP.Containers
                    #endregion


                    #region 注册微信支付（按需）        -- DPBMARK TenPay

                            //注册旧微信支付版本（V2）（可注册多个）
                            //  .RegisterTenpayOld(senparcWeixinSetting.Value, "【盛派网络小助手】公众号")//这里的 name 和第一个 RegisterMpAccount() 中的一致，会被记录到同一个 SenparcWeixinSettingItem 对象中

                            //注册最新微信支付版本（V3）（可注册多个）
                            .RegisterTenpayV3(senparcWeixinSetting.Value, "【盛派网络小助手】公众号")//记录到同一个 SenparcWeixinSettingItem 对象中
                        /* 特别注意：
                         * 在 services.AddSenparcWeixinServices() 代码中，已经自动为当前的 
                         * senparcWeixinSetting  对应的TenpayV3 配置进行了 Cert 证书配置，
                         * 如果此处注册的微信支付信息和默认 senparcWeixinSetting 信息不同，
                         * 请在 ConfigureServices() 方法中使用 services.AddCertHttpClient() 
                         * 添加对应证书。
                         */

                    #endregion                          // DPBMARK_END


                        ;

                    /* 微信配置结束 */

                    #endregion
                });

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
        /// <summary>
        /// 配置微信跟踪日志（演示，按需）
        /// </summary>
        private void ConfigTraceLog()
        {
            //这里设为Debug状态时，/App_Data/WeixinTraceLog/目录下会生成日志文件记录所有的API请求日志，正式发布版本建议关闭

            //如果全局的IsDebug（Senparc.CO2NET.Config.IsDebug）为false，此处可以单独设置true，否则自动为true
            Senparc.CO2NET.Trace.SenparcTrace.SendCustomLog("系统日志", "系统启动");//只在Senparc.Weixin.Config.IsDebug = true的情况下生效

            //全局自定义日志记录回调
            Senparc.CO2NET.Trace.SenparcTrace.OnLogFunc = () =>
            {
                //加入每次触发Log后需要执行的代码
            };

            //当发生基于WeixinException的异常时触发
            WeixinTrace.OnWeixinExceptionFunc = async ex =>
            {
                //加入每次触发WeixinExceptionLog后需要执行的代码


            };
        }
    }
}
