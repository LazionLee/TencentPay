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
            services.AddMemoryCache();//ʹ�ñ��ػ���������
            services.AddSingleton<ITenPyConfigRead, TenPyConfigRead>();
            services.AddTransient<IWxLogin, WxLogin>();
            services.AddSenparcWeixinServices(Configuration)//Senparc.Weixin ע�ᣨ���룩
                                                            //  .AddSenparcWebSocket<CustomNetCoreWebSocketMessageHandler>() //Senparc.WebSocket ע�ᣨ���裩  -- DPBMARK WebSocket DPBMARK_END
                    ;
            services.AddSenparcWeixinServices(Configuration)//Senparc.Weixin ע�ᣨ���룩
                    ;
            services.AddCertHttpClient(Configuration["SenparcWeixinSetting:TenPayV3_MchId"] + "_", Configuration["SenparcWeixinSetting:TenPayV3_MchId"] + "", Configuration["SenparcWeixinSetting:TenPayV3_CertPath"] + "");

            //services.AddCertHttpClient("name", "pwd", "path");//�˴�������Ӹ��� Cert ֤��
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
            // ���� CO2NET ȫ��ע�ᣬ���룡
            // ���� UseSenparcGlobal() �ĸ����÷��� CO2NET Demo��https://github.com/Senparc/Senparc.CO2NET/blob/master/Sample/Senparc.CO2NET.Sample.netcore3/Startup.cs
            var registerService = app.UseSenparcGlobal(env, senparcSetting.Value, globalRegister =>
            {
                #region CO2NET ȫ������

                #region ȫ�ֻ������ã����裩

                //��ͬһ���ֲ�ʽ����ͬʱ�����ڶ����վ��Ӧ�ó���أ�ʱ������ʹ�������ռ佫����루�Ǳ��룩
                //   globalRegister.ChangeDefaultCacheNamespace("DefaultCO2NETCache");



                #endregion

                #region ע����־�����裬���飩

                globalRegister.RegisterTraceLog(ConfigTraceLog);//����TraceLog

                #endregion

                #region APM ϵͳ����״̬ͳ�Ƽ�¼����

                //����APM�������ʱ�䣨Ĭ������¿��Բ������ã�
                Senparc.CO2NET.APM.Config.EnableAPM = true;//Ĭ���Ѿ�Ϊ�����������Ҫ�رգ�������Ϊ false
                Senparc.CO2NET.APM.Config.DataExpire = TimeSpan.FromMinutes(60);

                #endregion

                #endregion
            }, true)
                //ʹ�� Senparc.Weixin SDK
                .UseSenparcWeixin(senparcWeixinSetting.Value, weixinRegister =>
                 {
                    #region ΢���������

                    /* ΢�����ÿ�ʼ
                    * 
                    * ���鰴������˳�����ע�ᣬ�����뽫������ڵ�һλ��
                    */

                    #region ΢�Ż��棨���裬����������ÿ�ͷ����ȷ���������������������ע�����ʹ����ȷ�����ã�
                    //ע�⣺���ʹ�÷Ǳ��ػ��棬����ִ�б���ע����룬�����յ�����ǰ��չ�������û�н���ע�ᡱ���쳣



                    #endregion

                    #region ע�ṫ�ںŻ�С���򣨰��裩

                    weixinRegister




                            //�������⣬��Ȼ�����ڳ�������ط�ע�ṫ�ںŻ�С����
                            //AccessTokenContainer.Register(appId, appSecret, name);//�����ռ䣺Senparc.Weixin.MP.Containers
                    #endregion


                    #region ע��΢��֧�������裩        -- DPBMARK TenPay

                            //ע���΢��֧���汾��V2������ע������
                            //  .RegisterTenpayOld(senparcWeixinSetting.Value, "��ʢ������С���֡����ں�")//����� name �͵�һ�� RegisterMpAccount() �е�һ�£��ᱻ��¼��ͬһ�� SenparcWeixinSettingItem ������

                            //ע������΢��֧���汾��V3������ע������
                            .RegisterTenpayV3(senparcWeixinSetting.Value, "��ʢ������С���֡����ں�")//��¼��ͬһ�� SenparcWeixinSettingItem ������
                        /* �ر�ע�⣺
                         * �� services.AddSenparcWeixinServices() �����У��Ѿ��Զ�Ϊ��ǰ�� 
                         * senparcWeixinSetting  ��Ӧ��TenpayV3 ���ý����� Cert ֤�����ã�
                         * ����˴�ע���΢��֧����Ϣ��Ĭ�� senparcWeixinSetting ��Ϣ��ͬ��
                         * ���� ConfigureServices() ������ʹ�� services.AddCertHttpClient() 
                         * ��Ӷ�Ӧ֤�顣
                         */

                    #endregion                          // DPBMARK_END


                        ;

                    /* ΢�����ý��� */

                    #endregion
                });

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
        /// <summary>
        /// ����΢�Ÿ�����־����ʾ�����裩
        /// </summary>
        private void ConfigTraceLog()
        {
            //������ΪDebug״̬ʱ��/App_Data/WeixinTraceLog/Ŀ¼�»�������־�ļ���¼���е�API������־����ʽ�����汾����ر�

            //���ȫ�ֵ�IsDebug��Senparc.CO2NET.Config.IsDebug��Ϊfalse���˴����Ե�������true�������Զ�Ϊtrue
            Senparc.CO2NET.Trace.SenparcTrace.SendCustomLog("ϵͳ��־", "ϵͳ����");//ֻ��Senparc.Weixin.Config.IsDebug = true���������Ч

            //ȫ���Զ�����־��¼�ص�
            Senparc.CO2NET.Trace.SenparcTrace.OnLogFunc = () =>
            {
                //����ÿ�δ���Log����Ҫִ�еĴ���
            };

            //����������WeixinException���쳣ʱ����
            WeixinTrace.OnWeixinExceptionFunc = async ex =>
            {
                //����ÿ�δ���WeixinExceptionLog����Ҫִ�еĴ���


            };
        }
    }
}
