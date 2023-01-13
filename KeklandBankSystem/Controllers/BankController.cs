using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CryptoHelper;
using KeklandBankSystem.Infrastructure;
using KeklandBankSystem.Model;
using KeklandBankSystem.Model.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using VkNet.Abstractions;

namespace KeklandBankSystem.Controllers
{
    public class BankController : Controller
    {
        public BankController(IBankServices bnksrvc, IHostingEnvironment appEnvironment)
        {
            _bankServices = bnksrvc;
            _appEnvironment = appEnvironment;

            _configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();
        }

        private IBankServices _bankServices;
        private IHostingEnvironment _appEnvironment;
        private IConfiguration _configuration;


        [HttpGet()]
        public async Task<IActionResult> Index(int? page) // Главная страница, информация и ссылка на регистрацию - всё. ( уже нет )
        {
            var govs = (await _bankServices.GetAllGoverments()).OrderByDescending(m => m.AllOrganizationBalance).ToList();

            var itemOnPage = 15;
            var maxPage = (govs.Count() / itemOnPage) + ((govs.Count() % itemOnPage) > 0 ? 1 : 0);

            if (page == null || page < 1 || page > maxPage)
            {
                page = 1;
            }

            ViewBag.CurrectPage = page;
            ViewBag.CountPage = maxPage;

            var model = new IndexPanelForms
            {
                allGovs = govs.Skip(((int)page - 1) * itemOnPage).Take(itemOnPage).ToList(),
                LastStatistics = await _bankServices.GetLastStatistic(),
                BigAds = BankServices.MegaAdd,
                SmallAds = BankServices.MiniAdd,
                LastArticles = await _bankServices.GetLastArticles(),
                BugUrl = BankServices.MegaAddUrl,
                SmallUrl = BankServices.MiniAddUrl
            };

            return View(model);
        }

