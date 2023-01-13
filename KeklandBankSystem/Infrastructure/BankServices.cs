using KeklandBankSystem.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using VkNet.Abstractions;

namespace KeklandBankSystem.Infrastructure
{
    public interface IBankServices
    {
        Task<User> FindByNameAsync(string Name);
        Task<User> FindByVKID(long vkid);
        Task<User> FindByIdAsync(int Id);
        Task CreateUserAsync(User user);
        Task<User> GetUser();
        User GetUserNonAs();
        Task<bool> UserIsInRole(User user, string roleName);
        bool IsUserAdminNonAs(User user);
        Task<List<User>> GetAllUser();
        Task<Transaction> GetTransaction(int id);
        Task DeleteTransaction(Transaction model);

        string CompleteUserName(User user);

        bool UserHavePremium(User user);

        // Casino

        Task CreateWinner(CasinoWin model);
        Task<List<CasinoWin>> GetAllWiners();

        // Shop

        Task CreateTradeShopItem(TradeItemShop model);
        Task<TradeItemShop> GetTradeShopItem(int itemId);
        Task<TradeItemShop> GetTradeShopItemId(int Id);
        Task TradeItemRemove(TradeItemShop model);
        Task UpdateTradeItem(TradeItemShop model);
        Task RemoveUserItem(User user, ShopItem item, int col = 1);
        Task<List<ShopItem>> GetAllItems();
        Task<List<TradeShopItem>> GetAllItemsTrade();
        Task<ShopItem> GetShopItem(int id);
        Task<List<ShopItem>> GetUserItem(User user);
        Task<List<ShopItemUser>> GetUserItemFunc(User user);
        Task CreateShopItem(ShopItem model);
        Task DeleteItem(ShopItem item);
        Task UpdateItem(ShopItem item);
        Task SetUserItem(User user, ShopItem item, int col = 1);
        Task<List<ItemStatistic>> GetItemShopStatistics(ShopItem item);
        Task<ItemStatistic> GetLastItemStatistic(ShopItem item);
        Task UpdateItemStatistic(ItemStatistic item);

        Task<List<ShopItem>> GetOrganizationsItem(int orgId);

        Task<List<UserTopItems>> GetTopItemUser();
        Task<List<ShopItem>> GetItemForType(int rareType);
        //

        // PROJECT

        Task CreateProject(Project model);
        Task CreateProjectSender(ProjectSender model);
        Task<Project> GetProject(int id);
        Task UpdateProject(Project project);
        Task<List<ProjectSender>> GetProjectSenders(int id);
        Task<List<Project>> GetAllProject();

        //

        Task CreateOrgJob(OrgJob org);
        Task SetUserToJob(User user, OrgJob org);
        Task SetUserToJob(OrgJobUser usrjbor);
        Task<List<OrgJob>> GetOrganizationJob(Organization org);
        Task<OrgJob> GetOrganizationJob(int id);
        Task DeleteOrganizationJob(OrgJob orgJob);
        Task<List<OrgJobUser>> GetOrgJobUser(OrgJob orgJob);
        Task DeleteOrgJobUser(OrgJob job, User user);
        Task<OrgJobUser> GetOrgJobForUser(User user, OrgJob job);
        Task UpdateJob(OrgJob job);

        Task<List<OrgJob>> GetUserJob(User user, Organization org);

        Task<float> GetStavka();
        Task AddMoneyToGov(int Money);
        Task SaveGov(SystemMain gov);

        Task<List<Organization>> GetOrganizations();
        Task<Organization> GetOrganizations(int Id);
        Task<Organization> GetOrganizations(string mId);

        Task<List<BankTransaction>> GetOrganizationTransactions(Organization organization, int val);

        List<Transaction> GetTransactions(User user, int col);
        List<Ticket> GetTickets(User user, int col);

        Task DeleteOrganization(Organization org);

        Task<Ticket> GetLastTicket();
        int GetCountTicket();

        Task SaveTicket(Ticket ticket);

        Task CreateTransaction(Transaction trans);
        Task<RegCode> GetRegCode(string str);
        Task DeleteRegCode(RegCode regCode);
        Task UpdateUser(User user);
        
        Task<List<Organization>> GetOrganizationWhereUserAdmin(User user);
        Task<bool> UserIsPresident(User user);

        // passcode

        Task DeletePassCode(PassCode code);
        Task<PassCode> GetPassCode(string code);
        Task<List<PassCode>> GetListPassCodes();

        Task CreatePassCode(PassCode code);
        Task AddUsedCode(PassCode code, User user);
        Task<bool> PassCodeIsUsed(PassCode code, User user);
        Task<PermissionAdmin> GetUserPermission(User user);

        //

        Task SaveDeposit(Deposit dep);

        // aaaa
        Task InfluenceOrganization();
        Task GetNalogs();
        Task AddStatistics();
        Task AddDeposit();
        Task PayDay();
        Task FixBug();
        Task UpdateTopUser();
        Task AddItemStatistics();
        Task UpdateItemTopUser();
        Task WeithMunis();
        Task PrepareAllMoney(); //
        Task RefreshUniq();
        Task GovermentCompleteBalance();
        Task AddPremiumMoney();
        Task PremiumUpdate();
        Task GovermentTaxesDayIncrease();
        Task DeleteImage();
        // aaaa

        Task<SystemMain> GetGoverment();

        Task<List<Transaction>> GetTransactions(int col);

        Task CreateOrganization(Organization org);
        Task UpdateOrganization(Organization org);
        Task CreateBankTransaction(BankTransaction orgt);

        Task CreateTicket(Ticket ticket);
        List<User> GetTopUser(int col);

        Task CreateRoleUser(UserRole role);
        Task<Ticket> FindTicketById(int id);

        Task CreateCode(RegCode code);
        float GetNalogStavka();

        long NowDateTime();

        Task<Statistic> GetLastStatistic();
        Task AddToSpentStat(int col);
        Task AddToRecdStat(int col);
        Task UpdateStat(Statistic stat);
        Task DeleteNews(int id);

        Task CreateDeposit(Deposit dep);

        Task<Deposit> GetDeposit(User user);

        List<Statistic> GetStatisticsList(int col);

        Task<int[]> GetNumberStatistic();

        // Welfare

        Task<WeithLevel> GetWeithLevel(int Id);
        Task<WeithLevel> GetWeithLevelUser(int Welfare);

        // img
        Task<ImageSystem> CreateImageSys(IHostingEnvironment i, IFormFile file, string sFilePath, int Type = -1);
        Task DeleteImageSysAndFile(string path);

        // Gov

        Task CreateGoverment(GovermentPolitical gov);

        Task<GovermentPolitical> GetGoverment(int id);
        Task<List<Organization>> GetGovermentOrganization(int id);

        Task<List<GovermentPolitical>> GetAllGoverments();

        Task UpdateGoverment(GovermentPolitical gov); 
        Task<int> GetAllMoneyGov(int id);

        Task AddView(); //
        Task SpentMoney(int col); //

        Task AddUniqView(User user);
        // ADS

        Task RefreshAds();
        Task CreateAds(Ads ad);

        List<Organization> GetTopOrganization(int col);
        Task<List<GovermentStatistics>> GetTopGoverment(int col);

        // Articles

        Task CreateArticles(Articles articles);

        Task<List<Articles>> GetLastArticles();

        Task<Articles> GetArticles(int id);

        Task UpdateArticles(Articles article);

        // news

        Task CreateNews(News news);
        Task UpdateNews(News news);
        Task<List<News>> GetMainNews(int num);
        Task<List<News>> GetAllNews();

        Task NewsDays();

        Task<News> GetNews(int id);

        // entity

        Task<List<EntityTicketInformation>> GetEntityTicketInformation(int userId);
        Task CreateEntityTicket(EntityTicketInformation model);

