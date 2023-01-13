using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using KeklandBankSystem.Controllers;
using KeklandBankSystem.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VkNet;
using VkNet.Abstractions;
using VkNet.Model;

namespace KeklandBankSystem
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        //public static bool hasStart = true;

        [Obsolete]
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(builder =>
                builder
                    .AddDebug()
                    .AddConsole()
                    .AddConfiguration(Configuration.GetSection("Logging"))
                    .SetMinimumLevel(LogLevel.Information)
            );

            services.AddControllersWithViews();
            services.AddEntityFrameworkNpgsql()
                .AddDbContext<BankContext>(options =>
                    options.UseNpgsql(Environment.GetEnvironmentVariable("API_ConnectionString"))
                );

            // Создаем новый Scope и мигрируем базу данных в нём
            var serviceProvider = services.BuildServiceProvider();
            var context = serviceProvider.GetRequiredService<BankContext>();
            var seedService = new SeedMigrateDataBaseService(context);
            if (context.Database.GetPendingMigrations().Any())
                context.Database.Migrate();

            var seedServiceStatus = seedService.MigrateSeedDataBase().IsCompleted;

            if(seedServiceStatus == true)
            {
                Console.WriteLine("Seed Service Complete");
            }

            services.AddHangfire(config =>
                config.UsePostgreSqlStorage(Environment.GetEnvironmentVariable("API_ConnectionString")));

            services.AddHangfireServer();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IBankServices, BankServices>();

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = new PathString("/user/login");
                options.AccessDeniedPath = new PathString("/user/denied");
                options.LogoutPath = new PathString("/user/logout");
                options.ExpireTimeSpan = TimeSpan.FromDays(360);
            });

            services.AddMvc(options =>
            {
                options.Filters.Add(typeof(AdminFilterIdenty));
            });

            services.AddSession();
        }

        public IConfiguration Configuration { get; }

        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env, IServiceProvider serviceProvider)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });


            if (Environment.GetEnvironmentVariable("API_Environment") != "Publish")
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStatusCodePages();

            app.UseHangfireDashboard(Environment.GetEnvironmentVariable("API_HangFireUrling"), new DashboardOptions
            {
                Authorization = new[] { new HanfFireAuthFilter() }
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseStatusCodePagesWithRedirects("/error/{0}");

            app.UseSession();

            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Bank}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
                endpoints.MapHangfireDashboard();
            });

            var manager = new RecurringJobManager();
            manager.AddOrUpdate<IBankServices>("1", (IBankServices _service) => _service.InfluenceOrganization(), Cron.DayInterval(3));
            manager.AddOrUpdate<IBankServices>("2", (IBankServices _service) => _service.GetNalogs(), Cron.DayInterval(5));
            manager.AddOrUpdate<IBankServices>("3", (IBankServices _service) => _service.AddStatistics(), Cron.DayInterval(1));
            manager.AddOrUpdate<IBankServices>("4", (IBankServices _service) => _service.AddDeposit(), Cron.DayInterval(1));
            manager.AddOrUpdate<IBankServices>("5", (IBankServices _service) => _service.PayDay(), Cron.DayInterval(3));
            manager.AddOrUpdate<IBankServices>("6", (IBankServices _service) => _service.UpdateTopUser(), Cron.HourInterval(1));
            manager.AddOrUpdate<IBankServices>("7", (IBankServices _service) => _service.AddItemStatistics(), Cron.DayInterval(3));
            manager.AddOrUpdate<IBankServices>("8", (IBankServices _service) => _service.UpdateItemTopUser(), Cron.HourInterval(1));
            manager.AddOrUpdate<IBankServices>("9", (IBankServices _service) => _service.AddPremiumMoney(), Cron.DayInterval(1));
            manager.AddOrUpdate<IBankServices>("10", (IBankServices _service) => _service.PremiumUpdate(), Cron.DayInterval(1));
            manager.AddOrUpdate<IBankServices>("11", (IBankServices _service) => _service.WeithMunis(), Cron.DayInterval(7));
            manager.AddOrUpdate<IBankServices>("12", (IBankServices _service) => _service.GovermentCompleteBalance(), Cron.HourInterval(1));
            manager.AddOrUpdate<IBankServices>("13", (IBankServices _service) => _service.GovermentTaxesDayIncrease(), Cron.DayInterval(1));
            manager.AddOrUpdate<IBankServices>("14", (IBankServices _service) => _service.DeleteImage(), Cron.DayInterval(1));
            manager.AddOrUpdate<IBankServices>("15", (IBankServices _service) => _service.PrepareAllMoney(), Cron.DayInterval(1));
            manager.AddOrUpdate<IBankServices>("16", (IBankServices _service) => _service.RefreshAds(), Cron.HourInterval(12));
            manager.AddOrUpdate<IBankServices>("17", (IBankServices _service) => _service.NewsDays(), Cron.DayInterval(1));
            manager.AddOrUpdate<IBankServices>("18", (IBankServices _service) => _service.FixBug(), Cron.DayInterval(1));
        }
    }

    public static class ImageValidator
    {
        public const int ImageMinimumBytes = 512;

        public static bool IsImage(this IFormFile postedFile)
        {
            //-------------------------------------------
            //  Типы
            //-------------------------------------------
            if (postedFile.ContentType.ToLower() != "image/jpg" &&
                        postedFile.ContentType.ToLower() != "image/jpeg" &&
                        postedFile.ContentType.ToLower() != "image/pjpeg" &&
                        postedFile.ContentType.ToLower() != "image/gif" &&
                        postedFile.ContentType.ToLower() != "image/x-png" &&
                        postedFile.ContentType.ToLower() != "image/png" &&
                        postedFile.ContentType.ToLower() != "image/webp")
            {
                return false;
            }

            //-------------------------------------------
            //  Тоже типы
            //-------------------------------------------
            if (Path.GetExtension(postedFile.FileName).ToLower() != ".jpg"
                && Path.GetExtension(postedFile.FileName).ToLower() != ".png"
                && Path.GetExtension(postedFile.FileName).ToLower() != ".gif"
                && Path.GetExtension(postedFile.FileName).ToLower() != ".jpeg"
                && Path.GetExtension(postedFile.FileName).ToLower() != ".webp")
            {
                return false;
            }

            try
            {
                if (!postedFile.OpenReadStream().CanRead)
                {
                    return false;
                }

                if (postedFile.Length < ImageMinimumBytes)
                {
                    return false;
                }

                var buffer = new byte[ImageMinimumBytes];
                postedFile.OpenReadStream().Read(buffer, 0, ImageMinimumBytes);
                var content = System.Text.Encoding.UTF8.GetString(buffer);
                if (Regex.IsMatch(content, @"<script|<html|<head|<title|<body|<pre|<table|<a\s+href|<img|<plaintext|<cross\-domain\-policy",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline))
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }

    [Serializable]
    public class Updates
    {
        /// <summary>
        /// Тип события
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Объект, инициировавший событие
        /// Структура объекта зависит от типа уведомления
        /// </summary>
        [JsonProperty("object")]
        public JObject Object { get; set; }

        /// <summary>
        /// ID сообщества, в котором произошло событие
        /// </summary>
        [JsonProperty("group_id")]
        public long GroupId { get; set; }

        [JsonProperty("secret")]
        public string Secret { get; set; }
    }

    public class HanfFireAuthFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return true;
        }
    }


    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class AdminFilterIdenty : Attribute, IAsyncAuthorizationFilter
    {
        public AdminFilterIdenty()
        {
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var name = (string)context.RouteData.Values["Action"];
            var _bankServices = (IBankServices)context.HttpContext.RequestServices.GetService(typeof(IBankServices));

            var gov = await _bankServices.GetGoverment();

            var userPrincipal = context.HttpContext.User;

            var user = await _bankServices.FindByNameAsync(userPrincipal.Identity.Name);

            if (user != null)
            {
                await _bankServices.AddUniqView(user);
            }

            if (Environment.GetEnvironmentVariable("API_Environment") == "Test")
            {
                if (userPrincipal != null && user != null)
                {
                    var userIsTest = await _bankServices.UserIsInRole(user, "Tester") || await _bankServices.UserIsInRole(user, "Administrator") || await _bankServices.UserIsInRole(user, "Owner");
                    if (!userIsTest)
                    {
                        if (!(name == "Error"))
                        {
                            context.Result = new RedirectToActionResult("Error", "Bank", new { code = 1900 });
                            return;
                        }
                    }
                }
                else
                {
                    if (!(name == "Login" || name == "Error"))
                    {
                        context.Result = new RedirectToActionResult("Error", "Bank", new { code = 1900 });
                        return;
                    }
                }
            }
            else
            {
                if (gov.SiteIsOn)
                {
                    if (userPrincipal != null)
                    {
                        if (user != null)
                        {
                            if (user.VKUniqId == 0)
                            {
                                if (name != "Error" && name != "EditUrlVkNew")
                                    context.Result = new RedirectToActionResult("Error", "Bank", new { code = 1500 });
                            }

                            if (!user.IsArrested)
                                return;

                            if (name != "Error")
                                context.Result = new RedirectToActionResult("Error", "Bank", new { code = 1200 });
                            return;
                        }

                    }
                }
                else
                {
                    if (userPrincipal != null)
                    {
                        if (user != null)
                        {
                            var userIsOwner = await _bankServices.UserIsInRole(user, "Owner");
                            if (userIsOwner)
                                return;
                        }

                    }

                    if (name != "Error")
                        context.Result = new RedirectToActionResult("Error", "Bank", new { code = 1201 });
                    return;
                }
            }

            //if (name == "Error") return;

            //var gov = await _bankServices.GetGoverment();
            //if (!gov.SiteIsOn)
            //{
            //context.Result = new RedirectToActionResult("Error", "Bank", new { code = 1200 });
            //}

            //if (!user.Identity.IsAuthenticated)
            //{
            // return RedirectToAction("AccessDenied", "Account", new { area = "User" });
            //  return;
            //}

            // you can also use registered services
            //var someService = context.HttpContext.RequestServices.GetService<ISomeService>();

            //var isAuthorized = someService.IsUserAuthorized(user.Identity.Name, _someFilterParameter);
            //if (!isAuthorized)
            //{
            //  context.Result = new StatusCodeResult((int)System.Net.HttpStatusCode.Forbidden);
            //return;
            //}
        }
    }

    public static class IntTransform
    {
        public static string ConvertInt(int num)
        {
            try
            {
                var pre = "<span title=" + num + ">";
                var post = "</span>";

                string ret;

                var d = "";
                if (num < 0)
                    d = "-";

                num = Math.Abs(num);

                if (num < 1000) // < 1k
                {
                    ret = d + num.ToString();
                    return pre + ret + post;
                }

                if (num < 1000000) // > 1k - 1M
                {
                    if (num < 750000)
                    {
                        ret = d + ((int)(num / 1000)).ToString() + "." + ((int)(num % 1000)).ToString().Substring(0, 2) + "K";
                    }
                    else
                    {
                        ret = d + ((Convert.ToDouble(num) / 1000000)).ToString().Substring(0, 4) + "M";
                    }
                }
                else ret = d + ((int)(num / 1000000)).ToString() + "." + ((int)(num % 1000000)).ToString().Substring(0, 2) + "M";

                return pre + ret + post;
            }
            catch (Exception)
            {
                var pre = "<span title=" + num + ">";
                var post = "</span>";
                var d = "";
                if (num < 0)
                    d = "-";

                num = Math.Abs(num);


                string ret;
                if (num < 1000) // < 1k
                {
                    ret = d + num.ToString();
                    return pre + ret + post;
                }

                if (num < 1000000) // > 1k - 1M
                {
                    if (num < 750000)
                    {
                        ret = d + ((int)(num / 1000)).ToString() + "K";
                    }
                    else
                    {
                        ret = d + ((Convert.ToDouble(num) / 1000000)).ToString() + "M";
                    }
                }
                else ret = d + ((int)(num / 1000000)).ToString() + "M";

                return pre + ret + post;
            }
        }
    }

    public class DateTimeTransform
    {
        public static string ConvertDateTime(long ticks)
        {
            var dateTime = new DateTime(ticks);

            const int SECOND = 1;
            const int MINUTE = 60 * SECOND;
            const int HOUR = 60 * MINUTE;
            const int DAY = 24 * HOUR;
            const int MONTH = 30 * DAY;

            var ts = new TimeSpan(DateTime.Now.Ticks - dateTime.Ticks);
            var delta = Math.Abs(ts.TotalSeconds);

            if (delta is >= 0 and <= 5)
            {
                return "Только что";
            }

            if (delta < 1 * MINUTE)
            {
                if ((ts.Seconds % 10) == 1) return ts.Seconds + " секунду назад";
                if ((ts.Seconds % 10) >= 2 && (ts.Seconds % 10) <= 4) return ts.Seconds + " секунды назад";
                return ts.Seconds + " секунд назад";
            }

            if (delta < 2 * MINUTE)
                return "Минуту назад";

            if (delta < 59 * MINUTE)
            {
                if ((ts.Minutes % 10) == 1) return ts.Minutes + " минуту назад";
                if ((ts.Minutes % 10) >= 2 && (ts.Minutes % 10) <= 4) return ts.Minutes + " минуты назад";
                return ts.Minutes + " минут назад";
            }

            if (delta < 95 * MINUTE)
                return "Час назад";

            if (delta < 24 * HOUR)
            {
                if (Math.Abs(ts.Hours) == 21) return Math.Abs(ts.Hours) + " час назад";
                if ((Math.Abs(ts.Hours) % 10) >= 2 && (Math.Abs(ts.Hours) % 10) <= 4) return Math.Abs(ts.Hours) + " часа назад";
                return Math.Abs(ts.Hours) + " часов назад";
            }

            if (delta < 48 * HOUR)
                return "Вчера";

            if (delta < 72 * HOUR)
                return "Позавчера";

            if (delta < 30 * DAY)
            {
                if (ts.Days == 11 || ts.Days == 12 || ts.Days == 13 || ts.Days == 14) return ts.Days + " дней назад";
                if (ts.Days == 21) return "21 день назад";
                if ((ts.Days % 10) >= 2 && (ts.Days % 10) <= 4) return ts.Days + " дня назад";

                return ts.Days + " дней назад";
            }

            if (delta < 12 * MONTH)
            {
                var months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                if (months == 1) return "Месяц назад";
                if (months is >= 2 and <= 4) return months + " месяца назад";
                return months + " месяцев назад";
            }

            var years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
            if (years == 11 || years == 12 || years == 13 || years == 14) return years + " лет назад";
            if ((years % 10) == 1) return years + " год назад";
            if ((years % 10) >= 2 && (years % 10) <= 4) return years + " года назад";

            return years + " лет назад";
        }
    }
}