        [HttpGet("create_article")]
        public async Task<IActionResult> CreateArticle()
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);
            if (perm.CreateArticles)
                return View(new ArticlesModel());

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("create_article")]
        public async Task<IActionResult> CreateArticle(ArticlesModel model)
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);
            if (!perm.CreateArticles) 
                return RedirectToAction("Error", "Bank", new { code = 700 });

            var article = new Articles
            {
                Date = _bankServices.NowDateTime(),
                HtmlText = model.HtmlText,
                MiniText = model.MiniText
            };

            if (model.ImageUrl != null || !string.IsNullOrEmpty(model.ImageUrlString))
            {
                if (model.ImageUrl != null)
                {
                    if (model.ImageUrl.Length > 10485760) // 10mb
                    {
                        ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный размер.");
                        return View(model);
                    }

                    if (!model.ImageUrl.IsImage())
                    {
                        ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный формат.");
                        return View(model);
                    }

                    var img = await _bankServices.CreateImageSys(_appEnvironment, model.ImageUrl, "userImagesStatic", -1);

                    article.ImageUrl = img.ScreePath;
                }
                else article.ImageUrl = model.ImageUrlString;
            }
            else
            {
                ModelState.AddModelError("", "Вы не загрузили картинку.");
                return View(model);
            }

            await _bankServices.CreateArticles(article);

            return RedirectToAction("SingleArticle", "Bank", new { id = article.Id });

        }

        [HttpGet("article/{id}")]
        public async Task<IActionResult> SingleArticle(int id)
        {
            var article = await _bankServices.GetArticles(id);

            if (article != null)
                return View(article);

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        [HttpGet("check_news")]
        [Authorize]
        public async Task<IActionResult> CheckNews()
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (!perm.CheckNews) 
                return RedirectToAction("Error", "Bank", new { code = 700 });

            var model = (await _bankServices.GetAllNews()).Where(m => m.Type == "status_timing").FirstOrDefault();

            if (model != null)
            {
                return View(new CheckNewsModel
                {
                    MiniInformation = model.MiniInformation,
                    Name = model.Name,
                    Url = model.Url,
                    Rare = 0,
                    Id = model.Id
                });
            }

            return View(null);

        }

        [HttpPost("check_news")]
        [Authorize]
        public async Task<IActionResult> CheckNews(CheckNewsModel model)
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (!perm.CheckNews) 
                return RedirectToAction("Error", "Bank", new { code = 700 });

            if (model.isOk)
            {
                if (model.Rare < 1 || model.Rare > 4)
                {
                    ModelState.AddModelError("", "Неверная популярность ( от 1 до 4 )");
                    return View(model);
                }

                var news = await _bankServices.GetNews(model.Id);

                news.MiniInformation = model.MiniInformation;
                news.Name = model.Name;
                news.Url = model.Url;
                news.Type = "status_ok";

                switch (model.Rare)
                {
                    case 1: news.ShowsDays = 2; break;
                    case 2: news.ShowsDays = 5; break;
                    case 3: news.ShowsDays = 9; break;
                    case 4: news.ShowsDays = 14; break;
                }

                news.Rare = model.Rare;

                await _bankServices.UpdateNews(news);
            }
            else
            {
                var news = await _bankServices.GetNews(model.Id);

                await _bankServices.DeleteNews(model.Id);
            }

            return RedirectToAction("CheckNews", "Bank");

        }

        [HttpGet("add_news")]
        [Authorize]
        public IActionResult CreateNews()
        {
            return View(new NewsModel());
        }

        [HttpGet("default_gov")]
        public async Task<IActionResult> SingleGovermentDefault()
        {
            var govs = await _bankServices.GetGovermentOrganization(-1);

            return View(govs.OrderByDescending(m => m.Balance).ToList());
        }

        [HttpPost("add_news")]
        [Authorize]
        public async Task<IActionResult> CreateNews(NewsModel model)
        {
            if (ModelState.IsValid)
            {
                var news = new News
                {
                    MiniInformation = model.MiniInformation,
                    Name = model.Name,
                    Type = "status_timing",
                    Url = model.Url
                };

                await _bankServices.CreateNews(news);

                return RedirectToAction("Index", "Bank");
            }

            return View(model);
        }

        [HttpGet("org/{id}")]
        public async Task<IActionResult> SingleOrganization(int id)
        {
            var org = await _bankServices.GetOrganizations(id);
            if (org != null)
            {
                var items = await _bankServices.GetOrganizationsItem(id);

                items = items.Skip(Math.Max(0, items.Count() - 4)).ToList();

                var model = new SingleOrganizationModel
                {
                    GovermentPolitical = await _bankServices.GetGoverment(org.GovermentId),
                    LastUserItems = items,
                    organization = org
                };

                return View(model);
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        [HttpGet("top")]
        public ActionResult TopUsers()
        {
            return View(BankServices.topUser);
        }

        [HttpGet("adminUsers")]
        public async Task<IActionResult> AdminTopUsers()
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);
            if (perm.GetStatistics)
            {
                return View();
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });

        }

        [HttpGet("getCase")]
        public async Task<IActionResult> BuyCase()
        {
            var user = await _bankServices.GetUser();

            if (user != null)
            {
                return View();
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        public async Task<IActionResult> BuyCaseFinal()
        {
            var sys = await _bankServices.GetGoverment();

            if (!sys.CasesIsOn)
            {
                return Json(new { type = "error", message = "Кейсы временно не работают." });
            }

            var user = await _bankServices.GetUser();

            if (user != null)
            {
                if (user.Money < 2000)
                    return Json(new { message = "Недостаточно средств", type = "error" });

                user.Money -= 2000;

                await _bankServices.SpentMoney(2000);

                var random = new Random();
                var num = random.Next(0, 100000);

                await _bankServices.UpdateUser(user);


                await _bankServices.CreateTransaction(new Transaction
                {
                    Date = _bankServices.NowDateTime(),
                    Id1 = user.Id,
                    Id2 = -1,
                    Text = "Покупка ящика с предметами.",
                    Value = 2000
                });

                var list = new List<ShopItem>();

                if (num is >= 0 and <= 70000) // default
                {
                    list = await _bankServices.GetItemForType(0);
                }
                else if (num is > 70000 and <= 99000) // дорогое
                {
                    list = await _bankServices.GetItemForType(1);
                }
                else if (num is > 99000 and <= 99500) // редкое
                {
                    list = await _bankServices.GetItemForType(2);
                }
                else if (num is > 99500 and <= 99990) // epic 
                {
                    list = await _bankServices.GetItemForType(3);
                }
                else // legendary
                {
                    list = await _bankServices.GetItemForType(4);
                }

                var item = Random(list);

                await _bankServices.SetUserItem(user, item);

                return Json(new { item = item, type = "ok" });

            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        public T Random<T>(IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }

            var r = new Random();
            var list = enumerable as IList<T> ?? enumerable.ToList();
            return list.Count == 0 ? default(T) : list[r.Next(0, list.Count)];
        }

        [HttpGet("itemTop")]
        public IActionResult ItemTopUser()
        {
            return View(BankServices.topItemUser);
        }

        [HttpGet("error/{code}")]
        public async Task<IActionResult> Error(int code) // Ошибки там
        {
            var user = await _bankServices.GetUser();

            if (user == null && code == 1200)
                return RedirectToAction("Error", "Bank", new { code = 404 });

            var model = new ErrorModel
            {
                ErrorCode = code
            };

            return View(model);
        }

        [HttpGet("admin/create_org")]
        [Authorize]
        public async Task<IActionResult> CreateOrganization()
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (perm.CreateOrganization)
            {
                return View(new CreateOrganization());
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("org/transfer_in/{id}")]
        [Authorize]
        public async Task<IActionResult> SendMoneyOrganization(int id)
        {
            var org = await _bankServices.GetOrganizations(id);
            if (org != null)
            {
                if (org.Status != "status_ok")
                    return RedirectToAction("Error", "Bank", new { code = 404 });

                var model = new TransferOrganizationModel
                {
                    orgId = id
                };

                return View(model);
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        [HttpGet("org/transfer_out/{id}")]
        [Authorize]
        public async Task<IActionResult> OutMoneyOrganization(int id)
        {
            var user = await _bankServices.GetUser();
            var org = await _bankServices.GetOrganizations(id);
            var perm = await _bankServices.GetUserPermission(user);

            if (org != null && (org.AdminId == user.Id || org.Zam1Name == user.Id || perm.ChangeOrganizationEconomy || org.Zam2Name == user.Id))
            {
                if (org.Status != "status_ok")
                    return RedirectToAction("Error", "Bank", new { code = 404 });

                var model = new TransferOutOrganizationModel
                {
                    orgId = id
                };

                return View(model);
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        [HttpPost("admin/editDeposit/{id}")]
        [Authorize]
        public async Task<IActionResult> UserEditDeposit(Deposit model, int id)
        {
            var user = await _bankServices.FindByIdAsync(model.UserId);
            var dep = await _bankServices.GetDeposit(user);
            var view = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(view);

            if (user != null && dep != null)
            {
                if (perm.ChangeUserDeposit)
                {
                    dep.Money = model.Money;

                    await _bankServices.SaveDeposit(dep);

                    return RedirectToAction("MyDeposit", "Bank", new { id = model.UserId });
                }

                return RedirectToAction("Error", "Bank", new { code = 700 });

            }

            return RedirectToAction("Error", "Bank", new { code = 404 });

        }

        [HttpGet("admin/editDeposit/{id}")]
        [Authorize]
        public async Task<IActionResult> UserEditDeposit(int id)
        {
            var user = await _bankServices.FindByIdAsync(id);
            var viewd = await _bankServices.GetUser();

            var perm = await _bankServices.GetUserPermission(viewd);

            if (user != null)
            {
                if (perm.ChangeUserDeposit)
                {
                    var Dep = await _bankServices.GetDeposit(user);

                    if (Dep != null)
                    {
                        return View(Dep);
                    }

                    return RedirectToAction("Error", "Bank", new { code = 404 });
                }

                return RedirectToAction("Error", "Bank", new { code = 700 });
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        [HttpPost("org/transfer_out/{id}")]
        [Authorize]
        public async Task<IActionResult> OutMoneyOrganization(TransferOutOrganizationModel model, int id)
        {
            var userTo = await _bankServices.FindByNameAsync(model.NameTo);
            var orgIn = await _bankServices.GetOrganizations(model.orgId);
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (orgIn != null && (orgIn.AdminId == user.Id || perm.ChangeOrganizationEconomy || orgIn.Zam1Name == user.Id || orgIn.Zam2Name == user.Id))
            {

                if (model.Value <= 0)
                {
                    ModelState.AddModelError("", "Неверное количество денег.");
                    return View(model);
                }

                var stavka = await _bankServices.GetStavka();
                var nalog = stavka;

                if (model.Value > 100)
                {
                    nalog = (int)((model.Value / 100) * stavka);
                }

                if (model.Value < stavka)
                {
                    ModelState.AddModelError("", "Минимум " + stavka + " Мемлар!");
                    return View(model);
                }

                if (model.Value + nalog > orgIn.Balance)
                {
                    ModelState.AddModelError("", "Недостаточно средств в организации.");
                    return View(model);
                }


                if (userTo != null)
                {
                    if (userTo.IsArrested)
                    {
                        ModelState.AddModelError("", "Пользователь аррестован.");
                        return View(model);
                    }

                    await _bankServices.AddMoneyToGov((int)nalog);

                    var trans = new BankTransaction
                    {
                        Date = _bankServices.NowDateTime(),
                        BankId1 = orgIn.Id,
                        Id2 = userTo.Id,
                        Text = model.Comment,
                        Value = model.Value
                    };

                    var trans2 = new Transaction
                    {
                        Date = _bankServices.NowDateTime(),
                        Id1 = -1,
                        Id2 = userTo.Id,
                        Value = model.Value,
                        Text = "Выведено со счёта организации '" + orgIn.Name + "'"
                    };

                    await _bankServices.SpentMoney(model.Value + (int)nalog);
                    await _bankServices.AddToRecdStat((int)nalog);

                    orgIn.Balance -= model.Value + (int)nalog;
                    userTo.Money += model.Value;

                    await _bankServices.UpdateOrganization(orgIn);
                    await _bankServices.UpdateUser(userTo);

                    await _bankServices.CreateBankTransaction(trans);
                    await _bankServices.CreateTransaction(trans2);

                    return RedirectToAction("SingleOrganization", "Bank", new { id = orgIn.Id });

                }

                ModelState.AddModelError("", "Получателя с именем " + model.NameTo + " не существует.");
            }
            else return RedirectToAction("Error", "Bank", new { code = 700 });

            return View(model);
        }

        [HttpGet("org/edit_orig/{id}")]
        [Authorize]
        public async Task<IActionResult> EditOrganization(int id)
        {
            var user = await _bankServices.GetUser();
            var org = await _bankServices.GetOrganizations(id);
            var perm = await _bankServices.GetUserPermission(user);

            if (org != null)
            {
                if (perm.ChangeOrganizationEconomy || perm.ChangeOrganizationInfo || user.Id == org.AdminId || user.Id == org.Zam1Name || user.Id == org.Zam2Name)
                {
                    var userAdmin = await _bankServices.FindByIdAsync(org.AdminId);

                    var model = new EditOrganizationModel
                    {
                        AdminName = userAdmin.Name,
                        Short_Desc = org.Short_Desc,
                        NewStatus = org.Status,
                        VkUrl = org.VkUrl,
                        Name = org.Name,
                        Id = org.Id
                    };

                    if (perm.ChangeOrganizationEconomy)
                    {
                        model.Balance = org.Balance;
                        model.Influence = org.Influence;
                    }

                    return View(model);

                }

                return RedirectToAction("Error", "Bank", new { code = 700 });
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        [HttpGet("org/edit_orig_zam/{id}")]
        [Authorize]
        public async Task<IActionResult> EditOrganizationZam(int id)
        {
            var user = await _bankServices.GetUser();
            var org = await _bankServices.GetOrganizations(id);
            var perm = await _bankServices.GetUserPermission(user);

            if (org != null)
            {
                if (perm.EditZamOrg || user.Id == org.AdminId)
                {
                    var usr1 = await _bankServices.FindByIdAsync(org.Zam1Name);
                    var usr2 = await _bankServices.FindByIdAsync(org.Zam2Name);

                    var model = new SetOrganizationZamModel
                    {
                        Id = id,
                        Zam1 = (usr1 == null ? "" : usr1.Name),
                        Zam2 = (usr2 == null ? "" : usr2.Name)
                    };

                    return View(model);

                }

                return RedirectToAction("Error", "Bank", new { code = 700 });
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        [HttpPost("org/edit_orig_zam/{id}")]
        [Authorize]
        public async Task<IActionResult> EditOrganizationZam(SetOrganizationZamModel model, int id)
        {
            var user = await _bankServices.GetUser();
            var org = await _bankServices.GetOrganizations(model.Id);
            var perm = await _bankServices.GetUserPermission(user);

            if (org != null)
            {
                if (perm.EditZamOrg || user.Id == org.AdminId)
                {
                    if (model.Zam1 == model.Zam2 && !string.IsNullOrEmpty(model.Zam1))
                    {
                        ModelState.AddModelError("", "Одинковые ники.");
                        return View(model);
                    }

                    if (string.IsNullOrEmpty(model.Zam1))
                    {
                        org.Zam1Name = 0;
                    }
                    else
                    {
                        var zam1 = await _bankServices.FindByNameAsync(model.Zam1);

                        if (zam1 == null)
                        {
                            ModelState.AddModelError("", "Ошибка в первом нике заместителя.");
                            return View(model);
                        }

                        org.Zam1Name = zam1.Id;
                    }

                    if (string.IsNullOrEmpty(model.Zam2))
                    {
                        org.Zam2Name = 0;
                    }
                    else
                    {

                        var zam2 = await _bankServices.FindByNameAsync(model.Zam2);

                        if (zam2 == null)
                        {
                            ModelState.AddModelError("", "Ошибка во втором нике заместителя.");
                            return View(model);
                        }

                        org.Zam2Name = zam2.Id;

                    }

                    await _bankServices.UpdateOrganization(org);

                    return RedirectToAction("SingleOrganization", "Bank", new { id = model.Id });
                }

                return RedirectToAction("Error", "Bank", new { code = 700 });
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        [HttpGet("org/del_org/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteOrganization(int id)
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (perm.DeleteOrganization)
            {
                var org = await _bankServices.GetOrganizations(id);

                if (org != null)
                {
                    return View(org);
                }

                return RedirectToAction("Error", "Bank", new { code = 404 });
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("org/del_org/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteOrganization(Organization org)
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (perm.DeleteOrganization)
            {
                if (org != null)
                {
                    await _bankServices.DeleteOrganization(org);

                    return RedirectToAction("Index", "Bank");
                }

                return RedirectToAction("Error", "Bank", new { code = 404 });
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("org/edit_orig/{id}")]
        [Authorize]
        public async Task<IActionResult> EditOrganization(EditOrganizationModel model)
        {
            var user = await _bankServices.GetUser();
            var org = await _bankServices.GetOrganizations(model.Id);
            var perm = await _bankServices.GetUserPermission(user);

            if (org != null)
            {
                if (perm.ChangeOrganizationInfo || perm.ChangeOrganizationEconomy || user.Id == org.AdminId || user.Id == org.Zam1Name || user.Id == org.Zam2Name)
                {
                    var adminUser = await _bankServices.FindByNameAsync(model.AdminName);

                    if (perm.ChangeOrganizationEconomy || perm.ChangeOrganizationInfo)
                    {
                        if (perm.ChangeOrganizationEconomy)
                        {
                            org.Balance = model.Balance;
                            org.Influence = model.Influence;
                        }

                        org.AdminId = adminUser.Id;

                        var status = "";

                        if (model.NewStatus == "Заморожено")
                        {
                            status = "status_frozzen";
                        }
                        else if (model.NewStatus == "Работает")
                        {
                            status = "status_ok";
                        }
                        else
                        {
                            status = "status_off";
                        }

                        org.Status = status;
                    }

                    if (model.ImageUrl != null || !string.IsNullOrEmpty(model.ImageStringUrl))
                    {

                        if (model.ImageUrl != null)
                        {
                            if (model.ImageUrl.Length > 10485760) // 10mb
                            {
                                ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный размер.");
                                return View(model);
                            }

                            if (!model.ImageUrl.IsImage())
                            {
                                ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный формат.");
                                return View(model);
                            }

                            var img = await _bankServices.CreateImageSys(_appEnvironment, model.ImageUrl, "userImagesStatic", -1);

                            org.ImageUrl = img.ScreePath;
                        }
                        else org.ImageUrl = model.ImageStringUrl;
                    }

                    org.Name = model.Name;
                    org.Short_Desc = model.Short_Desc;
                    org.VkUrl = model.VkUrl;


                    await _bankServices.UpdateOrganization(org);
                    return RedirectToAction("SingleOrganization", "Bank", new { id = org.Id });

                }

                return RedirectToAction("Error", "Bank", new { code = 700 });
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        /*[HttpGet("faq")]
        public IActionResult FAQ()
        {
            return View();
        }*/

        //GovCreateOrganization


        [HttpGet("org/create_buy")]
        [Authorize]
        public IActionResult BuyCreateOrganization()
        {
            return View(new CreateOrganization());
        }

        [HttpGet("org/toggleOrg/{id}")]
        [Authorize]
        public async Task<IActionResult> ToggleOrganization(int id)
        {
            var org = await _bankServices.GetOrganizations(id);
            var gov = await _bankServices.GetGoverment(org.GovermentId);
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (org != null && gov != null)
            {
                if (gov.LeaderId == user.Id || perm.ChangeOrganizationInfo)
                {
                    return View(org.Id);
                }

                return RedirectToAction("Error", "Bank", new { code = 700 });
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        [HttpPost("org/toggleOrg/{id}")]
        [Authorize]
        public async Task<IActionResult> ToggleOrganization(int id, Organization orgs)
        {
            var org = await _bankServices.GetOrganizations(id);
            var gov = await _bankServices.GetGoverment(org.GovermentId);
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (org != null && gov != null)
            {
                if (gov.LeaderId == user.Id || perm.ChangeOrganizationInfo)
                {
                    if (org.Status == "status_ok")
                    {
                        org.Status = " status_frozzen";
                    }
                    else org.Status = "status_ok";

                    await _bankServices.UpdateOrganization(org);

                    return RedirectToAction("SingleOrganization", "Bank", new { id = org.Id });
                }

                return RedirectToAction("Error", "Bank", new { code = 700 });
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        [HttpGet("org/creategovorg/{id}")]
        [Authorize]
        public async Task<IActionResult> GovCreateOrganization(int id)
        {
            var gov = await _bankServices.GetGoverment(id);
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (gov != null)
            {
                if (gov.LeaderId == user.Id || perm.CreateOrganization)
                {
                    return View(new CreateOrganizationGov
                    {
                        GovermentId = id
                    });
                }

                return RedirectToAction("Error", "Bank", new { code = 700 });
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        [HttpPost("org/creategovorg/{id}")]
        [Authorize]
        public async Task<IActionResult> GovCreateOrganization(CreateOrganizationGov model, int id)
        {
            var gov = await _bankServices.GetGoverment(id);
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (gov != null)
            {
                if (gov.LeaderId == user.Id || perm.CreateOrganization)
                {
                    if (gov.FreeOrganizationCreateCount <= 0)
                    {
                        ModelState.AddModelError("", "У вас лимит на организации.");
                        return View(model);
                    }

                    var org = new Organization
                    {
                        AdminId = user.Id,
                        Balance = 0,
                        Influence = 0,
                        Name = model.Name,
                        Status = "status_ok",
                        Short_Desc = model.Short_Desc,
                        VkUrl = model.VkUrl,
                        isBuy = false,
                        GovermentId = gov.Id
                    };

                    if (model.ImageUrl != null || !string.IsNullOrEmpty(model.ImageStringUrl))
                    {

                        if (model.ImageUrl != null)
                        {
                            if (model.ImageUrl.Length > 10485760) // 10mb
                            {
                                ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный размер.");
                                return View(model);
                            }

                            if (!model.ImageUrl.IsImage())
                            {
                                ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный формат.");
                                return View(model);
                            }

                            var img = await _bankServices.CreateImageSys(_appEnvironment, model.ImageUrl, "userImagesStatic", -1);

                            org.ImageUrl = img.ScreePath;
                        }
                        else org.ImageUrl = model.ImageStringUrl;
                    }
                    else
                    {
                        ModelState.AddModelError("", "Картинка не была установлена.");
                        return View(model);
                    }

                    gov.FreeOrganizationCreateCount -= 1;

                    await _bankServices.UpdateGoverment(gov);

                    await _bankServices.CreateOrganization(org);

                    return RedirectToAction("SingleOrganization", "Bank", new { id = org.Id });
                }

                return RedirectToAction("Error", "Bank", new { code = 700 });
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        [HttpPost("org/addUserToJob/{id}/{orgId}")]
        public async Task<IActionResult> AddUserToJob(AddUserToJob model, int id, int orgId)
        {
            var view = await _bankServices.GetUser();
            var user = await _bankServices.FindByNameAsync(model.Name);
            var orgj = await _bankServices.GetOrganizationJob(model.orgJob.Id);
            var org = await _bankServices.GetOrganizations(orgId);
            var perm = await _bankServices.GetUserPermission(view);

            if (view.Id == org.AdminId || perm.JobSettings || view.Id == org.Zam1Name || view.Id == org.Zam2Name)
            {
                if (user != null)
                {
                    var userJob = await _bankServices.GetOrgJobForUser(user, orgj);

                    if (userJob != null)
                    {
                        ModelState.AddModelError("", "Пользователей уже на этой должности.");
                        return View(model);
                    }

                    if (user.IsArrested)
                    {
                        ModelState.AddModelError("", "Пользователь аррестован.");
                        return View(model);
                    }

                    var a = new OrgJobUser
                    {
                        UserId = user.Id,
                        OrgJobId = model.orgJob.Id
                    };

                    await _bankServices.SetUserToJob(a);

                    return RedirectToAction("JobList", "Bank", new { id = orgId });
                }

                ModelState.AddModelError("", "Пользователей с ником '" + model.Name + "' не найдено.");
                return View(model);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("org/addUserToJob/{id}/{orgId}")]
        public async Task<IActionResult> AddUserToJob(int id, int orgId)
        {
            var job = await _bankServices.GetOrganizationJob(id);
            var org = await _bankServices.GetOrganizations(orgId);
            var user = await _bankServices.GetUser();

            var perm = await _bankServices.GetUserPermission(user);

            if (user.Id == org.AdminId || perm.JobSettings || user.Id == org.Zam1Name || user.Id == org.Zam2Name)
            {
                var model = new AddUserToJob
                {
                    orgJob = job
                };

                return View(model);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("org/edit_job/{id}")]
        public async Task<IActionResult> UpdateJob(int id)
        {
            var job = await _bankServices.GetOrganizationJob(id);
            var org = await _bankServices.GetOrganizations(job.OrganizationId);
            var user = await _bankServices.GetUser();


            var perm = await _bankServices.GetUserPermission(user);

            if (user.Id == org.AdminId || perm.JobSettings || user.Id == org.Zam1Name || user.Id == org.Zam2Name)
            {
                return View(job);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("org/edit_job/{id}")]
        public async Task<IActionResult> UpdateJob(OrgJob model)
        {
            var org = await _bankServices.GetOrganizations(model.OrganizationId);
            var user = await _bankServices.GetUser();

            var perm = await _bankServices.GetUserPermission(user);

            if (user.Id == org.AdminId || perm.JobSettings || user.Id == org.Zam1Name || user.Id == org.Zam2Name)
            {
                if (model.PayDay < 0)
                {
                    ModelState.AddModelError("", "Неверное число.");
                    return View(model);
                }

                if (string.IsNullOrEmpty(model.Name))
                {
                    ModelState.AddModelError("", "Неверное имя.");
                    return View(model);
                }

                await _bankServices.UpdateJob(model);

                return RedirectToAction("JobList", "Bank", new { id = model.OrganizationId });
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("org/create_job/{id}")]
        public async Task<IActionResult> CreateJob(int id)
        {
            var org = await _bankServices.GetOrganizations(id);
            var user = await _bankServices.GetUser();

            var perm = await _bankServices.GetUserPermission(user);

            if (user.Id == org.AdminId || perm.JobSettings || user.Id == org.Zam1Name || user.Id == org.Zam2Name)
            {
                return View();
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("org/delete_orgJobuserWarning/{id}/{orgJobId}/{userId}")]
        public async Task<IActionResult> DeleteOrgJobUserwarning(int id, int orgJobId, int userId)
        {
            var org = await _bankServices.GetOrganizations(id);
            var orgJob = await _bankServices.GetOrganizationJob(orgJobId);
            var user = await _bankServices.FindByIdAsync(userId);
            var view = await _bankServices.GetUser();

            var perm = await _bankServices.GetUserPermission(view);

            if (view.Id == org.AdminId || perm.JobSettings || user.Id == org.Zam1Name || user.Id == org.Zam2Name)
            {
                var model = new DeleteUserJobAlertModel
                {
                    OrganizationId = org.Id,
                    OrganizationJobId = orgJob.Id,
                    UserId = user.Id
                };

                return View(model);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("adm/create_project")]
        public async Task<IActionResult> CreateProject()
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (perm.CreateProject)
            {
                return View();
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("project/{id}")]
        public async Task<IActionResult> SingleProject(int id)
        {
            var project = await _bankServices.GetProject(id);

            if (project != null)
            {
                return View(project);
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        [HttpGet("project_send/{id}")]
        public async Task<IActionResult> SendMoneyProject(int id)
        {
            var user = await _bankServices.GetUser();

            if (user == null)
                return RedirectToAction("Error", "Bank", new { code = 700 });

            var project = await _bankServices.GetProject(id);

            if (project != null)
            {
                var model = new SendMoneyProjectModel
                {
                    ProjectId = id
                };

                return View(model);
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        [HttpGet("admin/deletetransaction/{transId}")]
        public async Task<IActionResult> DeleteTransaction(int transId)
        {
            var user = await _bankServices.GetUser();

            var perm = await _bankServices.GetUserPermission(user);

            if (perm.DeleteTransaction)
            {
                var trans = await _bankServices.GetTransaction(transId);

                if (trans != null)
                {
                    await _bankServices.DeleteTransaction(trans);

                    return RedirectToAction("LastTransactions", "Bank");
                }
            }
            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        public async Task<IActionResult> CasinoMinMaxApply(int userId, float sum, float procent)
        {
            var sys = await _bankServices.GetGoverment();
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (!perm.EconomyPanel)
            {
                if (!sys.CasinoIsOn)
                {
                    return Json(new { type = "error", message = "Казино временно не работает." });
                }
            }

            try
            {
                var maxNum = 100000;

                var gov = await _bankServices.GetGoverment();

                if (user != null)
                {

                    if (sum <= 0)
                        return Json(new { type = "error", message = "Непредвиденная ошибка" });

                    if (procent < 0.009 || procent > 95)
                        return Json(new { type = "error", message = "Непредвиденная ошибка" });

                    if (sum - 0.01 > user.Coins)
                        return Json(new { type = "error", message = "Нехватает монет" });

                    var rnd = new Random();

                    var exhange = rnd.Next(0, maxNum); // Рандомная число

                    var predprocent = procent;

                    if (procent >= 40)
                    {
                        var podkrutka = rnd.Next(0, gov.Skrutka);
                        procent -= podkrutka;
                    }
                    else if (procent < 5)
                    {
                        if (procent >= 0.001 && procent <= 0.11)
                        {
                            var a = rnd.Next(0, 2);

                            if (a == 1)
                            {
                                procent = 0;
                            }
                        }
                        else
                        {
                            var podkrutka = (float)(rnd.NextDouble() - 0.6);
                            procent -= podkrutka;

                            if (procent <= 0)
                            {
                                procent = 0;
                            }
                        }
                    }


                    var separator = (maxNum / 100) * procent; // диапазон

                    var win = 100 / predprocent * sum;

                    if (exhange == separator)
                        exhange++;

                    if (exhange < separator)
                    {
                        user.Coins += win - sum;

                        await _bankServices.UpdateUser(user);

                        if (win >= sum * 2.5)
                        {
                            await _bankServices.CreateWinner(new CasinoWin
                            {
                                Count = win,
                                Date = _bankServices.NowDateTime(),
                                WinnerId = user.Id
                            });
                        }

                        return Json(new { type = "win", message = "Выпало: " + exhange, coins = user.Coins });
                    }

                    user.Coins -= sum;

                    await _bankServices.UpdateUser(user);

                    return Json(new { type = "nowin", message = "Выпало: " + exhange, coins = user.Coins });
                }

                return Json(new { type = "error", message = "Непредвиденная ошибка" });
            }
            catch
            {
                return Json(new { type = "error", message = "Непредвиденная ошибка" });
            }
        }

        [HttpGet("editurlvk")]
        [Authorize]
        public async Task<IActionResult> EditUrlVkNew(long uid, string hash)
        {
            if (hash != MD5HashPHP("sosamba" + uid + "sosamba"))
                return RedirectToAction("Error", "Bank", new { code = 1500 });

            var user = await _bankServices.GetUser();

            if (user == null)
                return RedirectToAction("Error", "Bank", new { code = 1500 });

            var search = (await _bankServices.GetAllUser()).Where(m => m.VKUniqId == uid).FirstOrDefault();

            if (search != null)
                return RedirectToAction("Error", "Bank", new { code = 1500 });

            user.VKUniqId = uid;

            await _bankServices.UpdateUser(user);

            return RedirectToAction("Index", "Bank");
        }

        [HttpGet("admin/allTransactionUser/{id}")]
        public async Task<IActionResult> GetAllUserTransaction(int id)
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if(perm.ShowAllOrgOrUserTransaction)
            {
                var search = await _bankServices.FindByIdAsync(id);

                if (search != null)
                {
                    var list = _bankServices.GetTransactions(search, -1);

                    return View(new AllUserTrans { id = id, list = list });
                }

                return RedirectToAction("Error", "Bank", new { code = 404 });
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("admin/allTransactionOrg/{id}")]
        public async Task<IActionResult> GetAllOrgTransaction(int id)
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (perm.ShowAllOrgOrUserTransaction)
            {
                var search = await _bankServices.GetOrganizations(id);

                if (search != null)
                {
                    var list = await _bankServices.GetOrganizationTransactions(search, -1);

                    return View(new AllOrgTrans { id = id, list = list });
                }

                return RedirectToAction("Error", "Bank", new { code = 404 });
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("casino/indraw")]
        public async Task<IActionResult> CasinoInDraw()
        {
            var user = await _bankServices.GetUser();

            if (user != null)
            {
                return View(new WithDrawCasino());
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("casino/indraw")]
        public async Task<IActionResult> CasinoInDraw(WithDrawCasino model)
        {
            var user = await _bankServices.GetUser();

            if (user != null)
            {
                try
                {
                    if (Convert.ToInt32(model.Coins) <= 0)
                    {
                        ModelState.AddModelError("", "Ошибка, число отрицательное.");
                        return View(model);
                    }
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Ошибка - не число.");
                    return View(model);
                }

                var Coins = Convert.ToDouble(model.Coins, System.Globalization.CultureInfo.InvariantCulture);

                if (user.Money < Coins * 100)
                {
                    ModelState.AddModelError("", "Недостаточно кеклар");
                    return View(model);
                }

                var money = (int)(Coins * 100);

                if (money <= 0)
                {
                    ModelState.AddModelError("", "Нельзя столько купить");
                    return View(model);
                }

                user.Coins += (float)Coins;
                await _bankServices.SpentMoney(money);
                user.Money -= money;

                await _bankServices.UpdateUser(user);

                await _bankServices.CreateTransaction(new Transaction
                {
                    Date = _bankServices.NowDateTime(),
                    Id2 = -1,
                    Id1 = user.Id,
                    Text = "Куплено коинов в казино.",
                    Value = money
                });

                var org = await _bankServices.GetOrganizations("casino");

                await _bankServices.CreateBankTransaction(new BankTransaction
                {
                    BankId2 = org.Id,
                    Id1 = user.Id,
                    Value = money,
                    Date = _bankServices.NowDateTime(),
                    Text = "Обмен кеклар на коины"
                });

                org.Balance += money;

                await _bankServices.UpdateOrganization(org);

                return RedirectToAction("CasinoMinMax", "Bank");
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }


        [HttpGet("casino/withdraw")]
        public async Task<IActionResult> CasinoWithDraw()
        {
            var user = await _bankServices.GetUser();

            if (user != null)
            {
                return View(new WithDrawCasino());
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("casino/withdraw")]
        public async Task<IActionResult> CasinoWithDraw(WithDrawCasino model)
        {
            var user = await _bankServices.GetUser();

            if (user != null)
            {
                var Coins = Convert.ToDouble(model.Coins, System.Globalization.CultureInfo.InvariantCulture);

                if (Coins > user.Coins + 0.01)
                {
                    ModelState.AddModelError("", "Недостаточно коинов.");
                    return View(model);
                }

                var money = (int)(Coins * 95);

                if (money <= 0)
                {
                    ModelState.AddModelError("", "Нельзя столько обменять.");
                    return View(model);
                }

                var org = await _bankServices.GetOrganizations("casino");

                if (org.Balance < money)
                {
                    ModelState.AddModelError("", "В организации нету денег.");
                    return View(model);
                }

                user.Coins -= (float)Coins;
                user.Money += money;

                await _bankServices.UpdateUser(user);

                await _bankServices.CreateTransaction(new Transaction
                {
                    Date = _bankServices.NowDateTime(),
                    Id1 = -1,
                    Id2 = user.Id,
                    Text = "Выведено со счёта казино.",
                    Value = money
                });

                await _bankServices.CreateBankTransaction(new BankTransaction
                {
                    Id2 = user.Id,
                    BankId1 = org.Id,
                    Value = money,
                    Date = _bankServices.NowDateTime(),
                    Text = "Обмен коинов на Мемлары"
                });

                org.Balance -= money;

                await _bankServices.UpdateOrganization(org);

                return RedirectToAction("CasinoMinMax", "Bank");
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("casino/minmax")]
        public async Task<IActionResult> CasinoMinMax()
        {
            var user = await _bankServices.GetUser();

            if (user != null)
            {
                return View(user);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("project_send/{id}")]
        public async Task<IActionResult> SendMoneyProject(int id, SendMoneyProjectModel model)
        {
            var user = await _bankServices.GetUser();
            var project = await _bankServices.GetProject(id);

            if (user.Money < model.Money)
            {
                ModelState.AddModelError("", "Недостаточно средств.");
                return View(model);
            }

            if (model.Money <= 0)
            {
                ModelState.AddModelError("", "Некоректное количество средств.");
                return View(model);
            }

            var money = model.Money;

            if (project.Balance + model.Money > project.Target)
                money = project.Target - project.Balance;

            project.Balance += money;
            user.Money -= money;
            await _bankServices.SpentMoney(money);

            await _bankServices.CreateTransaction(new Transaction
            {
                Date = _bankServices.NowDateTime(),
                Id1 = user.Id,
                Id2 = -1,
                Text = "Отправлено в счёт проекта: '" + project.Name + "'",
                Value = money
            });

            await _bankServices.CreateProjectSender(new ProjectSender
            {
                Date = _bankServices.NowDateTime(),
                Money = money,
                UserId = user.Id,
                ProjectId = project.Id
            });

            await _bankServices.UpdateUser(user);
            await _bankServices.UpdateProject(project);

            if (project.Balance >= project.Target)
            {
                var author = await _bankServices.FindByIdAsync(project.AuthorId);
                var gov = await _bankServices.GetGoverment();

                var org = await _bankServices.GetOrganizations("main"); // ТМЛ

                var moneyOrg = (project.Balance / 100) * gov.Nalog_Project;
                var moneyUser = (project.Balance / 100) * gov.UserGetMoneyProject;

                author.Money += moneyUser;

                await _bankServices.CreateTransaction(new Transaction
                {
                    Date = _bankServices.NowDateTime(),
                    Id1 = -1,
                    Id2 = author.Id,
                    Text = "Проект успешно завершился, доля создателя - " + gov.UserGetMoneyProject + "%",
                    Value = moneyUser
                });

                await _bankServices.UpdateUser(author);

                org.Balance += moneyOrg;

                await _bankServices.CreateBankTransaction(new BankTransaction
                {
                    Id1 = -1,
                    BankId2 = org.Id,
                    Date = _bankServices.NowDateTime(),
                    Text = "Проект успешно завершился, налог ТМЛ - " + gov.Nalog_Project + "%",
                    Value = moneyOrg
                });

                await _bankServices.UpdateOrganization(org);
            }

            return RedirectToAction("SingleProject", "Bank", new { id = project.Id });
        }

        public async Task<IActionResult> AllProject()
        {
            var projs = await _bankServices.GetAllProject();

            return View(projs);
        }

        [HttpPost("adm/create_project")]
        public async Task<IActionResult> CreateProject(CreateProejcttModel model)
        {
            var user = await _bankServices.FindByNameAsync(model.AuthorName);

            var userAdmin = await _bankServices.GetUser();

            var perm = await _bankServices.GetUserPermission(userAdmin);

            if (!perm.CreateProject)
                return RedirectToAction("Error", "Bank", new { code = 700 });

            if (user == null)
            {
                ModelState.AddModelError("", "Такого пользователя нету");
                return View(model);
            }

            var project = new Infrastructure.Project
            {
                AuthorId = user.Id,
                Balance = 0,
                Info = model.Info,
                Name = model.Name,
                Target = model.Target,
            };

            if (model.ImageUrl != null || !string.IsNullOrEmpty(model.ImageStringUrl))
            {

                if (model.ImageUrl != null)
                {
                    if (model.ImageUrl.Length > 10485760) // 10mb
                    {
                        ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный размер.");
                        return View(model);
                    }

                    if (!model.ImageUrl.IsImage())
                    {
                        ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный формат.");
                        return View(model);
                    }

                    var img = await _bankServices.CreateImageSys(_appEnvironment, model.ImageUrl, "userImagesStatic", -1);

                    project.ImageUrl = img.ScreePath;
                }
                else project.ImageUrl = model.ImageStringUrl;
            }
            else
            {
                ModelState.AddModelError("", "Картинка не была установлена.");
                return View(model);
            }

            await _bankServices.CreateProject(project);

            return RedirectToAction("SingleProject", "Bank", new { id = project.Id });
        }

        [HttpPost("org/delete_orgJobuser/{id}/{orgJobId}/{userId}")]
        public async Task<IActionResult> DeleteOrgJobUser(DeleteUserJobAlertModel model)
        {
            var org = await _bankServices.GetOrganizations(model.OrganizationId);
            var orgJob = await _bankServices.GetOrganizationJob(model.OrganizationJobId);
            var user = await _bankServices.FindByIdAsync(model.UserId);

            var view = await _bankServices.GetUser();

            var perm = await _bankServices.GetUserPermission(view);

            if (view.Id == org.AdminId || perm.JobSettings || view.Id == org.Zam1Name || view.Id == org.Zam2Name)
            {
                await _bankServices.DeleteOrgJobUser(orgJob, user);

                return RedirectToAction("JobList", "Bank", new { id = model.OrganizationId });
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("org/delete_orgJob/{id}")]
        public async Task<IActionResult> DeleteOrgJob(int id)
        {
            var orgJob = await _bankServices.GetOrganizationJob(id);

            var org = await _bankServices.GetOrganizations(orgJob.OrganizationId);
            var user = await _bankServices.GetUser();

            var perm = await _bankServices.GetUserPermission(user);

            if (user.Id == org.AdminId || perm.JobSettings || user.Id == org.Zam1Name || user.Id == org.Zam2Name)
            {
                await _bankServices.DeleteOrganizationJob(orgJob);

                return RedirectToAction("JobList", "Bank", new { id = orgJob.OrganizationId });
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("org/create_job/{id}")]
        public async Task<IActionResult> CreateJob(CreateJobModel model, int id)
        {
            var org = await _bankServices.GetOrganizations(id);

            var user = await _bankServices.GetUser();

            var perm = await _bankServices.GetUserPermission(user);

            if (user.Id == org.AdminId || perm.JobSettings || user.Id == org.Zam1Name || user.Id == org.Zam2Name)
            {

                var orgjob = new OrgJob
                {
                    Name = model.Name,
                    OrganizationId = org.Id,
                    PayDay = model.PayDay
                };

                await _bankServices.CreateOrgJob(orgjob);

                return RedirectToAction("JobList", "Bank", new { id = id });
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("org/job_list/{id}")]
        public async Task<IActionResult> JobList(int id)
        {
            var org = await _bankServices.GetOrganizations(id);
            var user = await _bankServices.GetUser();

            var perm = await _bankServices.GetUserPermission(user);

            if (user.Id == org.AdminId || perm.JobSettings || user.Id == org.Zam2Name || user.Id == org.Zam1Name)
            {
                var list = await _bankServices.GetOrganizationJob(org);

                var model = new JobModel
                {
                    orgJobs = list,
                    id = id
                };

                return View(model);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("org/create_buy")]
        [Authorize]
        public async Task<IActionResult> BuyCreateOrganization(CreateOrganization model)
        {
            var user = await _bankServices.GetUser();

            var price = 200000;
            var fin = 5000;

            if (_bankServices.UserHavePremium(user))
            {
                price = price / 2;
                fin *= 2;
            }

            if (user.Money > price)
            {
                var org = new Organization
                {
                    AdminId = user.Id,
                    Balance = 0,
                    Influence = fin,
                    Name = model.Name,
                    Status = "status_ok",
                    Short_Desc = model.Short_Desc,
                    VkUrl = model.VkUrl,
                    isBuy = true,
                    GovermentId = -1
                };

                if (model.ImageUrl != null || !string.IsNullOrEmpty(model.ImageStringUrl))
                {

                    if (model.ImageUrl != null)
                    {
                        if (model.ImageUrl.Length > 10485760) // 10mb
                        {
                            ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный размер.");
                            return View(model);
                        }

                        if (!model.ImageUrl.IsImage())
                        {
                            ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный формат.");
                            return View(model);
                        }

                        var img = await _bankServices.CreateImageSys(_appEnvironment, model.ImageUrl, "userImagesStatic", -1);

                        org.ImageUrl = img.ScreePath;
                    }
                    else org.ImageUrl = model.ImageStringUrl;
                }
                else
                {
                    ModelState.AddModelError("", "Картинка не была установлена.");
                    return View(model);
                }

                user.Money -= price;
                await _bankServices.SpentMoney(price);

                await _bankServices.AddToRecdStat(price);

                var ticket = new Transaction
                {
                    Date = _bankServices.NowDateTime(),
                    Id1 = user.Id,
                    Id2 = -1,
                    Text = "Покупка лицензии на создание официальной организации.",
                    Value = price
                };

                await _bankServices.CreateTransaction(ticket);
                await _bankServices.UpdateUser(user);
                await _bankServices.CreateOrganization(org);

                return RedirectToAction("SingleOrganization", "Bank", new { id = org.Id });
            }

            ModelState.AddModelError("", "Недостаточно средств.");
            return View(model);
        }

        [HttpGet("last_trans")]
        public IActionResult LastTransactions()
        {
            return View();
        }

        [HttpPost("org/transfer_in/{id}")]
        [Authorize]
        public async Task<IActionResult> SendMoneyOrganization(TransferOrganizationModel model, int id)
        {
            var orgTo = await _bankServices.GetOrganizations(model.orgId);
            var userIn = await _bankServices.GetUser();

            if (model.Value <= 0)
            {
                ModelState.AddModelError("", "Неверное количество денег.");
                return View(model);
            }

            var stavka = await _bankServices.GetStavka();
            var nalog = stavka;

            if (model.Value < stavka)
            {
                ModelState.AddModelError("", "Минимум " + stavka + " Мемлар!");
                return View(model);
            }

            if (model.Value > 100)
            {
                nalog = (int)((model.Value / 100) * stavka);
            }

            if (model.Value + nalog > userIn.Money)
            {
                ModelState.AddModelError("", "Недостаточно средств.");
                return View(model);
            }


            if (orgTo != null)
            {
                await _bankServices.AddMoneyToGov((int)nalog);

                var trans = new BankTransaction
                {
                    Date = _bankServices.NowDateTime(),
                    Id1 = userIn.Id,
                    BankId2 = orgTo.Id,
                    Text = model.Comment,
                    Value = model.Value
                };

                // TODO WTF??
                if (orgTo.Id == 19)
                {
                    await _bankServices.AddToRecdStat(model.Value);
                }

                var trans2 = new Transaction
                {
                    Date = _bankServices.NowDateTime(),
                    Id1 = userIn.Id,
                    Id2 = -1,
                    Value = model.Value,
                    Text = "Зачислено на счёт организации '" + orgTo.Name + "'"
                };

                await _bankServices.AddToRecdStat((int)nalog);

                userIn.Money -= model.Value + (int)nalog;
                await _bankServices.SpentMoney(model.Value + (int)nalog);
                orgTo.Balance += model.Value;

                await _bankServices.UpdateOrganization(orgTo);
                await _bankServices.UpdateUser(userIn);

                await _bankServices.CreateBankTransaction(trans);
                await _bankServices.CreateTransaction(trans2);

                return RedirectToAction("SingleOrganization", "Bank", new { id = orgTo.Id });
            }
            return View(model);
        }

        [HttpPost("admin/create_org")]
        [Authorize]
        public async Task<IActionResult> CreateOrganization(CreateOrganization model)
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);
            if (perm.CreateOrganization)
            {
                var admin = await _bankServices.FindByNameAsync(model.AdminName);

                if (admin == null)
                {
                    ModelState.AddModelError("", "Такого пользователя не существует.");
                    return View(model);
                }

                var status = "";

                if (model.NewStatus == "Заморожено")
                {
                    status = "status_frozzen";
                }
                else if (model.NewStatus == "Работает")
                {
                    status = "status_ok";
                }
                else
                {
                    status = "status_off";
                }

                var org = new Organization
                {
                    AdminId = admin.Id,
                    Balance = 0,
                    Influence = model.Influence,
                    Name = model.Name,
                    Status = status,
                    Short_Desc = model.Short_Desc,
                    VkUrl = model.VkUrl,
                    GovermentId = -1

                };

                if (model.ImageUrl != null || !string.IsNullOrEmpty(model.ImageStringUrl))
                {

                    if (model.ImageUrl != null)
                    {
                        if (model.ImageUrl.Length > 10485760) // 10mb
                        {
                            ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный размер.");
                            return View(model);
                        }

                        if (!model.ImageUrl.IsImage())
                        {
                            ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный формат.");
                            return View(model);
                        }

                        var img = await _bankServices.CreateImageSys(_appEnvironment, model.ImageUrl, "userImagesStatic", -1);

                        org.ImageUrl = img.ScreePath;
                    }
                    else org.ImageUrl = model.ImageStringUrl;
                }
                else
                {
                    ModelState.AddModelError("", "Картинка не была установлена.");
                    return View(model);
                }

                await _bankServices.CreateOrganization(org);

                return RedirectToAction("SingleOrganization", "Bank", new { id = org.Id });
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("user/transfer")]
        [Authorize]
        public IActionResult Transfer()
        {
            return View();
        }

        [HttpGet("user/transfer_complete")]
        [Authorize]
        public IActionResult TransferComplete()
        {
            return View();
        }

        [HttpGet("user/tickets/{id}")]
        [Authorize]
        public async Task<IActionResult> MyTickets(int id)
        {
            var user = await _bankServices.FindByIdAsync(id);
            if (user != null)
            {
                var userView = await _bankServices.GetUser();

                var perm = await _bankServices.GetUserPermission(userView);

                if (perm.CheckTickets || user.Id == userView.Id)
                {
                    var list = _bankServices.GetTickets(user, 10);
                    return View(list);
                }

                return RedirectToAction("Error", "Bank", new { code = 700 });
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        [HttpGet("admin/edit_user/{id}")]
        [Authorize]
        public async Task<IActionResult> UserEdit(int id)
        {
            var user = await _bankServices.FindByIdAsync(id);
            var userView = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(userView);
            var role = await _bankServices.GetUserRoleEntity(user);
            if (perm.ChangeUserEconomy || perm.ChangeUserInfo)
            {
                var model = new EditUserModel
                {
                    PremiumDay = user.PremiumDay,
                    IsArrested = user.IsArrested,
                    Money = user.Money,
                    Name = user.Name,
                    Role = role.RoleName
                };

                return View(model);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("admin/edit_user/{id}")]
        [Authorize]
        public async Task<IActionResult> UserEdit(EditUserModel userUpdate, int id)
        {
            var user = await _bankServices.GetUser();
            var updaterUser = await _bankServices.FindByIdAsync(id);
            var perm = await _bankServices.GetUserPermission(user);
            var roleEditor = await _bankServices.UserIsInRole(user, "Owner");

            if (perm.ChangeUserInfo || perm.ChangeUserEconomy)
            {
                if (userUpdate.Money < 0)
                {
                    ModelState.AddModelError("", "Неверное количество денег.");
                    return View(userUpdate);
                }

                if (string.IsNullOrEmpty(userUpdate.Name))
                {
                    ModelState.AddModelError("", "Неккоректное имя.");
                    return View(userUpdate);
                }

                var search = await _bankServices.FindByNameAsync(userUpdate.Name);

                if (userUpdate.Name != search.Name)
                {
                    if (search != null)
                    {
                        ModelState.AddModelError("", "Имя занято!");
                        return View(userUpdate);
                    }
                }

                updaterUser.Name = userUpdate.Name;
                if (perm.ChangeUserEconomy)
                {
                    updaterUser.Money = userUpdate.Money;
                    updaterUser.PremiumDay = userUpdate.PremiumDay;
                }
                updaterUser.IsArrested = userUpdate.IsArrested;

                if(roleEditor)
                {
                    var userRole = await _bankServices.GetUserRoleEntity(updaterUser);
                    userRole.RoleName = userUpdate.Role;
                    await _bankServices.SaveRole(userRole);
                }

                await _bankServices.UpdateUser(updaterUser);

                return RedirectToAction("Balance", "Bank", new { id = userUpdate.Id });
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("admin/president_panel/")]
        [Authorize]
        public async Task<IActionResult> GovEdit()
        {
            var user = await _bankServices.GetUser();
            var goverment = await _bankServices.GetGoverment();
            var gov = await _bankServices.UserIsPresident(user);
            var isOwnerOrAdmin = (await _bankServices.UserIsInRole(user, "Administrator")) || (await _bankServices.UserIsInRole(user, "Owner"));

            if (gov || isOwnerOrAdmin)
            {
                return View(goverment);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("admin/president_panel/")]
        [Authorize]
        public async Task<IActionResult> GovEdit(SystemMain model)
        {
            var user = await _bankServices.GetUser();
            var gov = (model.PresidentName == user.Name);
            var mgov = await _bankServices.GetGoverment();

            if (gov || user.Name == "Верховный Трибунал")
            {
                try
                {
                    if (Convert.ToDouble(model.Stavka) < 0 || Convert.ToDouble(model.Stavka_Nalog) < 0 || Convert.ToDouble(model.Stavka_Vlojen) < 0 || Convert.ToDouble(model.Nalog_Item) < 0)
                    {
                        ModelState.AddModelError("", "Неверное количество налога.");
                        return View(model);
                    }
                }
                catch
                {
                    ModelState.AddModelError("", "Налог должен иметь ввид '0,1' .");
                    return View(model);
                }

                mgov.MoneyFromLike = model.MoneyFromLike;
                mgov.Nalog_Item = model.Nalog_Item;
                mgov.Nalog_Project = model.Nalog_Project;
                mgov.PresidentName = model.PresidentName;
                mgov.SiteIsOn = model.SiteIsOn;
                mgov.Skrutka = model.Skrutka;
                mgov.Stavka = model.Stavka;
                mgov.Stavka_Nalog = model.Stavka_Nalog;
                mgov.Stavka_Vlojen = model.Stavka_Vlojen;
                mgov.UserGetMoneyProject = model.UserGetMoneyProject;
                mgov.CasesIsOn = model.CasesIsOn;
                mgov.CasinoIsOn = model.CasinoIsOn;

                await _bankServices.SaveGov(mgov);

                return RedirectToAction("SingleOrganization", "Bank", new { id = 4 });
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("user/deposit/{id}")]
        [Authorize]
        public async Task<IActionResult> MyDeposit(int id)
        {
            var user = await _bankServices.FindByIdAsync(id);
            if (user != null)
            {
                var dep = await _bankServices.GetDeposit(user);

                UserDepositModel model;

                if (dep == null)
                {
                    model = new UserDepositModel
                    {
                        dep = new Deposit
                        {
                            Money = 0
                        },
                        user = user
                    };
                }
                else
                {
                    model = new UserDepositModel
                    {
                        dep = dep,
                        user = user
                    };
                }

                return View(model);
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        [HttpGet("user/removeDeposit")]
        [Authorize]
        public IActionResult RemoveDeposit()
        {
            return View();
        }

        [HttpGet("user/addDeposit")]
        [Authorize]
        public IActionResult AddDeposit()
        {
            return View(new DepostModel());
        }

        [HttpGet("buy_prem")]
        public IActionResult BuyPremium()
        {
            return View();
        }

        [HttpGet("admin/create_gov")]
        public async Task<IActionResult> CreateGoverment()
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (perm.CreateGoverment)
            {
                return View(new CreateGoverment());
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("admin/create_gov")]
        public async Task<IActionResult> CreateGoverment(CreateGoverment model)
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (perm.CreateGoverment)
            {
                var userGov = await _bankServices.FindByNameAsync(model.AdminName);

                if (userGov != null)
                {
                    var tml_orf = await _bankServices.GetOrganizations("main");

                    if(tml_orf.Balance - model.Budget < 0)
                    {
                        ModelState.AddModelError("", "Недостаточность финансирования.");
                        return View(model);
                    }

                    tml_orf.Balance -= model.Budget;

                    await _bankServices.UpdateOrganization(tml_orf);
                    await _bankServices.AddToSpentStat(model.Budget);
                    await _bankServices.SpentMoney(model.Budget);

                    var mainOrg = new Organization();

                    await _bankServices.CreateOrganization(mainOrg);

                    var gov = new GovermentPolitical
                    {
                        FreeOrganizationCreateCount = 15,
                        Name = model.GovermentName,
                        ImageCoverUrl = model.ImageCoverUrl,
                        Information = model.Information,
                        ImageFlagUrl = model.ImageFlagUrl,
                        LeaderId = userGov.Id,
                        TaxesForOrganization = 0,
                        MainOrganizationGovermentId = mainOrg.Id,
                        VKurl = model.VKurl
                    };

                    await _bankServices.CreateGoverment(gov);

                    mainOrg.Short_Desc = model.Information;
                    mainOrg.AdminId = userGov.Id;
                    mainOrg.Balance = 0;
                    mainOrg.ImageUrl = model.ImageFlagUrl;
                    mainOrg.Influence = 0;
                    mainOrg.isBuy = false;
                    mainOrg.isZacrep = false;
                    mainOrg.VkUrl = model.VKurl;
                    mainOrg.Name = model.GovermentName;
                    mainOrg.SpecialId = "goverment_org";
                    mainOrg.GovermentId = gov.Id;
                    mainOrg.Status = "status_ok";
                    mainOrg.Balance = model.Budget;

                    await _bankServices.CreateBankTransaction(new BankTransaction
                    {
                        Date = _bankServices.NowDateTime(),
                        Id1 = -1,
                        BankId2 = mainOrg.Id,
                        Text = "Облигации ТМЛ",
                        Value = model.Budget
                    });

                    await _bankServices.UpdateOrganization(mainOrg);

                    return RedirectToAction("SingleGoverment", "Bank", new { id = gov.Id });
                }

                ModelState.AddModelError("", "Пользователя с именем '" + model.AdminName + "' не существует.");
                return View(model);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("gov/getNalog/{id}")]
        public async Task<IActionResult> GetGovermentTaxes(int id)
        {
            var gov = await _bankServices.GetGoverment(id);
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (gov.LeaderId == user.Id || perm.EditGoverment)
            {
                return View(gov);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("gov/getNalog/{id}")]
        public async Task<IActionResult> GetGovermentTaxes(GovermentPolitical model, int id)
        {
            var user = await _bankServices.GetUser();

            var perm = await _bankServices.GetUserPermission(user);

            var gov = await _bankServices.GetGoverment(id);

            if (gov.LeaderId == user.Id || perm.EditGoverment)
            {
                if (gov.DaysGovermentTaxes == 0)
                {
                    var orgList = await _bankServices.GetGovermentOrganization(gov.Id);
                    var mainOrg = await _bankServices.GetOrganizations(gov.MainOrganizationGovermentId);

                    var money = 0;

                    foreach (var s in orgList.Where(m => m.Id != gov.MainOrganizationGovermentId).ToList())
                    {
                        if (s.Balance > 0)
                        {
                            double percent = (s.Balance / 100) * gov.TaxesForOrganization;

                            s.Balance -= Convert.ToInt32(percent);
                            mainOrg.Balance += Convert.ToInt32(percent);
                            money += Convert.ToInt32(percent);

                            await _bankServices.UpdateOrganization(s);

                            await _bankServices.CreateBankTransaction(new BankTransaction
                            {
                                Date = _bankServices.NowDateTime(),
                                BankId1 = s.Id,
                                Id2 = -1,
                                Text = "Государственный налог ( " + gov.TaxesForOrganization + "% )",
                                Value = Convert.ToInt32(percent)
                            });
                        }
                    }

                    gov.DaysGovermentTaxes = 3;

                    await _bankServices.UpdateOrganization(mainOrg);
                    await _bankServices.CreateBankTransaction(new BankTransaction
                    {
                        Date = _bankServices.NowDateTime(),
                        Id1 = -1,
                        BankId2 = mainOrg.Id,
                        Text = "Государственный налог ( " + gov.TaxesForOrganization + "% )",
                        Value = Convert.ToInt32(money)
                    });

                    await _bankServices.UpdateGoverment(gov);

                    return RedirectToAction("SingleGoverment", "Bank", new { id = gov.Id });
                }

                ModelState.AddModelError("", "Вы не можете взымать налог, пока не пройдет дней: " + gov.DaysGovermentTaxes);
                return View(gov);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("editGov/{id}")]
        public async Task<IActionResult> EditGovermentPolitical(int id)
        {
            var gov = await _bankServices.GetGoverment(id);
            var user = await _bankServices.GetUser();

            var perm = await _bankServices.GetUserPermission(user);

            if (gov.LeaderId == user.Id || perm.EditGoverment)
            {
                var leaderAdminGov = await _bankServices.FindByIdAsync(gov.LeaderId);

                var model = new EditGovermentPolitical
                {
                    FreeOrganizationCreateCount = gov.FreeOrganizationCreateCount,
                    Id = gov.Id,
                    Information = gov.Information,
                    LeaderName = leaderAdminGov.Name,
                    Name = gov.Name,
                    TaxesForOrganization = gov.TaxesForOrganization,
                    VKurl = gov.VKurl
                };

                return View(model);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("editGov/{id}")]
        public async Task<IActionResult> EditGovermentPolitical(EditGovermentPolitical model)
        {
            if (ModelState.IsValid)
            {
                var gov = await _bankServices.GetGoverment(model.Id);
                var user = await _bankServices.GetUser();

                var perm = await _bankServices.GetUserPermission(user);

                if (gov.LeaderId == user.Id || perm.EditGoverment)
                {
                    var newleaderAdminGov = await _bankServices.FindByNameAsync(model.LeaderName);

                    if (newleaderAdminGov != null)
                    {
                        if (model.TaxesForOrganization is >= 0 and <= 30)
                        {
                            if (model.ImageUrlCover != null || !string.IsNullOrEmpty(model.ImageStringUrlCover))
                            {
                                if (model.ImageUrlCover != null)
                                {
                                    if (model.ImageUrlCover.Length > 10485760) // 10mb
                                    {
                                        ModelState.AddModelError("", "Файл " + model.ImageUrlCover.FileName + " имеет неверный размер.");
                                        return View(model);
                                    }

                                    if (!model.ImageUrlCover.IsImage())
                                    {
                                        ModelState.AddModelError("", "Файл " + model.ImageUrlCover.FileName + " имеет неверный формат.");
                                        return View(model);
                                    }

                                    var img = await _bankServices.CreateImageSys(_appEnvironment, model.ImageUrlCover, "userImages", -1);

                                    gov.ImageCoverUrl = img.ScreePath;
                                }
                                else gov.ImageCoverUrl = model.ImageStringUrlCover;
                            }

                            if (model.ImageUrlFlag != null || !string.IsNullOrEmpty(model.ImageStringUrlFlag))
                            {
                                if (model.ImageUrlFlag != null)
                                {
                                    if (model.ImageUrlFlag.Length > 10485760) // 10mb
                                    {
                                        ModelState.AddModelError("", "Файл " + model.ImageUrlFlag.FileName + " имеет неверный размер.");
                                        return View(model);
                                    }

                                    if (!model.ImageUrlCover.IsImage())
                                    {
                                        ModelState.AddModelError("", "Файл " + model.ImageUrlFlag.FileName + " имеет неверный формат.");
                                        return View(model);
                                    }

                                    var img = await _bankServices.CreateImageSys(_appEnvironment, model.ImageUrlFlag, "userImages", -1);

                                    gov.ImageFlagUrl = img.ScreePath;
                                }
                                else gov.ImageFlagUrl = model.ImageStringUrlFlag;
                            }

                            gov.Information = model.Information;
                            gov.Name = model.Name;
                            gov.LeaderId = newleaderAdminGov.Id;
                            gov.VKurl = model.VKurl;
                            gov.TaxesForOrganization = model.TaxesForOrganization;

                            if (perm.EditGoverment)
                            {
                                gov.FreeOrganizationCreateCount = model.FreeOrganizationCreateCount;
                            }

                            await _bankServices.UpdateGoverment(gov);

                            return RedirectToAction("SingleGoverment", "Bank", new { id = gov.Id });
                        }

                        ModelState.AddModelError("", "Максимальное количество число налога: 30.");
                        return View(model);
                    }

                    ModelState.AddModelError("", "Имя '" + model.LeaderName + "' не существует.");
                    return View(model);
                }

                return RedirectToAction("Error", "Bank", new { code = 700 });
            }

            return View(model);
        }

        [HttpGet("gov/{id}")]
        public async Task<IActionResult> SingleGoverment(int id)
        {
            if (id > 0)
            {
                var gov = await _bankServices.GetGoverment(id);

                if (gov != null)
                {
                    var orgs = await _bankServices.GetGovermentOrganization(id);

                    var model = new SingleGov
                    {
                        GovermentPolitical = gov,
                        OrganizationsList = orgs.OrderByDescending(m => m.Balance).ToList()
                    };

                    return View(model);
                }

                return RedirectToAction("Error", "Bank", new { code = 400 });
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }


        [HttpPost("user/removeDeposit")]
        [Authorize]
        public async Task<IActionResult> RemoveDeposit(DepostModel model)
        {
            var user = await _bankServices.GetUser();

            //if (!_bankServices.UserHavePremium(user))
            //{
                //ModelState.AddModelError("", "Функуция доступна только премиум пользователям.");
                //return View(model);
            //}

            if (model.Col < 0)
            {
                ModelState.AddModelError("", "Неверное количество денег.");
                return View(model);
            }

            var dep = await _bankServices.GetDeposit(user);

            if (dep == null)
            {
                return RedirectToAction("MyDeposit", "Bank", new { id = user.Id });
            }

            if (model.Col > dep.Money)
            {
                ModelState.AddModelError("", "Недостаточно денег.");
                return View(model);
            }

            user.Money += model.Col;

            await _bankServices.UpdateUser(user);

            await _bankServices.CreateTransaction(new Transaction
            {
                Date = _bankServices.NowDateTime(),
                Id1 = -2,
                Id2 = user.Id,
                Text = "Снятие с депозита",
                Value = model.Col
            });

            dep.Money -= model.Col;
            await _bankServices.SaveDeposit(dep);

            return RedirectToAction("MyDeposit", "Bank", new { id = user.Id });
        }

        [HttpGet("admin/updateArticle/{id}")]
        [Authorize]
        public async Task<IActionResult> EditArticle(int id)
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (perm.CreateArticles)
            {
                var item = await _bankServices.GetArticles(id);

                if(item != null)
                {
                    return View(new ArticlesModel
                    {
                        HtmlText = item.HtmlText,
                        MiniText = item.MiniText
                    });
                }
                return RedirectToAction("Error", "Bank", new { id = 404 });
            }

            return RedirectToAction("Error", "Bank", new { id = 700 });
        }

        [HttpPost("admin/updateArticle/{id}")]
        [Authorize]
        public async Task<IActionResult> EditArticle(ArticlesModel model, int id)
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (perm.CreateArticles)
            {
                var item = await _bankServices.GetArticles(id);

                item.MiniText = model.MiniText;
                item.HtmlText = model.HtmlText;

                if (model.ImageUrl != null || !string.IsNullOrEmpty(model.ImageUrlString))
                {
                    if (model.ImageUrl != null)
                    {
                        if (model.ImageUrl.Length > 10485760) // 10mb
                        {
                            ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный размер.");
                            return View(model);
                        }

                        if (!model.ImageUrl.IsImage())
                        {
                            ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный формат.");
                            return View(model);
                        }

                        var img = await _bankServices.CreateImageSys(_appEnvironment, model.ImageUrl, "userImages", -1);

                        item.ImageUrl = img.ScreePath;
                    }
                    else item.ImageUrl = model.ImageUrlString;
                }

                await _bankServices.UpdateArticles(item);
                return RedirectToAction("SingleArticle", "Bank", new { id = id });
            }

            return RedirectToAction("Error", "Bank", new { id = 700 });
        }

        [HttpPost("user/addDeposit")]
        [Authorize]
        public async Task<IActionResult> AddDeposit(DepostModel model)
        {
            var user = await _bankServices.GetUser();

            if (!_bankServices.UserHavePremium(user))
            {
                ModelState.AddModelError("", "Функуция доступна только премиум пользователям.");
                return View(model);
            }

            if (model.Col <= 0)
            {
                ModelState.AddModelError("", "Неверное количество денег.");
                return View(model);
            }

            if (model.Col > user.Money)
            {
                ModelState.AddModelError("", "Недостаточно денег.");
                return View(model);
            }

            user.Money -= model.Col;

            await _bankServices.SpentMoney(model.Col);

            await _bankServices.UpdateUser(user);

            await _bankServices.CreateTransaction(new Transaction
            {
                Date = _bankServices.NowDateTime(),
                Id1 = user.Id,
                Id2 = -2,
                Text = "Взнос в депозит",
                Value = model.Col
            });

            var dep = await _bankServices.GetDeposit(user);

            if (dep == null)
            {
                await _bankServices.CreateDeposit(new Deposit
                {
                    Money = model.Col,
                    UserId = user.Id
                });
            }
            else
            {
                dep.Money += model.Col;
                await _bankServices.SaveDeposit(dep);
            }
            return RedirectToAction("MyDeposit", "Bank", new { id = user.Id });
        }

        [HttpGet("gov/stat")]
        public IActionResult Statistics()
        {
            var list = _bankServices.GetStatisticsList(20);
            return View(list);
        }

        [HttpGet("user/sendOrgToOrg/{id}")]
        [Authorize]
        public async Task<IActionResult> SendOrgToOrg(int id)
        {
            var user = await _bankServices.GetUser();

            var org = await _bankServices.GetOrganizations(id);

            var perm = await _bankServices.GetUserPermission(user);

            if (org != null)
            {
                if (perm.ChangeOrganizationEconomy || org.AdminId == user.Id || org.Zam1Name == user.Id || org.Zam2Name == user.Id)
                {
                    return View(new TransferOrgToOrgModel
                    {
                        orgId = org.Id
                    });
                }

                return RedirectToAction("Error", "Bank", new { code = 700 });
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        [HttpPost("user/sendOrgToOrg/{id}")]
        [Authorize]
        public async Task<IActionResult> SendOrgToOrg(int id, TransferOrgToOrgModel model)
        {
            var user = await _bankServices.GetUser();

            var org = await _bankServices.GetOrganizations(model.orgId);

            var perm = await _bankServices.GetUserPermission(user);

            if (org != null)
            {
                if (perm.ChangeOrganizationEconomy || org.AdminId == user.Id || org.Zam1Name == user.Id || org.Zam2Name == user.Id)
                {
                    var search = (await _bankServices.GetOrganizations()).FirstOrDefault(m => m.Name.ToLower() == model.OrgName.ToLower());

                    if(search == null)
                    {
                        ModelState.AddModelError("", "Организации с именём '" + search.Name + "' не существует.");
                        return View(model);
                    }

                    if(model.Count > 0 && model.Count <= org.Balance)
                    {
                        org.Balance -= model.Count;
                        search.Balance += model.Count;

                        await _bankServices.CreateBankTransaction(new BankTransaction
                        {
                            Date = _bankServices.NowDateTime(),
                            BankId1 = org.Id,
                            Id2 = -1,
                            Text = "Безспроцентный перевод в организацию '" + search.Name + "'",
                            Value = model.Count
                        });

                        await _bankServices.CreateBankTransaction(new BankTransaction
                        {
                            Date = _bankServices.NowDateTime(),
                            BankId2 = search.Id,
                            Id1 = -1,
                            Text = "Безспроцентный перевод из организации '" + org.Name + "'",
                            Value = model.Count
                        });

                        return RedirectToAction("SingleOrganization", "Bank", new { id = org.Id });

                    }

                    ModelState.AddModelError("", "Неверное число.");
                    return View(model);
                }

                return RedirectToAction("Error", "Bank", new { code = 700 });
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }


        [HttpGet("user/create_ticket")]
        [Authorize]
        public async Task<IActionResult> CreateTicket()
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (!(perm.CheckTickets && user.Id != 1))
            {
                return View(new CreateTicketModel());
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("upload_image_ckeditor")]
        public async Task<IActionResult> UploadImage(IFormFile upload)
        {

            if (upload != null)
            {
                if (upload.Length > 10485760) // 10mb
                {
                    return Json(new
                    {
                        uploaded = 0,
                        error = new
                        {
                            message = "Файл слишком большой!"
                        }
                    });
                }

                if (!upload.IsImage())
                {
                    return Json(new
                    {
                        uploaded = 0,
                        error = new
                        {
                            message = "Это не изоображение!"
                        }
                    });
                }

                var img = await _bankServices.CreateImageSys(_appEnvironment, upload, "userImagesStatic", -1);
                return Json(new
                {
                    uploaded = 1,
                    fileName = img.GenerateName,
                    url = img.ScreePath
                });
            }

            return Json(new
            {
                uploaded = 0,
                error = new
                {
                    message = "Ошибка сервера."
                }
            });
        }


        [HttpPost("user/create_ticket")]
        [Authorize]
        public async Task<IActionResult> CreateTicket(CreateTicketModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _bankServices.GetUser();

                if (model.Value <= 0)
                {
                    ModelState.AddModelError("", "Неверное количество денег.");
                    return View(model);
                }


                var ticket = new Ticket
                {
                    Status = "status_timing",
                    Text = model.Text,
                    UserId = user.Id,
                    Value = model.Value,
                    Date = _bankServices.NowDateTime()
                };

                if (model.ImageUrl != null || !string.IsNullOrEmpty(model.ImageStringUrl))
                {
                    if (model.ImageUrl.Any())
                    {
                        if (model.ImageUrl.Count() > 5)
                        {
                            ModelState.AddModelError("", "Максимальное количество файлов - 5.");
                            return View(model);
                        }

                        var str = "";
                        foreach (var image in model.ImageUrl)
                        {
                            if (image.Length > 10485760) // 10mb
                            {
                                ModelState.AddModelError("", "Файл " + image.FileName + " имеет неверный размер.");
                                return View(model);
                            }

                            if (!image.IsImage())
                            {
                                ModelState.AddModelError("", "Файл " + image.FileName + " имеет неверный формат.");
                                return View(model);
                            }

                            var img = await _bankServices.CreateImageSys(_appEnvironment, image, "userImagesTicket", 15); // 15 days
                            str += img.ScreePath + ';';
                        }

                        ticket.Images = str;
                    }
                    else ticket.Images = string.Join(";", model.ImageStringUrl.Split(';').Where(m => !string.IsNullOrEmpty(m)).ToList()) + ";";
                }

                await _bankServices.CreateTicket(ticket);


                return RedirectToAction("MyTickets", "Bank", new { id = user.Id });
            }
            return View(model);
        }

        [HttpGet("admin/reg_code")]
        [Authorize]
        public async Task<IActionResult> GenRegCode() // TODO: Dodeltab
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (perm.GenerateRegCodes)
            {
                return View();
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("admin/check_item_tickets")]
        [Authorize]
        public async Task<IActionResult> CheckItemTickets()
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);
            if (perm.CheckItems)
            {
                var ticket = await _bankServices.GetAllItems();
                ticket = ticket.Where(m => m.isActived == false).ToList();

                var model = new CheckItemTicketModel { };

                if (ticket.Count > 0)
                {

                    model = new CheckItemTicketModel
                    {
                        Col = ticket.Count(),
                        shopItem = ticket.Last()
                    };
                }
                else
                {
                    model = new CheckItemTicketModel
                    {
                        Col = ticket.Count(),
                        shopItem = null
                    };
                }

                return View(model);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("admin/check_item_tickets")]
        [Authorize]
        public async Task<IActionResult> CheckItemTickets(CheckItemTicketModel model)
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);
            if (perm.CheckItems)
            {
                var item = await _bankServices.GetShopItem(model.shopItem.Id);

                if (model.IsActive == "Разрешено")
                {

                    item.ImageUrl = model.shopItem.ImageUrl;
                    item.Name = model.shopItem.Name;
                    item.Price = model.shopItem.Price;
                    item.Short_Desc = model.shopItem.Short_Desc;
                    item.Type = model.shopItem.Type;
                    item.Value = model.shopItem.Value;
                    item.isCaseItem = model.shopItem.isCaseItem;
                    item.RarePoint = model.shopItem.RarePoint;
                    item.RareType = model.shopItem.RareType;
                    item.CreateNum = model.shopItem.CreateNum;

                    var org = await _bankServices.GetOrganizations(model.shopItem.OrgId);

                    if(org.Balance < item.Value * item.CreateNum)
                    {
                        ModelState.AddModelError("", "В организации не хватает денег для создание товаров.");
                        return View(model);
                    }

                    org.Balance -= item.Value * item.CreateNum;
                    await _bankServices.UpdateOrganization(org);

                    await _bankServices.CreateBankTransaction(new BankTransaction
                    {
                        Date = _bankServices.NowDateTime(),
                        BankId1 = org.Id,
                        Id2 = -1,
                        Text = "Создание товара '" + model.shopItem.Name + "'",
                        Value = item.Value * item.CreateNum
                    });

                    var find = await _bankServices.FindByIdAsync(model.shopItem.AuthorId);

                    find.newViewEntity++;

                    await _bankServices.UpdateUser(find);

                    await _bankServices.CreateEntityTicket(new EntityTicketInformation
                    {
                        Date = _bankServices.NowDateTime(),
                        Status = "status_ok",
                        UserId = model.shopItem.AuthorId,
                        Text = "Заявка на товар: '" + model.shopItem.Name + "'",
                        AdminInformation = model.AdminInformation
                    });


                    item.isActived = true;

                    await _bankServices.UpdateItem(item);
                }
                else
                {
                    var find = await _bankServices.FindByIdAsync(model.shopItem.AuthorId);

                    find.newViewEntity++;

                    await _bankServices.UpdateUser(find);

                    await _bankServices.CreateEntityTicket(new EntityTicketInformation
                    {
                        Date = _bankServices.NowDateTime(),
                        Status = "status_declaim",
                        UserId = model.shopItem.AuthorId,
                        Text = "Заявка на товар: '" + model.shopItem.Name + "'",
                        AdminInformation = model.AdminInformation
                    });

                    await _bankServices.DeleteItem(model.shopItem);
                }

                return RedirectToAction("CheckItemTickets", "Bank");
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("admin/check_tickets")]
        [Authorize]
        public async Task<IActionResult> CheckTickets()
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (perm.CheckTickets)
            {
                var ticket = await _bankServices.GetLastTicket();

                var model = new CheckTicketModel { };

                if (ticket != null)
                {
                    var userT = await _bankServices.FindByIdAsync(ticket.UserId);

                    if(userT == null)
                    {
                        model.ticket = null;
                        return View(model);
                    }

                    model.ticket = ticket;
                    model.user = userT;

                    if(ticket.Images != null)
                        model.ImagesUrl = ticket.Images.Remove(ticket.Images.Length - 1).Split(';').ToList();
                }
                else
                {
                    model.ticket = null;
                }

                return View(model);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("admin/check_tickets")]
        [Authorize]
        public async Task<IActionResult> CheckTickets(CheckTicketModel model)
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (perm.CheckTickets)
            {
                model.ticket = await _bankServices.FindTicketById(model.ticket.Id);
                if (model.ticket.Status == "status_timing")
                {
                    if (model.NewStatus == "Разрешить")
                    {
                        if (model.NewValue < 0)
                        {
                            ModelState.AddModelError("", "Неверное количество денег.");
                            return View(model);
                        }

                        model.ticket.Status = "status_ok";
                        model.ticket.AdminComment = model.AdminComment;

                        var userTicket = await _bankServices.FindByIdAsync(model.ticket.UserId);

                        if (model.NewValue == 0 || model.NewValue == model.ticket.Value)
                        {
                            if (model.OutBank == true)
                            {
                                var org = await _bankServices.GetOrganizations("main");

                                if (org.Balance < model.ticket.Value)
                                {
                                    ModelState.AddModelError("", "На балансе организации нехватает денег.");
                                    return View(model);
                                }

                                org.Balance -= model.ticket.Value;
                                await _bankServices.SpentMoney(model.ticket.Value);

                                await _bankServices.CreateBankTransaction(new BankTransaction
                                {
                                    BankId1 = org.Id,
                                    Id2 = model.ticket.UserId,
                                    Date = _bankServices.NowDateTime(),
                                    Text = "Одобренное банком пополнение.",
                                    Value = model.ticket.Value
                                });

                                await _bankServices.UpdateOrganization(org);

                            }

                            await _bankServices.AddToSpentStat(model.ticket.Value);

                            userTicket.Money += model.ticket.Value;

                            await _bankServices.UpdateUser(userTicket);

                            await _bankServices.CreateTransaction(new Transaction
                            {
                                Date = _bankServices.NowDateTime(),
                                Id1 = -1,
                                Id2 = model.ticket.UserId,
                                Text = "Одобренное " + ((model.OutBank) ? "банком" : "администрацией") + " пополнение.",
                                Value = model.ticket.Value
                            });
                        }
                        else
                        {
                            if (model.OutBank == true)
                            {
                                var org = await _bankServices.GetOrganizations("main");

                                if (org.Balance < model.NewValue)
                                {
                                    ModelState.AddModelError("", "На балансе организации нехватает денег.");
                                    return View(model);
                                }

                                await _bankServices.SpentMoney(model.ticket.Value);

                                org.Balance -= model.ticket.Value;

                                await _bankServices.CreateBankTransaction(new BankTransaction
                                {
                                    BankId1 = org.Id,
                                    Id2 = model.ticket.UserId,
                                    Date = _bankServices.NowDateTime(),
                                    Text = "Одобренное банком пополнение.",
                                    Value = model.NewValue
                                });

                                await _bankServices.UpdateOrganization(org);

                            }

                            userTicket.Money += model.NewValue;

                            await _bankServices.AddToSpentStat(model.NewValue);

                            await _bankServices.UpdateUser(userTicket);

                            await _bankServices.CreateTransaction(new Transaction
                            {
                                Date = _bankServices.NowDateTime(),
                                Id1 = -1,
                                Id2 = model.ticket.UserId,
                                Text = "Одобренное администрацией поощрение.",
                                Value = model.NewValue
                            });

                            model.ticket.Value = model.NewValue;
                        }

                        await _bankServices.SaveTicket(model.ticket);

                    }
                    else if (model.NewStatus == "Отказать")
                    {
                        model.ticket.Status = "status_declaim";
                        model.ticket.AdminComment = model.AdminComment;
                        await _bankServices.SaveTicket(model.ticket);
                    }
                    else
                    {
                        ModelState.AddModelError("", "Выберите: отказ / одобрено.");
                        return View(model);
                    }
                }
                return RedirectToAction("CheckTickets", "Bank");
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }


        [HttpPost("user/transfer")]
        [Authorize]
        public async Task<IActionResult> Transfer(TransferModel model)
        {
            var userTo = await _bankServices.FindByNameAsync(model.NameTo);
            var userIn = await _bankServices.GetUser();

            if (model.Value <= 0)
            {
                ModelState.AddModelError("", "Неверное количество денег.");
                return View(model);
            }

            var stavka = await _bankServices.GetStavka();
            var nalog = stavka;

            if (model.Value > 100)
            {
                nalog = (int)((model.Value / 100) * stavka);
            }

            if (model.Value < stavka)
            {
                ModelState.AddModelError("", "Минимум " + stavka + " Мемлар!");
                return View(model);
            }

            if (model.Value + nalog > userIn.Money)
            {
                ModelState.AddModelError("", "Недостаточно средств.");
                return View(model);
            }


            if (userTo != null)
            {
                if (userTo.IsArrested)
                {
                    ModelState.AddModelError("", "Пользователь аррестован.");
                    return View(model);
                }

                if (userTo.Name == userIn.Name)
                {
                    ModelState.AddModelError("", "Самому себе нельзя.");
                    return View(model);
                }

                await _bankServices.AddMoneyToGov((int)nalog);

                var trans = new Transaction
                {
                    Date = _bankServices.NowDateTime(),
                    Id1 = userIn.Id,
                    Id2 = userTo.Id,
                    Text = model.Comment,
                    Value = model.Value
                };


                userIn.Money -= model.Value + (int)nalog;
                userTo.Money += model.Value;

                await _bankServices.SpentMoney(model.Value + (int)nalog);

                await _bankServices.AddToRecdStat((int)nalog);

                await _bankServices.UpdateUser(userTo);
                await _bankServices.UpdateUser(userIn);

                await _bankServices.CreateTransaction(trans);

                return RedirectToAction("TransferComplete", "Bank");
            }

            ModelState.AddModelError("", "Получателя с именем " + model.NameTo + " не существует.");

            return View(model);
        }

        [HttpGet("user/balance/{id}")]
        public async Task<IActionResult> Balance(int id)
        {
            var user = await _bankServices.FindByIdAsync(id);

            if (user != null)
            {
                var userVied = await _bankServices.GetUser();

                if (userVied != null)
                {
                    if (userVied.Id == id)
                    {
                        user.NonViewTrans = false;
                        await _bankServices.UpdateUser(user);
                    }
                }


                var list = _bankServices.GetTransactions(user, 10);

                var model = new BalanceModel
                {
                    user = user,
                    TransList = list
                };

                return View(model);
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        [HttpGet("editItem/{id}")]
        public async Task<IActionResult> EditItem(int id)
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);
            var item = await _bankServices.GetShopItem(id);

            if (perm.EditItem)
                return View(item);

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("editItem/{id}")]
        public async Task<IActionResult> EditItem(ShopItem model, int id)
        {
            var item = await _bankServices.GetShopItem(model.Id);

            item.ImageUrl = model.ImageUrl;
            item.Name = model.Name;
            item.Price = model.Price;
            item.Short_Desc = model.Short_Desc;
            item.Value = model.Value;
            item.Type = model.Type;

            await _bankServices.UpdateItem(item);

            return RedirectToAction("AllItems", "Bank");
        }

        [HttpPost("deleteShopItem/{id}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);
            var item = await _bankServices.GetShopItem(id);

            if (perm.DeleteItem)
            {
                await _bankServices.DeleteItem(item);

                return RedirectToAction("AllItems", "Bank");
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("createItemComplete")]
        public IActionResult CreateItemComplete()
        {
            return View();
        }

        [HttpPost("admin/create_lot/{id}")]
        public async Task<IActionResult> CreateItem(CreateItemModel model, int id)
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            var org = await _bankServices.GetOrganizations(id);


            if (perm.CreateItemOrg || user.Id == org.AdminId || user.Id == org.Zam2Name || user.Id == org.Zam1Name)
            {
                var item = new ShopItem
                {
                    Price = model.Price,
                    Short_Desc = model.Short_Desc,
                    Value = model.Count,
                    Name = model.Name,
                    Type = model.Type,
                    AuthorId = user.Id,
                    OrgId = model.Id,
                    CreateNum = model.CreateNum
                };

                if(model.CreateNum * 1.2 > model.Price || model.CreateNum <= 0 || model.Price <= 0 || model.CreateNum * model.Count > org.Balance)
                {
                    ModelState.AddModelError("", "Неверная себестоимость или стоимость товара.");
                    return View(model);
                }

                if (model.ImageUrl != null || !string.IsNullOrEmpty(model.ImageStringUrl))
                {

                    if (model.ImageUrl != null)
                    {
                        if (model.ImageUrl.Length > 10485760) // 10mb
                        {
                            ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный размер.");
                            return View(model);
                        }

                        if (!model.ImageUrl.IsImage())
                        {
                            ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный формат.");
                            return View(model);
                        }

                        var img = await _bankServices.CreateImageSys(_appEnvironment, model.ImageUrl, "userImagesStatic", -1);

                        item.ImageUrl = img.ScreePath;
                    }
                    else item.ImageUrl = model.ImageStringUrl;
                }
                else
                {
                    ModelState.AddModelError("", "Картинка не была установлена.");
                    return View(model);
                }

                await _bankServices.CreateShopItem(item);

                return RedirectToAction("CreateItemComplete", "Bank");
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("admin/create_lot/{id}")]
        public async Task<IActionResult> CreateItem(int id)
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            var org = await _bankServices.GetOrganizations(id);


            if (perm.CreateItemOrg || org.AdminId == user.Id || user.Id == org.Zam2Name || user.Id == org.Zam1Name)
            {
                var model = new CreateItemModel
                {
                    Id = id
                };

                return View(model);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("user/login")]
        public async Task<IActionResult> Login()
        {
            var user = await _bankServices.GetUser();

            if (user == null)
                return View(new LoginModel());
            return RedirectToAction("Index", "Bank");

        }

        // proj

        [HttpGet("admin/check_prjects")]
        [Authorize]
        public async Task<IActionResult> CheckEntityProject()
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);
            if (perm.CheckProjects)
            {
                var list = await _bankServices.GetAllProjectEntity();

                if (list is { Count: > 0 })
                {
                    var proj = list.First();
                    var lastTicketUser = await _bankServices.GetEntityTicketInformation(proj.CreatorId);

                    var model = new AdminEntityProjectTicket
                    {
                        image = proj.ImageUrl,
                        Information = proj.Information,
                        isOk = false,
                        lastTickets = lastTicketUser,
                        ProjectName = proj.ProjectName,
                        Target = proj.Target,
                        AuthorId = proj.CreatorId,
                        ProjId = proj.Id,
                        AdminInformation = ""
                    };

                    return View(model);
                }

                return View(null);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("admin/check_prjects")]
        [Authorize]
        public async Task<IActionResult> CheckEntityProject(AdminEntityProjectTicket model)
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);
            if (perm.CheckProjects)
            {
                if(model.isOk)
                {
                    await _bankServices.CreateProject(new Infrastructure.Project
                    {
                        AuthorId = model.AuthorId,
                        Balance = 0,
                        ImageUrl = model.image,
                        Info = model.Information,
                        Name = model.ProjectName,
                        Target = model.Target
                    });

                    await _bankServices.CreateEntityTicket(new EntityTicketInformation
                    {
                        Date = _bankServices.NowDateTime(),
                        Status = "status_ok",
                        UserId = model.AuthorId,
                        Text = "Заявка на создание проекта: '" + model.ProjectName + "'",
                        AdminInformation = model.AdminInformation
                    });

                    var projUser = await _bankServices.FindByIdAsync(model.AuthorId);
                    projUser.newViewEntity++;
                    await _bankServices.UpdateUser(projUser);

                    await _bankServices.DeleteEntityProject(model.ProjId);

                    return RedirectToAction("CheckEntityProject", "Bank");
                }
                else
                {
                    await _bankServices.CreateEntityTicket(new EntityTicketInformation
                    {
                        Date = _bankServices.NowDateTime(),
                        Status = "status_declaim",
                        UserId = model.AuthorId,
                        Text = "Заявка на создание проекта: '" + model.ProjectName + "'",
                        AdminInformation = model.AdminInformation
                    });

                    var projUser = await _bankServices.FindByIdAsync(model.AuthorId);
                    projUser.newViewEntity++;
                    await _bankServices.UpdateUser(projUser);

                    await _bankServices.DeleteEntityProject(model.ProjId);

                    return RedirectToAction("CheckEntityProject", "Bank");
                }
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("user/createProjectTicket")]
        public async Task<IActionResult> CreateTicketProject()
        {
            var user = await _bankServices.GetUser();
            var lasTicket = await _bankServices.GetEntityTicketInformation(user.Id);
            
            if(user != null)
            {
                user.newViewEntity = 0;

                await _bankServices.UpdateUser(user);

                return View(new EntityTicketProjectModel
                { 
                    lastTickets = lasTicket
                });
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("user/createProjectTicket")]
        public async Task<IActionResult> CreateTicketProject(EntityTicketProjectModel model)
        {
            var user = await _bankServices.GetUser();

            if (user != null)
            {
                if(model.ticketProj.Target <= 0 
                    || string.IsNullOrEmpty(model.ticketProj.ProjectName)
                    || string.IsNullOrEmpty(model.ticketProj.Information))
                {
                    ModelState.AddModelError("", "Ошибка в заполнении полей.");
                    return View(model);
                }

                var proj = new EntityTicketProject
                {
                    CreatorId = user.Id,
                    Information = model.ticketProj.Information,
                    ProjectName = model.ticketProj.ProjectName,
                    Target = model.ticketProj.Target
                };

                if (model.ImageUrl != null || !string.IsNullOrEmpty(model.ImageStringUrl))
                {
                    if (model.ImageUrl != null)
                    {
                        if (model.ImageUrl.Length > 10485760) // 10mb
                        {
                            ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный размер.");
                            return View(model);
                        }

                        if (!model.ImageUrl.IsImage())
                        {
                            ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный формат.");
                            return View(model);
                        }

                        var img = await _bankServices.CreateImageSys(_appEnvironment, model.ImageUrl, "userImages", -1);

                        proj.ImageUrl = img.ScreePath;
                    }
                    else proj.ImageUrl = model.ImageStringUrl;
                }
                else
                {
                    ModelState.AddModelError("", "Не выбран файл.");
                    return View(model);
                }

                await _bankServices.CreateEntityProject(proj);
                await _bankServices.CreateEntityTicket(new EntityTicketInformation
                {
                    Date = _bankServices.NowDateTime(),
                    Status = "status_timing",
                    Text = "Заявка на создание проекта: '" + proj.ProjectName + "'",
                    UserId = user.Id
                });

                return RedirectToAction("Index", "Bank");

            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        // org

        [HttpGet("admin/check_organizations")]
        [Authorize]
        public async Task<IActionResult> CheckEntityOrganization()
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);
            if (perm.CheckOrganization)
            {
                var list = await _bankServices.GetAllOrganizationEntity();

                if (list is { Count: > 0 })
                {
                    var org = list.First();
                    var lastTicketUser = await _bankServices.GetEntityTicketInformation(org.CreatorId);

                    var model = new AdminEntityOrganizationTicket
                    {
                        image = org.ImageUrl,
                        isOk = false,
                        lastTickets = lastTicketUser,
                        AuthorId = org.CreatorId,
                        AdminInformation = "",
                        Information = org.Information,
                        Name = org.Name,
                        VKUrl = org.VKUrl,
                        OrgId = org.Id
                    };

                    return View(model);
                }

                return View(null);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("admin/check_organizations")]
        [Authorize]
        public async Task<IActionResult> CheckEntityOrganization(AdminEntityOrganizationTicket model)
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);
            if (perm.CheckOrganization)
            {
                if (model.isOk)
                {
                    await _bankServices.CreateOrganization(new Organization
                    {
                        AdminId = model.AuthorId,
                        Balance = 0,
                        Influence = model.Influence,
                        Name = model.Name,
                        Status = "status_ok",
                        Short_Desc = model.Information,
                        VkUrl = model.VKUrl,
                        isBuy = true,
                        GovermentId = -1,
                        ImageUrl = model.image
                    });

                    await _bankServices.CreateEntityTicket(new EntityTicketInformation
                    {
                        Date = _bankServices.NowDateTime(),
                        Status = "status_ok",
                        UserId = model.AuthorId,
                        Text = "Заявка на создание орагнизации: '" + model.Name + "'",
                        AdminInformation = model.AdminInformation
                    });

                    var projUser = await _bankServices.FindByIdAsync(model.AuthorId);
                    projUser.newViewEntity++;
                    await _bankServices.UpdateUser(projUser);

                    await _bankServices.DeleteEntityOrganization(model.OrgId);

                    return RedirectToAction("CheckEntityOrganization", "Bank");
                }
                else
                {
                    await _bankServices.CreateEntityTicket(new EntityTicketInformation
                    {
                        Date = _bankServices.NowDateTime(),
                        Status = "status_declaim",
                        UserId = model.AuthorId,
                        Text = "Заявка на создание орагнизации: '" + model.Name + "'",
                        AdminInformation = model.AdminInformation
                    });

                    var projUser = await _bankServices.FindByIdAsync(model.AuthorId);
                    projUser.newViewEntity++;
                    await _bankServices.UpdateUser(projUser);

                    await _bankServices.DeleteEntityOrganization(model.OrgId);

                    return RedirectToAction("CheckEntityProject", "Bank");
                }
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("user/createOrganizationTicket")]
        public async Task<IActionResult> CreateTicketOrganization()
        {
            var user = await _bankServices.GetUser();
            var lasTicket = await _bankServices.GetEntityTicketInformation(user.Id);

            if (user != null)
            {
                user.newViewEntity = 0;

                await _bankServices.UpdateUser(user);

                return View(new EntityTicketOrganizationModel
                {
                    lastTickets = lasTicket
                });
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("user/createOrganizationTicket")]
        public async Task<IActionResult> CreateTicketOrganization(EntityTicketOrganizationModel model)
        {
            var user = await _bankServices.GetUser();

            if (user != null)
            {
                var org = new EntityTicketOrganization
                {
                    CreatorId = user.Id,
                    ImageUrl = model.ticketOrg.ImageUrl,
                    Information = model.ticketOrg.Information,
                    Name = model.ticketOrg.Name,
                    VKUrl = model.ticketOrg.VKUrl
                };

                var lasTicket = await _bankServices.GetEntityTicketInformation(user.Id);
                model.lastTickets = lasTicket;

                if (model.ImageUrl != null || !string.IsNullOrEmpty(model.ImageStringUrl))
                {
                    if (model.ImageUrl != null)
                    {
                        if (model.ImageUrl.Length > 10485760) // 10mb
                        {
                            ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный размер.");
                            return View(model);
                        }

                        if (!model.ImageUrl.IsImage())
                        {
                            ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный формат.");
                            return View(model);
                        }

                        var img = await _bankServices.CreateImageSys(_appEnvironment, model.ImageUrl, "userImages", -1);

                        org.ImageUrl = img.ScreePath;
                    }
                    else org.ImageUrl = model.ImageStringUrl;
                }
                else
                {
                    ModelState.AddModelError("", "Не выбран файл.");
                    return View(model);
                }

                await _bankServices.CreateEntityOrganization(org);
                await _bankServices.CreateEntityTicket(new EntityTicketInformation
                {
                    Date = _bankServices.NowDateTime(),
                    Status = "status_timing",
                    Text = "Заявка на создание организации: '" + org.Name + "'",
                    UserId = user.Id
                });

                return RedirectToAction("Index", "Bank");

            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        // gov

        [HttpGet("admin/check_goverments")]
        [Authorize]
        public async Task<IActionResult> CheckEntityGoverment()
        {
            var user = await _bankServices.GetUser();

            var perm = await _bankServices.GetUserPermission(user);

            if (perm.CheckGoverments)
            {
                var list = await _bankServices.GetAllGovermentEntity();

                if (list is { Count: > 0 })
                {
                    var gov = list.First();
                    var lastTicketUser = await _bankServices.GetEntityTicketInformation(gov.CreatorId);

                    var model = new AdminEntityGovermentTicket
                    {
                        isOk = false,
                        lastTickets = lastTicketUser,
                        AdminInformation = "",
                        AuthorId = gov.CreatorId,
                        imageBG = gov.BGUrl,
                        imageFlag = gov.FlagUrl,
                        Information = gov.Information,
                        Name = gov.Name,
                        GovId = gov.Id,
                        VKUrl = gov.VKUrl
                    };

                    return View(model);
                }

                return View(null);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("admin/check_goverments")]
        [Authorize]
        public async Task<IActionResult> CheckEntityGoverment(AdminEntityGovermentTicket model)
        {
            var user = await _bankServices.GetUser();

            var perm = await _bankServices.GetUserPermission(user);

            if (perm.CheckGoverments)
            {
                if (model.isOk)
                {
                    var tml_orf = await _bankServices.GetOrganizations("main");

                    if (tml_orf.Balance - model.Budget < 0)
                    {
                        ModelState.AddModelError("", "Недостаточность финансирования.");
                        return View(model);
                    }

                    tml_orf.Balance -= model.Budget;

                    await _bankServices.UpdateOrganization(tml_orf);
                    await _bankServices.AddToSpentStat(model.Budget);
                    await _bankServices.SpentMoney(model.Budget);

                    var mainOrg = new Organization();

                    await _bankServices.CreateOrganization(mainOrg);

                    var gov = new GovermentPolitical
                    {
                        FreeOrganizationCreateCount = 15,
                        Name = model.Name,
                        ImageCoverUrl = model.imageBG,
                        Information = model.Information,
                        ImageFlagUrl = model.imageFlag,
                        LeaderId = model.AuthorId,
                        TaxesForOrganization = 0,
                        MainOrganizationGovermentId = mainOrg.Id,
                        VKurl = model.VKUrl
                    };

                    await _bankServices.CreateGoverment(gov);

                    mainOrg.Short_Desc = model.Information;
                    mainOrg.AdminId = model.AuthorId;
                    mainOrg.ImageUrl = model.imageFlag;
                    mainOrg.Influence = 0;
                    mainOrg.isBuy = false;
                    mainOrg.isZacrep = false;
                    mainOrg.VkUrl = model.VKUrl;
                    mainOrg.Name = model.Name;
                    mainOrg.SpecialId = "goverment_org";
                    mainOrg.GovermentId = gov.Id;
                    mainOrg.Status = "status_ok";
                    mainOrg.Balance = model.Budget;

                    await _bankServices.CreateBankTransaction(new BankTransaction
                    {
                        Date = _bankServices.NowDateTime(),
                        Id1 = -1,
                        BankId2 = mainOrg.Id,
                        Text = "Облигации ТМЛ",
                        Value = model.Budget
                    });

                    await _bankServices.UpdateOrganization(mainOrg);

                    //

                    await _bankServices.CreateEntityTicket(new EntityTicketInformation
                    {
                        Date = _bankServices.NowDateTime(),
                        Status = "status_ok",
                        UserId = model.AuthorId,
                        Text = "Заявка на создание государства: '" + model.Name + "'",
                        AdminInformation = model.AdminInformation
                    });

                    var projUser = await _bankServices.FindByIdAsync(model.AuthorId);
                    projUser.newViewEntity++;
                    await _bankServices.UpdateUser(projUser);

                    await _bankServices.DeleteEntityGoverment(model.GovId);

                    return RedirectToAction("CheckEntityGoverment", "Bank");
                }
                else
                {
                    await _bankServices.CreateEntityTicket(new EntityTicketInformation
                    {
                        Date = _bankServices.NowDateTime(),
                        Status = "status_declaim",
                        UserId = model.AuthorId,
                        Text = "Заявка на создание государства: '" + model.Name + "'",
                        AdminInformation = model.AdminInformation
                    });

                    var projUser = await _bankServices.FindByIdAsync(model.AuthorId);
                    projUser.newViewEntity++;
                    await _bankServices.UpdateUser(projUser);

                    await _bankServices.DeleteEntityGoverment(model.GovId);

                    return RedirectToAction("CheckEntityGoverment", "Bank");
                }
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("user/createGovermentTicket")]
        public async Task<IActionResult> CreateTicketGoverment()
        {
            var user = await _bankServices.GetUser();
            var lasTicket = await _bankServices.GetEntityTicketInformation(user.Id);

            if (user != null)
            {
                user.newViewEntity = 0;

                await _bankServices.UpdateUser(user);

                return View(new EntityTicketGovermentModel
                {
                    lastTickets = lasTicket
                });
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("user/createGovermentTicket")]
        public async Task<IActionResult> CreateTicketGoverment(EntityTicketGovermentModel model)
        {
            var user = await _bankServices.GetUser();

            if (user != null)
            {
                var gov = new EntityTicketGoverment
                {
                    CreatorId = user.Id,
                    Information = model.ticketGov.Information,
                    Name = model.ticketGov.Name,
                    VKUrl = model.ticketGov.VKUrl
                };

                if (model.ImageUrlBG != null || !string.IsNullOrEmpty(model.ImageStringUrlBG))
                {
                    if (model.ImageUrlBG != null)
                    {
                        if (model.ImageUrlBG.Length > 10485760) // 10mb
                        {
                            ModelState.AddModelError("", "Файл " + model.ImageUrlBG.FileName + " имеет неверный размер.");
                            return View(model);
                        }

                        if (!model.ImageUrlBG.IsImage())
                        {
                            ModelState.AddModelError("", "Файл " + model.ImageUrlBG.FileName + " имеет неверный формат.");
                            return View(model);
                        }

                        var img = await _bankServices.CreateImageSys(_appEnvironment, model.ImageUrlBG, "userImages", -1);

                        gov.BGUrl = img.ScreePath;
                    }
                    else gov.BGUrl = model.ImageStringUrlBG;
                }
                else
                {
                    ModelState.AddModelError("", "Не выбран один из файлов ( фон ).");
                    return View(model);
                }

                if (model.ImageUrlFlag != null || !string.IsNullOrEmpty(model.ImageStringUrlFlag))
                {
                    if (model.ImageUrlFlag != null)
                    {
                        if (model.ImageUrlFlag.Length > 10485760) // 10mb
                        {
                            ModelState.AddModelError("", "Файл " + model.ImageUrlFlag.FileName + " имеет неверный размер.");
                            return View(model);
                        }

                        if (!model.ImageUrlFlag.IsImage())
                        {
                            ModelState.AddModelError("", "Файл " + model.ImageUrlFlag.FileName + " имеет неверный формат.");
                            return View(model);
                        }

                        var img = await _bankServices.CreateImageSys(_appEnvironment, model.ImageUrlFlag, "userImages", -1);

                        gov.FlagUrl = img.ScreePath;
                    }
                    else gov.FlagUrl = model.ImageStringUrlFlag;
                }
                else
                {
                    ModelState.AddModelError("", "Не выбран один из файлов ( флаг ).");
                    return View(model);
                }

                await _bankServices.CreateEntityGoverment(gov);
                await _bankServices.CreateEntityTicket(new EntityTicketInformation
                {
                    Date = _bankServices.NowDateTime(),
                    Status = "status_timing",
                    Text = "Заявка на создание государства: '" + gov.Name + "'",
                    UserId = user.Id
                });

                return RedirectToAction("Index", "Bank");

            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }


        [HttpGet("shop/single_item/{id}")]
        public async Task<IActionResult> SingleItem(int id, string mess, bool type)                  
        {
            var item = await _bankServices.GetShopItem(id);
            var user = await _bankServices.GetUser();
            var adm = (await _bankServices.UserIsInRole(user, "Administrator")) || (await _bankServices.UserIsInRole(user, "Owner"));

            if(user != null)
            {
                if (item != null)
                {
                    if ((item.isActived && item.isCaseItem == false) || adm)
                    {
                        var model = new ShopItemModel();

                        if (mess == null)
                        {
                            model = new ShopItemModel
                            {
                                shopItem = item,
                                Message = null,
                            };
                        }
                        else
                        {
                            model = new ShopItemModel
                            {
                                shopItem = item,
                                Message = mess,
                                TypeMessage = type
                            };
                        }

                        return View(model);
                    }

                    return RedirectToAction("Error", "Bank", new { code = 700 });
                }

                return RedirectToAction("Error", "Bank", new { code = 404 });
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        [HttpPost("getNalogTransfer/")]
        [ActionName("GetNalogTransfer")]
        public async Task<ActionResult> GetNalogTransfer()
        {
            var a = await _bankServices.GetGoverment();
            return Json(a.Stavka);
        }

        [HttpPost("getMoneyLikeNum/")]
        [ActionName("GetMoneyLikeNum")]
        public async Task<ActionResult> GetMoneyLikeNum()
        {
            var a = await _bankServices.GetGoverment();
            return Json(a.MoneyFromLike);
        }

        [HttpPost("getAbsoluteUrl/")]
        [ActionName("GetAbsoluteUrl")]
        public ActionResult GetAbsoluteUrl(string act, string Controllers, string array)
        {
            var expConverter = new ExpandoObjectConverter();

            object obj = JsonConvert.DeserializeObject<ExpandoObject>(array, expConverter);

            var url = Url.Action(act, Controllers, obj);

            return Json(url);
        }

        [HttpPost("buyItem/{id}")]
        [ActionName("BuyItem")]
        public async Task<IActionResult> BuyItem(int id) // если ошибка - возвращаем средства
        {
            var user = await _bankServices.GetUser();
            if (user != null)
            {
                var item = await _bankServices.GetShopItem(id);
                var weith = await _bankServices.GetWeithLevelUser(user.Welfare + (int)(user.WelfareItem * 3.20));
                var priceT = item.Price - ((item.Price / 100 ) * ((weith.Id - 1) * 2));

                if (_bankServices.UserHavePremium(user))
                {
                    priceT = Convert.ToInt32(priceT * 0.9);
                }

                if (!item.isActived)
                    return RedirectToAction("SingleItem", "Bank", new { id = id });

                if (item.Value < 1)
                    return RedirectToAction("SingleItem", "Bank", new { id = id });

                if (priceT > user.Money || item.Price <= 0 || priceT <= 0)
                    return RedirectToAction("SingleItem", "Bank", new { id = id });

                var gov = await _bankServices.GetGoverment();

                var nalog = Convert.ToInt32(gov.Nalog_Item);

                if (priceT > 100)
                    nalog = Convert.ToInt32((priceT / 100) * gov.Nalog_Item);

                var price = priceT - nalog;

                if(price <= 0)
                {
                    price = 1;
                }

                user.Money -= price;
                await _bankServices.SpentMoney(price);
                user.WelfareItem += item.RarePoint;

                await _bankServices.UpdateUser(user);

                await _bankServices.SetUserItem(user, item);

                item.Value -= 1;

                await _bankServices.UpdateItem(item);

                var org = await _bankServices.GetOrganizations(item.OrgId);

                org.Balance += price;
                await _bankServices.UpdateOrganization(org);

                await _bankServices.AddToRecdStat(nalog);
                await _bankServices.AddMoneyToGov(nalog);

                var last = await _bankServices.GetLastItemStatistic(item);

                if (last != null)
                {
                    last.BuyCount += 1;
                    await _bankServices.UpdateItemStatistic(last);
                }

                await _bankServices.CreateBankTransaction(new BankTransaction
                {
                    Date = _bankServices.NowDateTime(),
                    Id1 = user.Id,
                    BankId2 = item.OrgId,
                    Text = "Покупка предмета '" + item.Name + "'",
                    Value = price
                });

                await _bankServices.CreateTransaction(new Transaction
                {
                    Date = _bankServices.NowDateTime(),
                    Id1 = user.Id,
                    Id2 = -1,
                    Value = price,
                    Text = "Покупка предмета '" + item.Name + "'"
                });

                return RedirectToAction("UserShopItem", "Bank", new { id = user.Id, mess = "Предмет успешно куплен", type = true });
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("user/items/{id}")]
        public async Task<IActionResult> UserShopItem(int id, string mess, bool type)
        {
            var user = await _bankServices.FindByIdAsync(id);
            if (user != null)
            {
                var list = await _bankServices.GetUserItemFunc(user);

                var model = new UserItemModel
                {
                    items = list,
                    user = user,
                    Message = mess,
                    TypeMessage = type
                };

                return View(model);
            }

            return RedirectToAction("Error", "Bank", new { code = 404 });
        }

        [HttpPost("user/login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _bankServices.FindByNameAsync(model.Name);
                if (user == null)
                {
                    ModelState.AddModelError("", "Некорректные логин и(или) пароль");
                    return View(model);
                }

                if (Crypto.VerifyHashedPassword(user.Password, model.Password))
                {
                    await Authenticate(model.Name);

                    return RedirectToAction("Index", "Bank");
                }

                ModelState.AddModelError("", "Некорректные логин и(или) пароль");
            }
            return View(model);
        }

        [HttpGet("trade_item_place/{idItem}")]
        public async Task<IActionResult> TradeItemPlace(int idItem)
        {
            var user = await _bankServices.GetUser();
            var item = await _bankServices.GetShopItem(idItem);

            if (user == null || item == null)
                return RedirectToAction("Error", "Bank", new { code = 404 });

            var userItem = await _bankServices.GetUserItem(user);

            var model = new TradeItemShop
            {
                ShopItemId = item.Id,
                newSum = item.Price,
                Count = userItem.Count(m => m.Id == item.Id)
            };

            return View(model);
        }

        [HttpPost("buy_item_trade_place/{id}")]
        public async Task<IActionResult> BuyItemTradePlace(int id)
        {
            var tradeItem = await _bankServices.GetTradeShopItemId(id);
            var item = await _bankServices.GetShopItem(tradeItem.ShopItemId);
            var user = await _bankServices.GetUser();

            if (user == null || item == null)
                return RedirectToAction("Error", "Bank", new { code = 404 });

            var userSeller = await _bankServices.FindByIdAsync(tradeItem.SellerId);

            if (user.Money < tradeItem.newSum || item.Price <= 0 || tradeItem.newSum <= 0)
                return RedirectToAction("TradeShop", "Bank");

            await _bankServices.SpentMoney(tradeItem.newSum);
            userSeller.Money += tradeItem.newSum;
            user.Money -= tradeItem.newSum;
            user.WelfareItem += item.RarePoint;

            await _bankServices.SetUserItem(user, item);

            tradeItem.Count -= 1;

            if (tradeItem.Count == 0) await _bankServices.TradeItemRemove(tradeItem);
            else await _bankServices.UpdateTradeItem(tradeItem);

            await _bankServices.CreateTransaction(new Transaction
            {
                Date = _bankServices.NowDateTime(),
                Id1 = user.Id,
                Id2 = userSeller.Id,
                Text = "Покупка предмета на рынке: '" + item.Name + "'",
                Value = tradeItem.newSum
            });

            await _bankServices.UpdateUser(userSeller);
            await _bankServices.UpdateUser(user);

            return RedirectToAction("UserShopItem", "Bank", new { id = user.Id, mess = "Предмет успешно куплен", type = true });
        }

        [HttpGet("buyWeith")]
        public async Task<IActionResult> BuyWeith()
        {
            var user = await _bankServices.GetUser();

            if (user != null)
            {
                var model = new BuyWelframe();
                return View(model);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("buyWeith")]
        public async Task<IActionResult> BuyWeith(BuyWelframe model)
        {
            var user = await _bankServices.GetUser();

            if (user != null)
            {
                var money = model.Welfare * 90;
                var org = await _bankServices.GetOrganizations("main");

                if (money > user.Money || model.Welfare <= 0)
                {
                    ModelState.AddModelError("", "Нехватает денег.");
                    return View(model);
                }

                await _bankServices.SpentMoney(money);
                user.Money -= money;
                user.Welfare += model.Welfare;
                org.Balance += money;

                await _bankServices.UpdateOrganization(org);
                await _bankServices.UpdateUser(user);

                await _bankServices.CreateBankTransaction(new BankTransaction
                {
                    Date = _bankServices.NowDateTime(),
                    Id1 = user.Id,
                    BankId2 = org.Id,
                    Text = "Покупка очков благосостояния.",
                    Value = money
                });

                await _bankServices.CreateTransaction(new Transaction
                {
                    Date = _bankServices.NowDateTime(),
                    Id1 = user.Id,
                    Id2 = -1,
                    Text = "Покупка очков благосостояния.",
                    Value = money
                });

                return RedirectToAction("UserWeith", "Bank");
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("userWeith")]
        public async Task<IActionResult> UserWeith()
        {
            var user = await _bankServices.GetUser();
            var userLevel = await _bankServices.GetWeithLevelUser(user.Welfare + (int)(user.WelfareItem * 3.20));
            var nextLevel = await _bankServices.GetWeithLevel(userLevel.Id + 1);

            if (user != null)
            {
                var model = new WeithUser
                {
                    Weith = user.Welfare + (int)(user.WelfareItem * 3.20),
                    MyLevel = userLevel,
                    NextLevel = nextLevel
                };

                return View(model);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("trade_govement_item/{id}")]
        public async Task<IActionResult> TradeItemGovermnet(int id)
        {
            var item = await _bankServices.GetShopItem(id);
            var user = await _bankServices.GetUser();

            if (user == null || item == null)
                return RedirectToAction("Error", "Bank", new { code = 404 });

            var items = await _bankServices.GetUserItemFunc(user);
            var list = items.Where(m => m.ShopItemId == id).FirstOrDefault();
            var count = list.Value;

            if (count < 1)
                return RedirectToAction("Error", "Bank", new { code = 404 });

            var ret = new TradeItemGovModel { Id = id, Col = count };

            return View(ret);
        }

        [HttpGet("create_promo")]
        [Authorize]
        public async Task<IActionResult> CreatePromoCode()
        {
            var user = await _bankServices.GetUser();

            var perm = await _bankServices.GetUserPermission(user);

            if (perm.CreatePromoCode)
            {
                var list = await _bankServices.GetListPassCodes();
                var PromoCode = new PassCode
                {
                    Value = "1"
                };

                var model = new CreatePassCodeModel
                {
                    passcode = PromoCode,
                    listPassCodes = list
                };

                return View(model);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("create_promo")]
        [Authorize]
        public async Task<IActionResult> CreatePromoCode(CreatePassCodeModel model)
        {
            var user = await _bankServices.GetUser();

            var perm = await _bankServices.GetUserPermission(user);

            if (perm.CreatePromoCode)
            {
                await _bankServices.CreatePassCode(model.passcode);

                return RedirectToAction("Index", "Bank");

            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("active_code")]
        [Authorize]
        public IActionResult ActiveCode(string message)
        {
            var model = new CodeModeActive
            {
                Message = message
            };

            return View(model);
        }

        [HttpGet("all_items_admin")]
        [Authorize]
        public async Task<IActionResult> AllItemsAdmin(string search, int? page, string type, int? rare)
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (perm.GetStatistics)
            {
                var item = await _bankServices.GetAllItems();

                if (!string.IsNullOrEmpty(type))
                {
                    item = item.Where(m => m.Type == type).ToList();
                }

                if (rare != null)
                {
                    item = item.Where(m => m.RareType == rare).ToList();
                }

                if (!string.IsNullOrEmpty(search))
                {
                    var search1 = item.Where(m => m.Name.ToLower().Contains(search.ToLower())).ToList();
                    var search2 = item.Where(m => m.Short_Desc.ToLower().Contains(search.ToLower())).ToList();
                    item = search1.Concat(search2).Distinct().ToList();
                }

                // page

                var itemOnPage = 24;
                var maxPage = (item.Count() / itemOnPage) + ((item.Count() % itemOnPage) > 0 ? 1 : 0);

                if (page == null || page < 1 || page > maxPage)
                {
                    page = 1;
                }

                ViewBag.CurrectPage = page;
                ViewBag.CountPage = maxPage;
                ViewBag.Type = type;
                ViewBag.Rare = rare;
                ViewBag.Search = search;

                item = item.Skip(((int)page - 1) * itemOnPage).Take(itemOnPage).ToList();

                return View(item);
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("set_useritem")]
        [Authorize]
        public async Task<IActionResult> SetUserItem(SetUserItemModel model, int id)
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (perm.AddItemUser)
            {
                var item = await _bankServices.GetShopItem(model.ItemId);
                var userFind = await _bankServices.FindByIdAsync(model.User.Id);

                if (item == null || userFind == null)
                {
                    ModelState.AddModelError("", "Ошибка в id Предмета.");
                    return View(model);
                }

                await _bankServices.SetUserItem(userFind, item, model.Col);

                return RedirectToAction("Balance", "Bank", new { id = model.User.Id });
            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpGet("set_useritem")]
        [Authorize]
        public async Task<IActionResult> SetUserItem(int id)
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);

            if (perm.AddItemUser)
            {
                var userFind = await _bankServices.FindByIdAsync(id);

                var model = new SetUserItemModel
                {
                    User = userFind
                };

                return View(model);

            }

            return RedirectToAction("Error", "Bank", new { code = 700 });
        }

        [HttpPost("active_code")]
        [Authorize]
        public async Task<IActionResult> ActiveCode(CodeModeActive model, string message)
        {
            var passcode = await _bankServices.GetPassCode(model.Code);
            var user = await _bankServices.GetUser();

            if (passcode != null)
            {
                var used = await _bankServices.PassCodeIsUsed(passcode, user);

                if (!used)
                {
                    await _bankServices.AddUsedCode(passcode, user);

                    var org = await _bankServices.GetOrganizations("main");

                    switch (passcode.Type)
                    {
                        case "type_money":
                            user.Money += Convert.ToInt32(passcode.Value);

                            await _bankServices.CreateTransaction(new Transaction
                            {
                                Date = _bankServices.NowDateTime(),
                                Id1 = -1,
                                Id2 = user.Id,
                                Text = "Полученные деньги с промо-кода.",
                                Value = Convert.ToInt32(passcode.Value)
                            });

                            await _bankServices.SpentMoney(Convert.ToInt32(passcode.Value));
                            org.Balance -= Convert.ToInt32(passcode.Value);
                            await _bankServices.UpdateOrganization(org);

                            await _bankServices.UpdateUser(user);

                            await _bankServices.DeletePassCode(passcode);

                            return RedirectToAction("ActiveCode", "Bank", new { message = "Поздравляю, вы получили Мемлары в количестве: '" + passcode.Value });

                        case "type_item":
                            var item = await _bankServices.GetShopItem(Convert.ToInt32(passcode.Value));
                            await _bankServices.SetUserItem(user, item);

                            await _bankServices.DeletePassCode(passcode);

                            return RedirectToAction("ActiveCode", "Bank", new { message = "Поздравляю, вы получили предмет: '" + item.Name + " ( Цена: " + item.Price + " Мемлар. )" });

                        case "type_premium":
                            user.PremiumDay += Convert.ToInt32(passcode.Value);

                            await _bankServices.UpdateUser(user);

                            await _bankServices.DeletePassCode(passcode);
                            return RedirectToAction("ActiveCode", "Bank", new { message = "Поздравляю, вы получили '" + passcode.Value + "' дней премиума." });

                        case "type_nothing":
                            await _bankServices.DeletePassCode(passcode);
                            return RedirectToAction("ActiveCode", "Bank", new { message = "Увы, вам выпало: ничего." });

                        case "type_moneyRandom":
                            var rnd = new Random();

                            var money = rnd.Next(0, Convert.ToInt32(passcode.Value));

                            user.Money += money;

                            await _bankServices.CreateTransaction(new Transaction
                            {
                                Date = _bankServices.NowDateTime(),
                                Id1 = -1,
                                Id2 = user.Id,
                                Text = "Полученные деньги с промо-кода.",
                                Value = money
                            });

                            await _bankServices.SpentMoney(money);

                            org.Balance -= money;
                            await _bankServices.UpdateOrganization(org);

                            await _bankServices.UpdateUser(user);
                            await _bankServices.DeletePassCode(passcode);

                            return RedirectToAction("ActiveCode", "Bank", new { message = "Поздравляю, вы получили случайное количество Мемлар: '" + money });

                        default:
                            return RedirectToAction("ActiveCode", "Bank", new { message = "Нерабочий промокод. ( ошибка типа )" });

                    }
                }

                return RedirectToAction("ActiveCode", "Bank", new { message = "Промокод уже использован!" });
            }

            return RedirectToAction("ActiveCode", "Bank", new { message = "Неверный промо-код." });
        }

        [HttpPost("trade_govement_item/{id}")]
        public async Task<IActionResult> TradeItemGovermnet(TradeItemGovModel model, int id)
        {
            var item = await _bankServices.GetShopItem(id);
            var user = await _bankServices.GetUser();

            if (user == null || item == null)
                return RedirectToAction("Error", "Bank", new { code = 404 });

            var items = await _bankServices.GetUserItemFunc(user);
            var list = items.Where(m => m.ShopItemId == id).FirstOrDefault();
            var count = list.Value;

            if (count < 1)
                return RedirectToAction("Error", "Bank", new { code = 404 });

            if (model.Col <= 0 || model.Col > count)
            {
                return RedirectToAction("UserShopItem", "Bank", new { id = user.Id, mess = "Неверное количество", type = false });
            }

            var cost = ((item.Price / 100) * 35) * model.Col;

            var org = await _bankServices.GetOrganizations("shop");

            if (org.Balance < cost)
            {
                return RedirectToAction("UserShopItem", "Bank", new { id = user.Id, mess = "В банке недостаточно денег", type = false });
            }

            await _bankServices.RemoveUserItem(user, item, model.Col);

            await _bankServices.SpentMoney(cost);

            user.Money += cost;
            org.Balance -= cost;

            await _bankServices.CreateBankTransaction(new BankTransaction
            {
                BankId1 = org.Id,
                Id2 = user.Id,
                Date = _bankServices.NowDateTime(),
                Text = "Продажа предмета(ов) государству '" + item.Name + "' ( " + model.Col + "шт. )",
                Value = cost
            });

            await _bankServices.UpdateUser(user);
            await _bankServices.UpdateOrganization(org);

            return RedirectToAction("UserShopItem", "Bank", new { id = user.Id, mess = "Предмет(ы) успешно продан(ы)", type = true });
        }

        [HttpGet("add_ad")]
        [Authorize]
        public async Task<IActionResult> CreateAd()
        {
            var user = await _bankServices.GetUser();
            var isprem = _bankServices.UserHavePremium(user);

            return View(new CreateAdModel
            {
                UserIsPrem = isprem,
                Value = 1
            });
        }

        [HttpPost("add_ad")]
        [Authorize]
        public async Task<IActionResult> CreateAd(CreateAdModel model)
        {
            var user = await _bankServices.GetUser();
            var isprem = _bankServices.UserHavePremium(user);

            if (model.Value is > 0 and < 1489)
            {
                var value = 0;

                if(isprem)
                {
                    if(model.isBigger) value = 420;
                    else value = 300;
                }
                else
                {
                    if (model.isBigger) value = 520;
                    else value = 400;
                }

                if (user.Money >= value * model.Value)
                {
                    var ads = new Ads
                    {
                        CreatorId = user.Id,
                        isBigger = model.isBigger,
                        View = model.Value,
                        Url = model.Url
                    };

                    if (model.ImageUrl != null || !string.IsNullOrEmpty(model.ImageStringUrl))
                    {
                        if (model.ImageUrl != null)
                        {
                            if (model.ImageUrl.Length > 10485760) // 10mb
                            {
                                ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный размер.");
                                return View(model);
                            }

                            if (!model.ImageUrl.IsImage())
                            {
                                ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный формат.");
                                return View(model);
                            }

                            var img = await _bankServices.CreateImageSys(_appEnvironment, model.ImageUrl, "userImages", -1);

                            ads.ImageUrl = img.ScreePath;
                        }
                        else ads.ImageUrl = model.ImageStringUrl;
                    }
                    else
                    {
                        ModelState.AddModelError("", "Картинка не загружена.");
                        return View(model);
                    }

                    user.Money -= value * model.Value;
                    await _bankServices.SpentMoney(value * model.Value);
                    await _bankServices.AddToRecdStat(value * model.Value);
                    await _bankServices.CreateBankTransaction(new BankTransaction
                    {
                        Date = _bankServices.NowDateTime(),
                        Id1 = user.Id,
                        BankId2 = _bankServices.GetOrganizations("main").Result.Id,
                        Text = "Покупка рекламы",
                        Value = value * model.Value
                    });

                    await _bankServices.UpdateUser(user);
                    await _bankServices.CreateAds(ads);

                    return RedirectToAction("Index", "Bank");
                } // TODO

                ModelState.AddModelError("", "У вас не хватает денег.");
                return View(model);

            }

            ModelState.AddModelError("", "Неверное количество показов.");
            return View(model);
        }

        [HttpPost("trade_item_place/{idItem}")]
        public async Task<IActionResult> TradeItemPlace(TradeItemShop model, int idItem)
        {
            var user = await _bankServices.GetUser();
            var item = await _bankServices.GetShopItem(idItem);

            if (user == null || item == null)
                return RedirectToAction("Error", "Bank", new { code = 404 });

            var userItem = await _bankServices.GetUserItem(user);

            if (model.Count > userItem.Count(m => m.Id == model.ShopItemId) || model.Count <= 0)
            {
                ModelState.AddModelError("", "Неверное количество.");
                model.Count = userItem.Count(m => m.Id == item.Id);
                return View(model);
            }

            if (model.newSum <= 0)
            {
                ModelState.AddModelError("", "Новая цена должна быть больше нуля.");
                return View(model);
            }

            if (model.newSum > 2*item.Price)
            {
                ModelState.AddModelError("", "Максимальная стоимость перепродажи - в 2 раза.");
                return View(model);
            }

            model.SellerId = user.Id;

            await _bankServices.RemoveUserItem(user, item);

            await _bankServices.CreateTradeShopItem(model);

            return RedirectToAction("TradeShop", "Bank");
        }

        [HttpGet("user_tradeShop")]
        public async Task<IActionResult> TradeShop(string type, int? page, int? rare)
        {
            var list = await _bankServices.GetAllItemsTrade();

            if (!string.IsNullOrEmpty(type))
            {
                list = list.Where(m => m.item.Type == type).ToList();
            }

            if (rare != null)
            {
                list = list.Where(m => m.item.RareType == rare).ToList();
            }

            var retList = list.Where(m => m.item.isActived == true).ToList();

            // page

            var itemOnPage = 24;
            var maxPage = (retList.Count() / itemOnPage) + ((retList.Count() % itemOnPage) > 0 ? 1 : 0);

            if (page == null || page < 1 || page > maxPage)
            {
                page = 1;
            }

            ViewBag.CurrectPage = page;
            ViewBag.CountPage = maxPage;
            ViewBag.Type = type;
            ViewBag.Rare = rare;

            retList = retList.Skip(((int)page - 1) * itemOnPage).Take(itemOnPage).ToList();

            return View(retList);
        }

        [HttpGet("all_items")]
        public async Task<IActionResult> AllItems(string search, string type, int? page, int? rare)
        {
            var list = await _bankServices.GetAllItems();

            if (!string.IsNullOrEmpty(type))
            {
                list = list.Where(m => m.Type == type).ToList();
            }

            if (rare != null)
            {
                list = list.Where(m => m.RareType == rare).ToList();
            }

            var retList = list.Where(m => m.isActived == true).Where(m => m.isCaseItem == false).ToList();

            if (!string.IsNullOrEmpty(search))
            {
                var search1 = list.Where(m => m.Name.ToLower().Contains(search.ToLower())).ToList();
                var search2 = list.Where(m => m.Short_Desc.ToLower().Contains(search.ToLower())).ToList();
                list = search1.Concat(search2).Distinct().ToList();
            }

            // page

            var itemOnPage = 24;
            var maxPage = (retList.Count() / itemOnPage) + ((retList.Count() % itemOnPage) > 0 ? 1 : 0);

            if (page == null || page < 1 || page > maxPage)
            {
                page = 1;
            }

            ViewBag.CurrectPage = page;
            ViewBag.CountPage = maxPage;
            ViewBag.Type = type;
            ViewBag.Rare = rare;
            ViewBag.Search = search;

            retList = retList.Skip(((int)page - 1) * itemOnPage).Take(itemOnPage).ToList();

            return View(retList);
        }

        [HttpGet("user/register")]
        public async Task<IActionResult> Register()
        {
            var user = await _bankServices.GetUser();

            if (user == null)
                return View(new RegisterModel());
            return RedirectToAction("Index", "Bank");
        }

        /*[HttpPost("user/register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                // await Authenticate(model.Email); // аутентификация
                //var regCode = await _bankServices.GetRegCode(model.RegCode);
                //if (regCode != null)
                //{
                User user = new User
                {
                    Name = model.Name,
                    Password = model.Password,
                    Money = 0,
                    UrlVK = model.VkUrl
                };


                if (model.Name.Contains(">") || model.Name.Contains("=") || model.Name.Contains("<") || model.Name.Contains(":") || model.Name.Contains("\""))
                {
                    ModelState.AddModelError("", "Некорентный ввод.");
                    return View(model);
                }

                if (String.IsNullOrEmpty(model.Name) || model.Name.Length > 20 || model.Name.Length < 3)
                {
                    ModelState.AddModelError("", "Максимальная длина имени: 20, минимальная 3.");
                    return View(model);
                }


                //await _bankServices.DeleteRegCode(regCode);

                if (await _bankServices.FindByNameAsync(model.Name) == null) // Есть ли уже аккаунт с таким именем
                {

                    if (model.VkUrl == null)
                    {
                        ModelState.AddModelError("", "Введите ссылку на профиль ВК.");
                        return View(model);
                    }


                    if (model.ImageUrl != null || !String.IsNullOrEmpty(model.ImageStringUrl))
                    {
                        if (model.ImageUrl != null)
                        {
                            if (model.ImageUrl.Length > 10485760) // 10mb
                            {
                                ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный размер.");
                                return View(model);
                            }

                            if (!model.ImageUrl.IsImage())
                            {
                                ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный формат.");
                                return View(model);
                            }

                            var img = await _bankServices.CreateImageSys(_appEnvironment, model.ImageUrl, "userImages", -1);

                            user.ImageUrl = img.ScreePath;
                        }
                        else user.ImageUrl = model.ImageStringUrl;
                    }
                    else user.ImageUrl = "/userImages/" + "default_image.png";

                    await _bankServices.CreateUserAsync(user); // добавляем пользователя

                    await _bankServices.CreateRoleUser(new UserRole() { RoleName = "User", UserId = user.Id });

                    await Authenticate(model.Name);
                    return RedirectToAction("Index", "Bank");
                }
                else ModelState.AddModelError("", "Пользователь с таким именем уже существует.");
                //}
                //else ModelState.AddModelError("", "Неверный номер гражданства Изгоев Мемов.");
            }
            return View(model);
        }*/

        public static string RandomPwd(int length)
        {
            try
            {
                var result = new byte[length];
                for (var index = 0; index < length; index++)
                {
                    result[index] = (byte)new Random().Next(33, 126);
                }
                return System.Text.Encoding.ASCII.GetString(result);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        [HttpPost("user/registerVK")]
        public async Task<IActionResult> RegisterVK(RegisterVKModel model)
        {
            var findUser = await _bankServices.FindByVKID(model.VKUid);

            if (findUser != null) // user is login
            {
                await Authenticate(findUser.Name);
                return RedirectToAction("Index", "Bank");
            }

            if (string.IsNullOrEmpty(model.Name) || string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("", "Неверные данные.");
                return View(model);
            }

            var user = new User
            {
                Name = model.Name,
                Password = Crypto.HashPassword(model.Password),
                Money = 0,
                VKUniqId = model.VKUid,
                ImageUrl = "/userImages/" + "default_image.png"
            };

            if (model.Name.Contains(">") || model.Name.Contains("=") || model.Name.Contains("<") || model.Name.Contains(":") || model.Name.Contains("\""))
            {
                ModelState.AddModelError("", "Некорентный ввод.");
                return View(model);
            }

            if (string.IsNullOrEmpty(model.Name) || model.Name.Length > 20 || model.Name.Length < 3)
            {
                ModelState.AddModelError("", "Максимальная длина имени: 20, минимальная 3.");
                return View(model);
            }

            var search = await _bankServices.FindByNameAsync(model.Name);

            if (search != null)
            {
                ModelState.AddModelError("", "Никнейм занят.");
                return View(model);
            }

            await _bankServices.CreateUserAsync(user); // добавляем пользователя

            await _bankServices.CreateRoleUser(new UserRole { RoleName = "User", UserId = user.Id });

            await Authenticate(model.Name);
            return RedirectToAction("Index", "Bank");
        }


        [HttpGet("user/registerVK")]
        public async Task<IActionResult> RegisterVK(long uid, string fn, string ln, string photo, string photo_rec, string hash)
        {
            // check hash
            var appid = Environment.GetEnvironmentVariable($"VkConnect_AppId");
            var secretKey = Environment.GetEnvironmentVariable($"VkConnect_SecretKey");

            if (hash != MD5HashPHP(appid + uid + secretKey))
                return RedirectToAction("Error", "Bank", new { code = 501 });

            var findUser = await _bankServices.FindByVKID(uid);

            if(findUser != null) // Автовход
            {
                await Authenticate(findUser.Name);
                return RedirectToAction("Index", "Bank");
            }

            // регистрация
            return View(new RegisterVKModel
            {
                Name = fn + " " + ln,
                Password = "",
                Photo = photo,
                VKUid = uid
            });
        }

        public static string MD5HashPHP(string str)
        {
            var asciiBytes = ASCIIEncoding.ASCII.GetBytes(str);
            var hashedBytes = MD5CryptoServiceProvider.Create().ComputeHash(asciiBytes);
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }

        [HttpGet("user/settings")]
        public async Task<IActionResult> EditUser()
        {
            var user = await _bankServices.GetUser();

            if (user != null)
            {
                var model = new EditUserModelSettings
                {
                    Name = user.Name,
                    Password = "",
                };

                return View(model);
            }

            return RedirectToAction("Index", "Bank");
        }

        [HttpPost("user/settings")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserModelSettings model)
        {
            if (ModelState.IsValid)
            {
                // await Authenticate(model.Email); // аутентификация
                //var regCode = await _bankServices.GetRegCode(model.RegCode);
                //if (regCode != null)
                //{
                var user = await _bankServices.GetUser();

                //await _bankServices.DeleteRegCode(regCode);

                if (model.Name.Contains(">") || model.Name.Contains("=") || model.Name.Contains("<") || model.Name.Contains(":") || model.Name.Contains("\""))
                {
                    ModelState.AddModelError("", "Некорентный ввод.");
                    return View(model);
                }

                if (string.IsNullOrEmpty(model.Name) || model.Name.Length > 20 || model.Name.Length < 3)
                {
                    ModelState.AddModelError("", "Максимальная длина имени: 20, минимальная 3.");
                    return View(model);
                }

                if (await _bankServices.FindByNameAsync(model.Name) == null || user.Name == model.Name) // Есть ли уже аккаунт с таким именем
                {
                    if (model.ImageUrl != null || !string.IsNullOrEmpty(model.ImageStringUrl))
                    {
                        if (model.ImageUrl != null)
                        {
                            if (model.ImageUrl.Length > 10485760) // 10mb
                            {
                                ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный размер.");
                                return View(model);
                            }

                            if (!model.ImageUrl.IsImage())
                            {
                                ModelState.AddModelError("", "Файл " + model.ImageUrl.FileName + " имеет неверный формат.");
                                return View(model);
                            }

                            await _bankServices.DeleteImageSysAndFile(user.ImageUrl); // Удаляем старую фотографию

                            var img = await _bankServices.CreateImageSys(_appEnvironment, model.ImageUrl, "userImages");
                            user.ImageUrl = img.ScreePath;
                        }
                    }

                    user.Name = model.Name;

                    if(!string.IsNullOrWhiteSpace(model.Password) && !string.IsNullOrWhiteSpace(model.Password))
                        user.Password = Crypto.HashPassword(model.Password);

                    await _bankServices.UpdateUser(user); // обновляем пользователя

                    await Authenticate(model.Name);
                    return RedirectToAction("Index", "Bank");
                }

                ModelState.AddModelError("", "Пользователь с таким именем уже существует.");
            }
            return View(model);
        }



        [HttpGet("gov/balance")]
        public IActionResult GovermentBalance()
        {
            return View();
        }

        [ActionName("IncrementView")]
        [HttpPost("increment_view")]
        public async Task<IActionResult> IncrementView()
        {
            await _bankServices.AddView();
            var last = await _bankServices.GetLastStatistic();
            return Json(last.ViewUser);
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Bank");
        }

        private async Task Authenticate(string userName)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, userName)
            };

            var id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id), new AuthenticationProperties { IsPersistent = true });
        }

        [HttpPost("regcode_gen")]
        [Authorize]
        public async Task<ActionResult> GenerateCodeAjax()
        {
            var user = await _bankServices.GetUser();
            var perm = await _bankServices.GetUserPermission(user);
            if (perm.GenerateRegCodes)
            {
                var rnd = new Random();
                var result = "";

                for (var a = 0; a < 10; a++)
                {
                    switch (rnd.Next(0, 2))
                    {
                        case 0: result += (char)rnd.Next(65, 90); break;
                        case 1: result += (char)rnd.Next(48, 57); break;
                    }
                }

                var code = new RegCode
                {
                    Code = result
                };

                await _bankServices.CreateCode(code);

                return Json(result);
            }
            return Json("Недостаточно прав.");
        }
    }
}