        // ent proj
        Task CreateEntityProject(EntityTicketProject model);
        Task DeleteEntityProject(int id);
        Task<List<EntityTicketProject>> GetAllProjectEntity();

        // org

        Task CreateEntityOrganization(EntityTicketOrganization model);
        Task DeleteEntityOrganization(int id);
        Task<List<EntityTicketOrganization>> GetAllOrganizationEntity();

        // gov

        Task CreateEntityGoverment(EntityTicketGoverment model);
        Task DeleteEntityGoverment(int id);
        Task<List<EntityTicketGoverment>> GetAllGovermentEntity();

        // Role

        Task<UserRole> GetUserRoleEntity(User user);
        Task SaveRole(UserRole model);
    }

    public class BankServices : IBankServices
    {
        [Obsolete]
        public BankServices(IHostingEnvironment _env, BankContext _bdb, IHttpContextAccessor hcontext)
        {
            bdb = _bdb;
            _httpContext = hcontext;
            env = _env;
        }

        public static bool isStarted = false;

        [Obsolete]
        private IHostingEnvironment env { get; set; }
        private BankContext bdb { get; set; }
        private IHttpContextAccessor _httpContext { get; set; }
        private IVkApi _vkApi { get; set; }

        /* TIME */

        public long NowDateTime() => DateTime.Now.Ticks;

        public async Task<GovermentPolitical> GetGoverment(int id)
        {
            return await bdb.Goverments.Where(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<EntityTicketInformation>> GetEntityTicketInformation(int userId)
        {
            return await bdb.EntityTicketInformations.Where(m => m.UserId == userId).OrderByDescending(m => m.Date).Take(Math.Max(45, bdb.EntityTicketInformations.Count(m => m.UserId == userId))).ToListAsync();
        }

        public async Task<List<Organization>> GetGovermentOrganization(int id)
        {
            return await bdb.Organizations.Where(m => m.GovermentId == id).ToListAsync();
        }

        public async Task CreateEntityTicket(EntityTicketInformation model)
        {
            bdb.EntityTicketInformations.Add(model);
            await bdb.SaveChangesAsync();
        }

        public async Task SaveRole(UserRole model)
        {
            bdb.UserRoles.Update(model);
            await bdb.SaveChangesAsync();
        }

        public async Task<PermissionAdmin> GetUserPermission(User user)
        {
            var defaultPermission = new PermissionAdmin { };

            if (user == null) return defaultPermission;

            var role = await bdb.UserRoles.Where(m => m.UserId == user.Id).FirstOrDefaultAsync();

            if (role == null)
                return defaultPermission;

            var search = await bdb.PermissionAdmins.Where(m => m.RoleName == role.RoleName).FirstOrDefaultAsync();

            return search ?? defaultPermission;
        }

        public async Task UpdateArticles(Articles article)
        {
            bdb.Articles.Update(article);
            await bdb.SaveChangesAsync();
        }

        public async Task<UserRole> GetUserRoleEntity(User user)
        {
            return await bdb.UserRoles.Where(m => m.UserId == user.Id).FirstOrDefaultAsync();
        }

        // proj

        public async Task CreateEntityProject(EntityTicketProject model)
        {
            bdb.EntityTicketProjects.Add(model);
            await bdb.SaveChangesAsync();
        }
        public async Task DeleteEntityProject(int id)
        {
            bdb.EntityTicketProjects.Remove(bdb.EntityTicketProjects.First(m => m.Id == id));

            await bdb.SaveChangesAsync();
        }

        public async Task<List<EntityTicketProject>> GetAllProjectEntity()
        {
            return await bdb.EntityTicketProjects.ToListAsync();
        }

        // org

        public async Task CreateEntityOrganization(EntityTicketOrganization model)
        {
            bdb.EntityTicketOrganizations.Add(model);
            await bdb.SaveChangesAsync();
        }
        public async Task DeleteEntityOrganization(int id)
        {
            bdb.EntityTicketOrganizations.Remove(bdb.EntityTicketOrganizations.First(m => m.Id == id));

            await bdb.SaveChangesAsync();
        }

        public async Task<List<EntityTicketOrganization>> GetAllOrganizationEntity()
        {
            return await bdb.EntityTicketOrganizations.ToListAsync();
        }

        // gov

        public async Task CreateEntityGoverment(EntityTicketGoverment model)
        {
            bdb.EntityTicketGoverments.Add(model);
            await bdb.SaveChangesAsync();
        }
        public async Task DeleteEntityGoverment(int id)
        {
            bdb.EntityTicketGoverments.Remove(bdb.EntityTicketGoverments.First(m => m.Id == id));

            await bdb.SaveChangesAsync();
        }

        public async Task<List<EntityTicketGoverment>> GetAllGovermentEntity()
        {
            return await bdb.EntityTicketGoverments.ToListAsync();
        }


        public async Task<Project> GetProject(int id)
        {
            return await bdb.Projects.Where(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Articles> GetArticles(int id)
        {
            return await bdb.Articles.Where(m => m.Id == id).FirstOrDefaultAsync();
        }
        public async Task CreateArticles(Articles articles)
        {
            bdb.Articles.Add(articles);
            await bdb.SaveChangesAsync();
        }

        public async Task<List<Articles>> GetLastArticles()
        {
            return await bdb.Articles.OrderByDescending(m => m.Date).Take(3).ToListAsync();
        }

        public async Task DeleteNews(int id)
        {
            var item = bdb.News.First(m => m.Id == id);
            bdb.News.Remove(item);
            await bdb.SaveChangesAsync();
        }

        public async Task<int> GetAllMoneyGov(int id)
        {
            var s = 0;
            var govorgs = await GetGovermentOrganization(id);

            foreach(var model in govorgs)
            {
                s += model.Balance;
            }

            return s;
        }

        public async Task<News> GetNews(int id)
        {
            return await bdb.News.Where(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<GovermentPolitical>> GetAllGoverments()
        {
            return await bdb.Goverments.ToListAsync();
        }

        public async Task<List<Project>> GetAllProject()
        {
            return await bdb.Projects.ToListAsync();
        }

        public async Task UpdateGoverment(GovermentPolitical gov)
        {
            bdb.Goverments.Update(gov);
            await bdb.SaveChangesAsync();
        }

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public async Task CreateNews(News news)
        {
            bdb.News.Add(news);
            await bdb.SaveChangesAsync();
        }

        public async Task UpdateNews(News news)
        {
            bdb.News.Update(news);
            await bdb.SaveChangesAsync();
        }

        public async Task<List<News>> GetMainNews(int num)
        {
            return await bdb.News.OrderByDescending(m => m.Rare).Take(num).ToListAsync();
        }

        public async Task<List<News>> GetAllNews()
        {
            return await bdb.News.ToListAsync();
        }

        public async Task<List<ShopItem>> GetOrganizationsItem(int orgId)
        {
            return await bdb.ShopItems.Where(m => m.OrgId == orgId).ToListAsync();
        }

        [Obsolete]
        public async Task<ImageSystem> CreateImageSys(IHostingEnvironment i, IFormFile file, string sFilePath, int Type = -1)
        {
            var fileName = RandomString(16);

            var model = new ImageSystem
            {
                ScreePath = "/" + sFilePath + "/" + fileName + ".png",
                MainPath = i.WebRootPath + "/" + sFilePath + "/" + fileName + ".png",
                GenerateName = fileName,
                Type = Type
            };

            using (var fileStream = new FileStream(model.MainPath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            bdb.ImageSystems.Add(model);
            await bdb.SaveChangesAsync();

            return model;
        }



        public async Task<List<GovermentStatistics>>GetTopGoverment(int col)
        {
            var govs = await bdb.Goverments.ToListAsync();

            var retList = new List<GovermentStatistics>();

            foreach (var s in govs)
            {
                retList.Add(new GovermentStatistics
                {
                    gov = s,
                    Balance = (await GetGovermentOrganization(s.Id)).Sum(m => m.Balance)
                });

            }

            if (retList.Count >= col)
            {
                return retList.OrderByDescending(m => m.Balance).ToList().GetRange(0, col);
            }

            return retList.OrderByDescending(m => m.Balance).ToList();
        }

        public List<Organization> GetTopOrganization(int col)
        {
            return bdb.Organizations.Count(m => string.IsNullOrEmpty(m.SpecialId)) >= col 
                ? bdb.Organizations.Where(m => string.IsNullOrEmpty(m.SpecialId)).OrderByDescending(m => m.Balance).Take(col).ToList()
                : bdb.Organizations.Where(m => string.IsNullOrEmpty(m.SpecialId)).OrderByDescending(m => m.Balance).ToList();
        }

        public async Task DeleteImageSysAndFile(string path)
        {
            try
            {
                var item = await bdb.ImageSystems.Where(m => m.ScreePath == path).FirstOrDefaultAsync();
                bdb.ImageSystems.Remove(item);
                await bdb.SaveChangesAsync();

                File.Delete(item.MainPath);
            }
            catch (Exception) { }
        }

        public async Task UpdateTradeItem(TradeItemShop model)
        {
            bdb.TradeItems.Update(model);
            await bdb.SaveChangesAsync();
        }

        public async Task UpdateProject(Project project)
        {
            bdb.Projects.Update(project);
            await bdb.SaveChangesAsync();
        }

        public async Task UpdateItemStatistic(ItemStatistic item)
        {
            bdb.ItemStatistics.Update(item);
            bdb.Entry<ItemStatistic>(item).State = EntityState.Modified;
            await bdb.SaveChangesAsync();
        }

        public async Task CreateGoverment(GovermentPolitical gov)
        {
            bdb.Goverments.Add(gov);
            await bdb.SaveChangesAsync();
        }

        public async Task CreateProject(Project model)
        {
            bdb.Projects.Add(model);
            await bdb.SaveChangesAsync();
        }

        public async Task<List<ProjectSender>> GetProjectSenders(int id)
        {
            return await bdb.ProjectSenders.Where(m => m.ProjectId == id).OrderByDescending(m => m.Date).ToListAsync();
        }

        public async Task CreateProjectSender(ProjectSender model)
        {
            bdb.ProjectSenders.Add(model);
            await bdb.SaveChangesAsync();
        }

        public async Task<WeithLevel> GetWeithLevel(int Id)
        {
            var count = bdb.WeithLevel.Count();

            if (Id > count)
                Id = count;

            return await bdb.WeithLevel.Where(m => m.Id == Id).FirstAsync();
        }

        public static int WeithFormula(int x)
        {
            var y = x switch
            {
                int i when i > 21 => Convert.ToInt32(50 * Math.Pow(1.63, (double)x)),
                int i when i is >= 8 and <= 21 => 5 * (int)Math.Pow(2, x),
                int i when i is >= 4 and < 8 => 12 * x * 10,
                int i when i < 4 => 6 * x * 10,
                _ => 0
            };
            return y;
        }

        public async Task AddUniqView(User user)
        {
            var s = await GetLastStatistic();

            if(user.IsUniqView == false)
            {
                s.UniqUser++;
                user.IsUniqView = true;

                bdb.Statistics.Update(s);
                bdb.Users.Update(user);
            }

            await bdb.SaveChangesAsync();
        }

        public async Task<WeithLevel> GetWeithLevelUser(int Welfare)
        {
            var list = await bdb.WeithLevel.ToListAsync();
            var level = await bdb.WeithLevel.FirstAsync();

            foreach(var a in list)
            {
                var y = 0;
                var x = a.Id;

                y = WeithFormula(x);

                if (Welfare > y)
                {
                    level = a;
                }

            }

            return level;
        }

        public async Task<User> FindByVKID(long vkid) => await bdb.Users.Where(m => m.VKUniqId == vkid).FirstOrDefaultAsync();

        public async Task AddUsedCode(PassCode code, User user)
        {
            var passcode = new UsedPassCode
            {
                PassCodeId = code.Id,
                UserId = user.Id
            };

            bdb.UsedPassCodes.Add(passcode);

            await bdb.SaveChangesAsync();
        }

        public async Task<bool> PassCodeIsUsed(PassCode code, User user)
        {
            var search = await bdb.UsedPassCodes.Where(m => m.PassCodeId == code.Id).Where(m => m.UserId == user.Id).FirstOrDefaultAsync();

            if (search == null) return false;
            return true;
        }

        public async Task TradeItemRemove(TradeItemShop model)
        {
            bdb.TradeItems.Remove(model);
            await bdb.SaveChangesAsync();
        }

        public async Task<List<UserTopItems>> GetTopItemUser()
        {
            var users = await GetAllUser();
            var list = new List<UserTopItems>();

            foreach(var i in users)
            {
                var items = await GetUserItem(i);
                var points = 0;

                foreach(var a in items)
                {
                    points += a.RarePoint;
                }

                list.Add(new UserTopItems
                {
                    userId = i.Id,
                    ItemPoints = points
                });

                points = 0;
            }

            return list.OrderByDescending(m => m.ItemPoints).ToList();
        }

        public async Task CreatePassCode(PassCode code)
        {
            bdb.PassCodes.Add(code);
            await bdb.SaveChangesAsync();
        }

        public async Task CreateWinner(CasinoWin model)
        {
            bdb.CasinoWins.Add(model);
            await bdb.SaveChangesAsync();
        }

        public async Task<ShopItem> GetShopItem(int id)
        {
            return await bdb.ShopItems.AsNoTracking().Where(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<ItemStatistic>> GetItemShopStatistics(ShopItem item)
        {
            return await bdb.ItemStatistics.AsNoTracking().Where(m => m.ShopItemId == item.Id).OrderByDescending(m => m.Date).Take(15).ToListAsync();
        }

        public async Task<ItemStatistic> GetLastItemStatistic(ShopItem item)
        {
            var a = await GetItemShopStatistics(item);

            if (a == null || a.Count <= 0)
            {
                bdb.ItemStatistics.Add(new ItemStatistic
                {
                    Date = NowDateTime(),
                    BuyCount = 0,
                    ShopItemId = item.Id
                });

                await bdb.SaveChangesAsync();

                return (await GetItemShopStatistics(item)).Last();
            }

            return a.Last();
        }

        public async Task DeleteItem(ShopItem item)
        {
            var list = await bdb.ShopItemUser.AsNoTracking().Where(m => m.ShopItemId == item.Id).ToListAsync();
            var list2 = await bdb.ItemStatistics.AsNoTracking().Where(m => m.ShopItemId == item.Id).ToListAsync();

            bdb.ShopItemUser.RemoveRange(list);
            bdb.ItemStatistics.RemoveRange(list2);

            bdb.ShopItems.Remove(item);

            await bdb.SaveChangesAsync();
        }

        public async Task<Transaction> GetTransaction(int id)
        {
            return await bdb.Transactions.AsNoTracking().Where(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task DeleteTransaction(Transaction model)
        {
            bdb.Transactions.Remove(model);
            await bdb.SaveChangesAsync();
        }

        public async Task<List<CasinoWin>> GetAllWiners()
        {
            return await bdb.CasinoWins.AsNoTracking().OrderByDescending(m => m.Date).ToListAsync();
        }

        public async Task<List<User>> GetAllUser()
        {
            return await bdb.Users.AsNoTracking().ToListAsync();
        }

        public async Task<List<OrgJob>> GetUserJob(User user, Organization org)
        {
            var rabota = bdb.OrgJobUser.AsNoTracking().Where(m => m.UserId == user.Id).ToList();

            if (rabota.Any())
            {
                var list = new List<OrgJob>();
                foreach (var r in rabota)
                {
                    list.AddRange(await bdb.OrgJobs.AsNoTracking().Where(m => m.Id == r.OrgJobId).Where(m => m.OrganizationId == org.Id).ToListAsync());
                }

                return list;
            }
            return null;
        }

        public async Task CreateTradeShopItem(TradeItemShop model)
        {
            bdb.TradeItems.Add(model);
            await bdb.SaveChangesAsync();
        }

        public async Task SetUserItem(User user, ShopItem item, int col = 1)
        {

            var items_seatch = await bdb.ShopItemUser.AsNoTracking().Where(m => m.UserId == user.Id).Where(m => m.ShopItemId == item.Id).FirstOrDefaultAsync();

            if(items_seatch != null)
            {
                items_seatch.Value += col;

                bdb.ShopItemUser.Update(items_seatch);
            }
            else
            {
                bdb.ShopItemUser.Add(new ShopItemUser
                {
                    ShopItemId = item.Id,
                    UserId = user.Id,
                    Value = col
                });
            }

            await bdb.SaveChangesAsync();

            if (item.RareType == 4)
            {
                user.HaveLegendary = true;
                await UpdateUser(user);
            }
        }

        public async Task<List<ShopItemUser>> GetUserItemFunc(User user)
        {
            var itemsUser = await bdb.ShopItemUser.AsNoTracking().Where(m => m.UserId == user.Id).ToListAsync();

            return itemsUser;
        }

        public bool UserHavePremium(User user)
        {
            return user.PremiumDay > 0;
        }

        public async Task<List<ShopItem>> GetUserItem(User user)
        {
            var itemsUser = await bdb.ShopItemUser.AsNoTracking().Where(m => m.UserId == user.Id).ToListAsync();
            var list = new List<ShopItem>();

            foreach(var a in itemsUser)
            {
                var item = await bdb.ShopItems.AsNoTracking().Where(m => m.Id == a.ShopItemId).FirstAsync();
                list.Add(item);
            }

            return list;
        }

        public async Task UpdateItem(ShopItem item)
        {
            bdb.ShopItems.Update(item);
            await bdb.SaveChangesAsync();
        }

        public async Task CreateShopItem(ShopItem model)
        {
            bdb.ShopItems.Add(model);
            await bdb.SaveChangesAsync();
        }

        public async Task UpdateJob(OrgJob job)
        {
            bdb.OrgJobs.Update(job);
            await bdb.SaveChangesAsync();
        }

        public async Task SetUserToJob(OrgJobUser usrjbor)
        {
            bdb.OrgJobUser.Add(usrjbor);
            await bdb.SaveChangesAsync();
        }

        public async Task UpdateStat(Statistic stat)
        {
            bdb.Statistics.Update(stat);

            await bdb.SaveChangesAsync();
        }

        public async Task<OrgJobUser> GetOrgJobForUser(User user, OrgJob job)
        {
            return await bdb.OrgJobUser.AsNoTracking().Where(m => m.OrgJobId == job.Id).Where(m => m.UserId == user.Id).FirstOrDefaultAsync();
        }

        public async Task<List<ShopItem>> GetAllItems()
        {
            return await bdb.ShopItems.ToListAsync();
        }

        public async Task RemoveUserItem(User user, ShopItem item, int col = 1)
        {
            var model = await bdb.ShopItemUser.Where(m => m.ShopItemId == item.Id).Where(m => m.UserId == user.Id).FirstAsync();

            if (model.Value > col)
            {
                model.Value -= col;
                bdb.ShopItemUser.Update(model);
            }
            else
            {
                bdb.ShopItemUser.Remove(model);
            }

            await bdb.SaveChangesAsync();

            if(item.RareType == 4)
            {
                var m = await GetUserItem(user);
                if (m.Count(s => s.RareType == 4) == 1)
                {
                    user.HaveLegendary = false;
                    await UpdateUser(user);
                }
            }
        }

        public async Task<OrgJob> GetOrganizationJob(int id)
        {
            return await bdb.OrgJobs.AsNoTracking().Where(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task CreateDeposit(Deposit dep)
        {
            bdb.Deposits.Add(dep);
            await bdb.SaveChangesAsync();
        }

        public async Task<List<OrgJobUser>> GetOrgJobUser(OrgJob orgJob)
        {
            return await bdb.OrgJobUser.AsNoTracking().Where(m => m.OrgJobId == orgJob.Id).ToListAsync();
        }

        public async Task<List<TradeShopItem>> GetAllItemsTrade()
        {
            var list = await bdb.TradeItems.ToListAsync();
            var retList = new List<TradeShopItem>();

            foreach(var a in list)
            {
                var item = await GetShopItem(a.ShopItemId);

                retList.Add(new TradeShopItem
                {
                    item = item,
                    itemShop = a
                });
            }

            return retList;
        }

        public async Task DeleteOrganizationJob(OrgJob orgJob)
        {
            var list = await GetOrgJobUser(orgJob);
            bdb.OrgJobUser.RemoveRange(list);
            bdb.OrgJobs.Remove(orgJob);

            await bdb.SaveChangesAsync();
        }

        public async Task DeleteOrgJobUser(OrgJob job, User user)
        {
            var a = bdb.OrgJobUser.AsNoTracking().Where(m => m.OrgJobId == job.Id).First(m => m.UserId == user.Id);

            bdb.OrgJobUser.Remove(a);
            await bdb.SaveChangesAsync();
        }

        public async Task AddToSpentStat(int col)
        {
            var stat = await GetLastStatistic();
            stat.Spent += col;
            await UpdateStat(stat);
        }

        public string CompleteUserName(User user)
        {
            var res = "<a href=\"/user/balance/" + user.Id + "\" class=\"user-outer\"><div class=\"user-image-outer\"><img src=\"" + user.ImageUrl + "\" class=\"user-image-inner\"></div>";
            if (user.HaveLegendary)
            {
                res += "<span title=\"Обладатель легендарного предмета\" class=\"legendary-user\">L</span>";
            }
            if (UserHavePremium(user))
            {
                res += "<span title=\"Обладатель премиума\" class=\"premium-user\">P</span>";
            }
            else if (UserIsInRole(user, "Administrator").Result)
            {
                res += "<span title=\"Администратор ТМЛ\" class=\"adm-user-d\">A</span>";
            }
            else if(UserIsInRole(user, "Moderator").Result)
            {
                res += "<span title=\"Модератор ТМЛ\" class=\"adm-moder-d\">M</span>";
            }
            else if(UserIsInRole(user, "Tester").Result)
            {
                res += "<span title=\"Тестер ТМЛ\" class=\"adm-tester-d\">T</span>";
            }
            else if(UserIsInRole(user, "Owner").Result)
            {
                res += "<span title=\"Создатель ТМЛ\" class=\"adm-owner-d\">O</span>";
            }

            return res += user.Name + "</a>";
        }

        public List<Statistic> GetStatisticsList(int col)
        {
            var realCount = bdb.Statistics.Count();
            if (realCount < col)
            {
                if (realCount == 0)
                    return new List<Statistic>();

                var b = bdb.Statistics.TakeLast(realCount).ToList();
                return b.SkipLast().ToList();
            }
            var a = bdb.Statistics.TakeLast(col).ToList();
            return a.SkipLast().ToList();
        }

        public async Task AddToRecdStat(int col)
        {
            var stat = await GetLastStatistic();
            stat.Recd += col;
            await UpdateStat(stat);
        }

        public async Task<List<OrgJob>> GetOrganizationJob(Organization org)
        {
           return await bdb.OrgJobs.AsNoTracking().Where(m => m.OrganizationId == org.Id).ToListAsync();
        }

        public List<User> GetTopUser(int col)
        {
            var list = bdb.Users.OrderByDescending(m => m.Money).Take(col).ToList();
            return list;
        }

        public async Task<Statistic> GetLastStatistic()
        {
            return await bdb.Statistics.OrderByDescending(m => m.Date).FirstAsync();
        }

        public async Task<List<ShopItem>> GetItemForType(int rareType)
        {
            return await bdb.ShopItems.AsNoTracking().Where(m => m.isCaseItem == true).Where(m => m.RareType == rareType).ToListAsync();
        }

        public async Task<SystemMain> GetGoverment()
        {
            return await bdb.SystemMain.FirstAsync();
        }

        public async Task<Organization> GetOrganizations(string mId)
        {
            return await bdb.Organizations.Where(m => m.SpecialId == mId).FirstOrDefaultAsync();
        }

        public async Task<Organization> GetOrganizations(int Id)
        {
            return await bdb.Organizations.Where(m => m.Id == Id).FirstOrDefaultAsync();
        }

        public async Task AddMoneyToGov(int Money)
        {
            var gov = await GetOrganizations("main");
            gov.Balance += Money;

            await UpdateOrganization(gov);
        }


        public async Task<bool> UserIsPresident(User user)
        {
            var gov = await bdb.SystemMain.FirstAsync();
            return user.Name == gov.PresidentName;
        }

        public async Task CreateBankTransaction(BankTransaction orgt)
        {
            bdb.BankTransactions.Add(orgt);
            await bdb.SaveChangesAsync();
        }

        public async Task<TradeItemShop> GetTradeShopItem(int itemId)
        {
            return await bdb.TradeItems.Where(m => m.ShopItemId == itemId).FirstAsync();
        }

        public async Task<TradeItemShop> GetTradeShopItemId(int Id)
        {
            return await bdb.TradeItems.Where(m => m.Id == Id).FirstAsync();
        }

        public async Task DeletePassCode(PassCode code)
        {
            if(code.Count == -1)
            {
                bdb.PassCodes.Remove(code);
            }
            else
            {
                code.Count -= 1;

                if(code.Count < 1)
                {
                    bdb.PassCodes.Remove(code);
                }
                else
                {
                    bdb.PassCodes.Update(code);
                }
            }

            await bdb.SaveChangesAsync();
        }

        public async Task<PassCode> GetPassCode(string code)
        {
            return await bdb.PassCodes.Where(m => m.Code == code).FirstOrDefaultAsync();
        }

        public async Task<List<PassCode>> GetListPassCodes()
        {
            return await bdb.PassCodes.ToListAsync();
        }

        public async Task<int[]> GetNumberStatistic()
        {
            var money1 = await bdb.Users.SumAsync(m => m.Money);

            var money2 = await bdb.Deposits.SumAsync(m => m.Money);

            var money3 = await bdb.Organizations.SumAsync(m => m.Balance);

            var mass = new int[3];

            mass[0] = money1;
            mass[1] = money2;
            mass[2] = money3;

            return mass;
        }

        public float GetNalogStavka()
        {
            var a = bdb.SystemMain.First();
            return a.Stavka_Nalog;
        }

        public async Task<Deposit> GetDeposit(User user)
        {
            return await bdb.Deposits.Where(m => m.UserId == user.Id).FirstOrDefaultAsync();
        }

        public async Task CreateOrgJob(OrgJob org)
        {
            bdb.OrgJobs.Add(org);
            await bdb.SaveChangesAsync();
        }

        public async Task<List<Organization>> GetOrganizationWhereUserAdmin(User user)
        {
            return await bdb.Organizations.AsNoTracking().Where(m => m.AdminId == user.Id).ToListAsync();
        }

        public async Task SetUserToJob(User user, OrgJob org)
        {
            var job = new OrgJobUser
            {
                OrgJobId = org.Id,
                UserId = user.Id
            };

            bdb.OrgJobUser.Add(job);
            await bdb.SaveChangesAsync();
        }

        public async Task CreateOrganization(Organization org)
        {
            var job = new OrgJob
            {
                Name = "Администратор",
                PayDay = 0,
                OrganizationId = org.Id
            };

            await CreateOrgJob(job);

            await SetUserToJob(new User { Id = org.AdminId }, job);

            bdb.Organizations.Add(org);
            await bdb.SaveChangesAsync();
        }

        public async Task<List<Organization>> GetOrganizations()
        {
            return await bdb.Organizations.OrderByDescending(m => m.Balance).ToListAsync();
        }

        public async Task<List<BankTransaction>> GetOrganizationTransactions(Organization organization, int val)
        {
            var list1 = await bdb.BankTransactions.Where(m => m.BankId1 == organization.Id).ToListAsync();
            var list2 = await bdb.BankTransactions.Where(m => m.BankId2 == organization.Id).ToListAsync();

            list1.AddRange(list2);

            if(val != -1) return list1.OrderByDescending(m => m.Date).Take(val).ToList();
            return list1.OrderByDescending(m => m.Date).ToList();
        }

        public async Task SaveDeposit(Deposit dep)
        {
            bdb.Deposits.Update(dep);
            await bdb.SaveChangesAsync();
        }

        public async Task DeleteOrganization(Organization org)
        {
            var list1 = bdb.BankTransactions.Where(m => m.BankId1 == org.Id).ToList();
            var list2 = bdb.BankTransactions.Where(m => m.BankId2 == org.Id).ToList();

            list1.AddRange(list2);

            bdb.BankTransactions.RemoveRange(list1);
            bdb.Organizations.Remove(org);
            await bdb.SaveChangesAsync();
        }

        public async Task<List<Transaction>> GetTransactions(int col)
        {
            return await bdb.Transactions.OrderByDescending(m => m.Date).Take(col).ToListAsync();
        }

        public async Task UpdateOrganization(Organization org)
        {
            bdb.Organizations.Update(org);
            await bdb.SaveChangesAsync();
        }

        public async Task SaveGov(SystemMain gov)
        {
            bdb.SystemMain.Update(gov);
            await bdb.SaveChangesAsync();
            bdb.Entry<SystemMain>(gov).State = EntityState.Detached;
        }

        public async Task<RegCode> GetRegCode(string str)
        {
            return await bdb.RegCodes.Where(m => m.Code == str).FirstOrDefaultAsync();
        }

        public async Task DeleteRegCode(RegCode regCode)
        {
            bdb.RegCodes.Remove(regCode);
            await bdb.SaveChangesAsync();
        }

        public async Task<List<User>> GetUsersAll()
        {
            return await bdb.Users.AsNoTracking().ToListAsync();
        }

        public async Task GetNalogs()
        {
            var list = await GetUsersAll();

            var obshee = 0;
            var org = await GetOrganizations("main");

            if (org == null)
                return;

            var stavka = GetNalogStavka();

            foreach (var user in list)
            {
                var nStavka = stavka;

                if (UserHavePremium(user))
                {
                    nStavka = nStavka / 2;
                }

                var dep = await GetDeposit(user);

                var userNal = 0;

                if (dep is { Money: > 100 })
                {
                    var depNalog = Convert.ToInt32((dep.Money / 100) * nStavka);
                    await SpentMoney(depNalog);
                    dep.Money -= depNalog;
                    userNal += depNalog;

                    await SaveDeposit(dep);
                }

                if (user.Money > 100)
                {
                    var nalog = Convert.ToInt32((user.Money / 100) * nStavka);
                    await SpentMoney(nalog);
                    user.Money -= nalog;
                    userNal += nalog;
                    await UpdateUser(user);
                }

                if (userNal > 0 && !user.IsArrested)
                {
                    obshee += userNal;
                    await AddToRecdStat(userNal);
                    await CreateTransaction(new Transaction
                    {
                        Date = NowDateTime(),
                        Id1 = user.Id,
                        Id2 = -1,
                        Value = userNal,
                        Text = "Налоговый сбор ( " + nStavka + "% )"
                    });
                }
            }

            org.Balance += obshee;

            await CreateBankTransaction(new BankTransaction
            {
                Date = NowDateTime(),
                Id1 = -1,
                BankId2 = org.Id,
                Text = "Налоговый сбор ( " + stavka + "% )",
                Value = obshee
            });

            await UpdateOrganization(org);
        }

        public async Task AddView()
        {
            var las = await GetLastStatistic();

            las.ViewUser++;

            bdb.Statistics.Update(las);

            await bdb.SaveChangesAsync();
        }
        public async Task SpentMoney(int col)
        {
            var las = await GetLastStatistic();

            las.SpentMoneyAll += col;

            bdb.Statistics.Update(las);

            await bdb.SaveChangesAsync();
        }
        public async Task NewsDays()
        {
            var list = await bdb.News.ToListAsync();

            foreach(var i in list)
            {
                i.ShowsDays--;

                if (i.ShowsDays <= 0)
                {
                    bdb.News.Remove(i);
                }
                else bdb.News.Update(i);
            }

            await bdb.SaveChangesAsync();
        }

        
        public async Task RefreshAds()
        {
            var r = new Random();

            var big = bdb.Ads.Where(m => m.isBigger == true).Skip(r.Next(0, bdb.Ads.Count(m => m.isBigger == true))).FirstOrDefault();
            var mini = bdb.Ads.Where(m => m.isBigger == false).Skip(r.Next(0, bdb.Ads.Count(m => m.isBigger == false))).FirstOrDefault();

            if (big != null)
            {
                big.View--;

                MegaAdd = big.ImageUrl;
                MegaAddUrl = big.Url;

                if (big.View > 0) bdb.Ads.Update(big);
                else bdb.Ads.Remove(big);
            }
            else MegaAdd = @"/userImagesAds/DefaultMegaAd.png";

            if (mini != null)
            {
                mini.View--;

                MiniAdd = mini.ImageUrl;
                MiniAddUrl = mini.Url;

                if (mini.View > 0)  bdb.Ads.Update(mini);
                else bdb.Ads.Remove(mini);
            }
            else MiniAdd = @"/userImagesAds/DefaultMiniAd.png";

            await bdb.SaveChangesAsync();
        }

        public async Task CreateAds(Ads ad)
        {
            bdb.Ads.Add(ad);
            await bdb.SaveChangesAsync();
        }

        public static string MiniAdd { get; set; }
        public static string MegaAdd { get; set; }

        public static string MiniAddUrl { get; set; }
        public static string MegaAddUrl { get; set; }

        public static int AllMoneyInBank { get; set; }

        public async Task PrepareAllMoney()
        {
            var allUser = await bdb.Users.ToListAsync();
            var allorg = await bdb.Organizations.ToListAsync();

            var las = await GetLastStatistic();

            foreach (var s in allUser)
            {
                AllMoneyInBank += s.Money;
            }

            foreach(var s in allorg)
            {
                AllMoneyInBank += s.Balance;
            }
        }

        public async Task InfluenceOrganization()
        {
            var org_list = await GetOrganizations();
            var money = 0;

            var org_main = await GetOrganizations("main");

            if (org_main == null)
                return;

            foreach (var org in org_list)
            {
                if (org.Influence > 0 && org.Status == "status_ok")
                {
                    org.Balance += org.Influence;

                    money += org.Influence;

                    await AddToSpentStat(org.Influence);

                    await CreateBankTransaction(new BankTransaction
                    {
                        BankId2 = org.Id,
                        Date = NowDateTime(),
                        Text = "Финансирование от банка",
                        Value = org.Influence,
                        Id1 = -1
                    });

                    await UpdateOrganization(org);
                }
            }

            org_main.Balance -= money;
            await SpentMoney(money);

            await AddToSpentStat(money);

            await UpdateOrganization(org_main);

            await CreateBankTransaction(new BankTransaction
            {
                Date = NowDateTime(),
                BankId1 = org_main.Id,
                Id2 = -3,
                Value = money,
                Text = "Финансирование организаций"
            });
        }

        public async Task CreateRoleUser(UserRole role)
        {
            bdb.UserRoles.Add(role);
            await bdb.SaveChangesAsync();
        }

        public async Task<Ticket> FindTicketById(int id)
        {
            return await bdb.Tickets.Where(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task SaveTicket(Ticket ticket)
        {
            bdb.Tickets.Update(ticket);
            await bdb.SaveChangesAsync();
        }

        public async Task CreateTicket(Ticket ticket)
        {
            bdb.Tickets.Add(ticket);
            await bdb.SaveChangesAsync();
        }

        
        public async Task AddItemStatistics()
        {
            var list = await bdb.ShopItems.AsNoTracking().Where(m => m.Value > 0).ToListAsync();

            foreach(var i in list)
            {
                bdb.ItemStatistics.Add(new ItemStatistic
                {
                    Date = NowDateTime(),
                    BuyCount = 0,
                    ShopItemId = i.Id
                });
            }

            await bdb.SaveChangesAsync();
        }

        
        public async Task AddStatistics()
        {
            bdb.Statistics.Add(new Statistic
            {
                Date = NowDateTime(),
                Recd = 0,
                Spent = 0
            });

            var users = bdb.Users.ToList();

            foreach (var i in users)
            {
                if (i.IsUniqView)
                {
                    i.IsUniqView = false;
                }

                //bdb.Users.Update(i);
            }

            bdb.Users.UpdateRange(users);

            await bdb.SaveChangesAsync();
        }

        public async Task<float> GetStavka()
        {
            var a = await bdb.SystemMain.FirstAsync();
            return a.Stavka;
        }

        public async Task CreateCode(RegCode code)
        {
            bdb.RegCodes.Add(code);
            await bdb.SaveChangesAsync();
        }

        // todo
        public async Task<bool> UserIsInRole(User user, string roleName)
        {
            if (user == null) return false;

            var a = await bdb.UserRoles.Where(m => m.UserId == user.Id).Where(m => m.RoleName == roleName).FirstOrDefaultAsync();
            if (a == null) return false;
            return true;
        }

        public async Task UpdateUser(User user)
        {
            bdb.Users.Update(user);
            await bdb.SaveChangesAsync();
            bdb.Entry<User>(user).State = EntityState.Detached;
        }

        public async Task<Ticket> GetLastTicket()
        {
            var a = await bdb.Tickets.Where(m => m.Status == "status_timing").OrderByDescending(m => m.Date).FirstOrDefaultAsync();
            return a;
        }

        public int GetCountTicket()
        {
            var a = bdb.Tickets.Where(m => m.Status == "status_timing").ToList();
            return a.Count();
        }

        public async Task CreateTransaction(Transaction trans)
        {
            if ((trans.Id2 > 0))
            {
                var userTo = await FindByIdAsync(trans.Id2);
                userTo.NonViewTrans = true;
                await UpdateUser(userTo);
            }
            else
            {
                var userTo = await FindByIdAsync(trans.Id1);
                userTo.NonViewTrans = true;
                await UpdateUser(userTo);
            }

            bdb.Transactions.Add(trans);
            await bdb.SaveChangesAsync();
        }

        public List<Transaction> GetTransactions(User user, int col)
        {
            var list = bdb.Transactions.AsNoTracking().Where(m => m.Id1 == user.Id).ToList();
            var list2 = bdb.Transactions.AsNoTracking().Where(m => m.Id2 == user.Id).ToList();

            list.AddRange(list2);

            if (col != -1) return list.OrderByDescending(m => m.Date).Take(col).ToList();
            return list.OrderByDescending(m => m.Date).ToList();
        }

        public List<Ticket> GetTickets(User user, int col)
        {
            var list = bdb.Tickets.AsNoTracking().Where(m => m.UserId == user.Id).OrderByDescending(m => m.Date).Take(col).ToList();
            return list;
        }

        public async Task<User> GetUser()
        {
            return await FindByNameAsync(_httpContext.HttpContext.User.Identity.Name);
        }

        
        public async Task UpdateTopUser()
        {
            var list = await GetAllUser();
            var ret = new List<User>();

            foreach (var i in list)
            {
                    var dep = await GetDeposit(i);

                    var items = await GetUserItem(i);

                    var itemMoney = 0;

                    foreach (var ite in items)
                    {
                        itemMoney += ite.Price;
                    }

                    if (dep != null)
                    {
                        if (i.Money + dep.Money + itemMoney > 0)
                        {
                            ret.Add(new User
                            {
                                Money = i.Money + dep.Money + itemMoney + (i.Welfare + i.WelfareItem) * 90,
                                Name = i.Name,
                                Id = i.Id,
                                ImageUrl = i.ImageUrl
                            });
                        }
                    }
                    else
                    {
                        if (i.Money + itemMoney > 0)
                        {
                            ret.Add(new User
                            {
                                Money = i.Money + itemMoney + (i.Welfare + i.WelfareItem) * 90,
                                Name = i.Name,
                                Id = i.Id,
                                ImageUrl = i.ImageUrl
                            });
                        }
                }
            }

            topUser = ret.OrderByDescending(m => m.Money).Take(15).ToList();
        }

        public static List<User> topUser = new List<User>();

        
        public async Task UpdateItemTopUser()
        {
            var list = await GetTopItemUser();

            topItemUser = list.Take(15).ToList();
        }

        public static List<UserTopItems> topItemUser { get; set; }

        
        public async Task ExtractUserItem()
        {
            var list = await bdb.ShopItemUser.ToListAsync();

            var block = new List<ShopItemUser>();

            foreach (var a in list)
            {
                if (a.isNeactive == false)
                {

                    var model = new ShopItemUser
                    {
                        ShopItemId = a.ShopItemId,
                        UserId = a.UserId,
                        Value = 1
                    };

                    var m = 0;

                    foreach (var s in list)
                    {
                        if (a.Id != s.Id && a.ShopItemId == s.ShopItemId && a.UserId == s.UserId)
                        {
                            model.Value += 1;

                            list[m].isNeactive = true;

                        }

                        m++;
                    }


                    block.Add(model);

                    Console.WriteLine("[PROJECTMC] Extract ` " + a.Id + " ` entity. Count exctract: " + model.Value);
                }
            }

            bdb.ShopItemUser.RemoveRange(list);
            bdb.ShopItemUser.AddRange(block);

            await bdb.SaveChangesAsync();
        }

        
        public async Task WeithMunis()
        {
            var list = await bdb.Users.ToListAsync();

            foreach (var i in list)
            {
                i.Welfare -= (int)(i.Welfare * 0.02);

                //bdb.Users.Update(i);
            }

            bdb.Users.UpdateRange(list);

            await bdb.SaveChangesAsync();
        }

        
        public async Task GovermentCompleteBalance()
        {
            var allGov = await bdb.Goverments.ToListAsync();

            foreach(var i in allGov)
            {
                var a = 0;
                var orgs = await bdb.Organizations.Where(m => m.GovermentId == i.Id).ToListAsync();

                foreach(var k in orgs)
                {
                    a += k.Balance;
                }

                i.AllOrganizationBalance = a;
                //bdb.Goverments.Update(i);
            }

            bdb.Goverments.UpdateRange(allGov);

            await bdb.SaveChangesAsync();
        }

        
        public async Task GovermentTaxesDayIncrease()
        {
            var list = await bdb.Goverments.ToListAsync();

            foreach(var i in list)
            {
                if (i.DaysGovermentTaxes > 0)
                {
                    i.DaysGovermentTaxes -= 1;
                }
                //bdb.Goverments.Update(i);
            }

            bdb.Goverments.UpdateRange(list);

            await bdb.SaveChangesAsync();
        }

        
        public async Task DeleteImage()
        {
            var list = await bdb.ImageSystems.Where(m => m.Type >= 0).ToListAsync();

            foreach(var i in list)
            {
                i.Type--;

                if (i.Type <= 0)
                {
                    await DeleteImageSysAndFile(i.MainPath);
                }
                else bdb.ImageSystems.Update(i);
            }

            await bdb.SaveChangesAsync();
        }

         // todo obsolete
        public async Task RefreshUniq()
        {
            var users = bdb.Users.ToList();

            foreach(var i in users)
            {
                if(i.IsUniqView)
                {
                    i.IsUniqView = false;
                }

                //bdb.Users.Update(i);
            }

            bdb.Users.UpdateRange(users);

            await bdb.SaveChangesAsync();
        }

        
        public async Task FixBug()
        {
            // BANK TRANSACTION

            var list = bdb.BankTransactions.ToList();
            var delList = new List<BankTransaction>();

            foreach(var i in list)
            {
                if(i.Id1 > 0)
                {
                    var user = await FindByIdAsync(i.Id1);

                    if(user == null)
                    {
                        delList.Add(i);
                    }
                }
                else if(i.Id2 > 0)
                {
                    var user = await FindByIdAsync(i.Id1);

                    if (user == null)
                    {
                        delList.Add(i);
                    }
                }
                else if(i.BankId1 > 0)
                {
                    var org = await GetOrganizations(i.BankId1);

                    if(org == null)
                    {
                        delList.Add(i);
                    }
                }
                else if (i.BankId2 > 0)
                {
                    var org = await GetOrganizations(i.BankId1);

                    if (org == null)
                    {
                        delList.Add(i);
                    }
                }
            }

            bdb.BankTransactions.RemoveRange(delList);

            // CASINO WINS

            var listCasinoWins = bdb.CasinoWins.ToList();
            var delListCasonWins = new List<CasinoWin>();

            foreach(var i in listCasinoWins)
            {
                if(i.Count is >= -1 and <= 1)
                {
                    delListCasonWins.Add(i);
                }
            }

            bdb.CasinoWins.RemoveRange(delListCasonWins);

            // ITEM STATISTICS

            var listItemStatistic = bdb.ItemStatistics.ToList();
            var delListItemStatistic = new List<ItemStatistic>();

            foreach (var i in listItemStatistic)
            {
                var search = await GetShopItem(i.ShopItemId);

                if(search == null)
                {
                    delListItemStatistic.Add(i);
                }
            }

            bdb.ItemStatistics.RemoveRange(delListItemStatistic);

            // ORGJOB

            var listOrgJobs = bdb.OrgJobs.ToList();
            var delListIOrgJobs = new List<OrgJob>();

            foreach (var i in listOrgJobs)
            {
                var search = await GetOrganizations(i.OrganizationId);

                if(search == null)
                {
                    delListIOrgJobs.Add(i);
                }
                else if(i.PayDay <= 0)
                {
                    delListIOrgJobs.Add(i);
                }
            }

            bdb.OrgJobs.RemoveRange(delListIOrgJobs);

            // ORGJOB USER

            var listOrgJobUser = bdb.OrgJobUser.ToList();
            var delListIOrgJobUser = new List<OrgJobUser>();

            foreach (var i in listOrgJobUser)
            {
                var search = await FindByIdAsync(i.UserId);
                var searchorg = await GetOrganizationJob(i.OrgJobId);

                if(search == null)
                {
                    delListIOrgJobUser.Add(i);
                }
                else if(searchorg == null)
                {
                    delListIOrgJobUser.Add(i);
                }
            }

            bdb.OrgJobUser.RemoveRange(delListIOrgJobUser);

            // PROJECT SENDERS

            var listProjectSenders = bdb.ProjectSenders.ToList();
            var delListProjectSenders = new List<ProjectSender>();

            foreach (var i in listProjectSenders)
            {
                var search = await FindByIdAsync(i.UserId);
                var searchorg = await GetProject(i.ProjectId);

                if (search == null)
                {
                    delListProjectSenders.Add(i);
                }
                else if (searchorg == null)
                {
                    delListProjectSenders.Add(i);
                }
            }

            bdb.ProjectSenders.RemoveRange(delListProjectSenders);

            // SHOPITEMS

            var listShopItemUser = bdb.ShopItemUser.ToList();
            var delListShopItemUser = new List<ShopItemUser>();

            foreach (var i in listShopItemUser)
            {
                var search = await FindByIdAsync(i.UserId);
                var searchorg = await GetShopItem(i.ShopItemId);

                if (search == null)
                {
                    delListShopItemUser.Add(i);
                }
                else if (searchorg == null)
                {
                    delListShopItemUser.Add(i);
                }
                else if(i.Value <= 0)
                {
                    delListShopItemUser.Add(i);
                }
            }

            bdb.ShopItemUser.RemoveRange(delListShopItemUser);

            // TRANSACTION


            var listTransactions = bdb.Transactions.ToList();
            var delListTransactions = new List<Transaction>();

            foreach (var i in listTransactions)
            {
                if(i.Id1 > 0)
                {
                    var search = await FindByIdAsync(i.Id1);
                    if(search == null)
                    {
                        delListTransactions.Add(i);
                    }
                }
                else if(i.Id2 > 0)
                {
                    var search = await FindByIdAsync(i.Id1);
                    if (search == null)
                    {
                        delListTransactions.Add(i);
                    }
                }
                else if(i.Value <= 0)
                {
                    delListTransactions.Add(i);
                }
            }

            bdb.Transactions.RemoveRange(delListTransactions);

            await bdb.SaveChangesAsync();
        }

        public async Task PayDay()
        {
            var list = await bdb.OrgJobUser.ToListAsync();

            /*if(env.IsDevelopment())
            {
                list = new List<OrgJobUser>()
                {
                    new OrgJobUser()
                    {
                        OrgJobId = 70,
                        UserId = 1
                    },
                    new OrgJobUser()
                    {
                        OrgJobId = 71,
                        UserId = 1
                    },
                    new OrgJobUser()
                    {
                        OrgJobId = 72,
                        UserId = 1
                    }
                };
            }*/

            foreach(var i in list)
            {
                var user = await FindByIdAsync(i.UserId);
                var prem = UserHavePremium(user);

                if(!user.IsArrested)
                {
                    var job = bdb.OrgJobs.AsNoTracking().First(m => m.Id == i.OrgJobId);
                    var org = await GetOrganizations(job.OrganizationId);
					
					if(job != null && org is { Status: "status_ok" })
					{
                        var payDay = job.PayDay;

                        if(prem)
                        {
                            payDay = Convert.ToInt32(job.PayDay * 1.5);
                        }

                        if (org.Balance >= payDay)
                        {
                            user.Money += payDay;
                            org.Balance -= payDay;

                            await SpentMoney(payDay);

                            await CreateTransaction(new Transaction
                            {
                                Date = NowDateTime(),
                                Id1 = -1,
                                Id2 = user.Id,
                                Text = "Зарплата от организации '" + org.Name + "' ( Должность: " + job.Name + " )",
                                Value = payDay
                            });

                            await CreateBankTransaction(new BankTransaction
                            {
                                Date = NowDateTime(),
                                BankId1 = org.Id,
                                Id2 = user.Id,
                                Value = payDay,
                                Text = "Зарплата от организации." + "' ( Должность: " + job.Name + " )"
                            });

                            bdb.Users.Update(user);
                            bdb.Organizations.Update(org);

                            await bdb.SaveChangesAsync();
                        }
                    }
                }

                bdb.Entry(user).State = EntityState.Detached;
            }
        }

        
        public async Task AddPremiumMoney()
        {

            var user = await GetAllUser();

            user = user.Where(m => m.PremiumDay > 0).ToList();

            foreach (var a in user)
            {
                a.Money += 500;

                //bdb.Users.Update(a);
            }

            bdb.Users.UpdateRange(user);

            await bdb.SaveChangesAsync();
        }

        
        public async Task PremiumUpdate()
        {

            var user = await GetAllUser();

            user = user.Where(m => m.PremiumDay > 0).ToList();

            foreach (var a in user)
            {
                if (a.PremiumDay > 0)
                {
                    a.PremiumDay -= 1;

                   // bdb.Users.Update(a);
                }
            }

            bdb.Users.UpdateRange(user);

            await bdb.SaveChangesAsync();
        }

        
        public async Task AddDeposit()
        {
            var dep = await bdb.Deposits.ToListAsync();

            var gov = await GetGoverment();
            var nal = gov.Stavka_Vlojen;
            var moneyBank = 0;

            foreach (var a in dep)
            {
                if (a.Money > 100)
                {
                    var user = await FindByIdAsync(a.UserId);

                    var prem = UserHavePremium(user);

                    if (prem)
                    {
                        if (!user.IsArrested)
                        {
                            var addDep = Convert.ToInt32(((a.Money / 100) * nal));

                            a.Money += addDep;
                            moneyBank += addDep;
                            //bdb.Deposits.Update(a);
                        }
                    }
                }
            }

            bdb.Deposits.UpdateRange(dep);

            var org = await GetOrganizations("main");

            if (org == null)
                return;


            if (org.Balance >= moneyBank)
            {
                await SpentMoney(moneyBank);
                org.Balance -= moneyBank;

                await CreateBankTransaction(new BankTransaction
                {
                    Date = NowDateTime(),
                    BankId1 = org.Id,
                    Id2 = -3,
                    Text = "Депозитные начисления.",
                    Value = moneyBank
                });

                await bdb.SaveChangesAsync();
            }
            else
            {
                bdb.Entry(dep).Reload();
            }
        }

        public User GetUserNonAs()
        {
            return bdb.Users.Where(m => m.Name == _httpContext.HttpContext.User.Identity.Name).FirstOrDefault();
        }

        public bool IsUserAdminNonAs(User user)
        {
            if (user == null) return false;
            var a = bdb.UserRoles.Where(m => m.UserId == user.Id).FirstOrDefault(m => m.RoleName == "Administrator");
            var b = bdb.UserRoles.Where(m => m.UserId == user.Id).FirstOrDefault(m => m.RoleName == "Owner");
            var c = bdb.UserRoles.Where(m => m.UserId == user.Id).FirstOrDefault(m => m.RoleName == "Moderator");
            return a != null || b != null || c != null;
        }

        public async Task CreateUserAsync(User user)
        {
            bdb.Users.Add(user);
            await bdb.SaveChangesAsync();
        }

        public async Task<User> FindByNameAsync(string Name)
        {
            return await bdb.Users.AsNoTracking().Where(m => m.Name == Name).FirstOrDefaultAsync();
        }

        public async Task<User> FindByIdAsync(int Id)
        {
            return await bdb.Users.AsNoTracking().Where(m => m.Id == Id).FirstOrDefaultAsync();
        }
    }

    public static class Listing
    {
        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int N)
        {
            return source.Skip(Math.Max(0, source.Count() - N));
        }


        public static void Reload(this CollectionEntry source)
        {
            if (source.CurrentValue != null)
            {
                foreach (var item in source.CurrentValue)
                    source.EntityEntry.Context.Entry(item).State = EntityState.Detached;
                source.CurrentValue = null;
            }
            source.IsLoaded = false;
            source.Load();
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
    (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var seenKeys = new HashSet<TKey>();
            foreach (var element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source)
        {
            using (var e = source.GetEnumerator())
            {
                if (e.MoveNext())
                {
                    for (var value = e.Current; e.MoveNext(); value = e.Current)
                    {
                        yield return value;
                    }
                }
            }
        }
    }
}
