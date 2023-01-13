using CryptoHelper;
using KeklandBankSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeklandBankSystem
{
    public class SeedMigrateDataBaseService
    {
        private BankContext bdb { get; set; }

        public SeedMigrateDataBaseService(BankContext _bdb)
        {
            bdb = _bdb;
        }

        public async Task MigrateSeedDataBase()
        {
            var usersCount = bdb.Users.Count();

            if(usersCount <= 0)
            {
                var user = new User
                {
                    PremiumDay = 0,
                    Coins = 0,
                    HaveLegendary = false,
                    ImageUrl = "/userImages/" + "default_image.png",
                    IsArrested = false,
                    IsUniqView = false,
                    Money = 0,
                    Name = "DezareD",
                    newViewEntity = 0,
                    NonViewTrans = false,
                    Password = Crypto.HashPassword("Dezare32321"),
                    VKUniqId = 173193814,
                    Welfare = 0,
                    WelfareItem = 0
                };

                bdb.Users.Add(user);
                await bdb.SaveChangesAsync();
                var role = new UserRole
                {
                    RoleName = "Owner",
                    UserId = user.Id
                };

                var listRolePermission = new List<PermissionAdmin>
                {
                    new PermissionAdmin
                    {
                        RoleName = "User"
                    },
                    new PermissionAdmin
                    {
                        RoleName = "Tester"
                    },
                    new PermissionAdmin
                    {
                        RoleName = "Moderator",
                        CheckGoverments = true,
                        CheckItems = true,
                        CheckNews = true,
                        CheckOrganization = true,
                        CheckProjects = true,
                        CheckTickets = true,
                        ShowAllOrgOrUserTransaction = true,
                        ShowAdminStatistics = true,
                        GetStatistics = true
                    },
                    new PermissionAdmin
                    {
                        RoleName = "Administrator",
                        AddItemUser = true,
                        ChangeOrganizationEconomy = true,
                        ChangeOrganizationInfo = true,
                        ChangeUserDeposit = true,
                        ChangeUserEconomy = true,
                        ChangeUserInfo = true,
                        CheckGoverments = true,
                        CheckItems = true,
                        CheckNews = true,
                        CheckOrganization = true,
                        CheckProjects = true,
                        CheckTickets = true,
                        CreateArticles = true,
                        CreateGoverment = true,
                        DeleteItem = true,
                        DeleteOrganization = true,
                        DeleteTransaction = false,
                        CreateItemOrg = true,
                        CreateOrganization = true,
                        CreateProject = true,
                        CreatePromoCode = true,
                        EconomyPanel = true,
                        EditGoverment = true,
                        EditItem = true,
                        EditZamOrg = true,
                        GenerateRegCodes = false,
                        GetStatistics = true,
                        JobSettings = true,
                        ShowAdminStatistics = true,
                        ShowAllOrgOrUserTransaction = true
                    },
                    new PermissionAdmin
                    {
                        RoleName = "Owner",
                        AddItemUser = true,
                        ChangeOrganizationEconomy = true,
                        ChangeOrganizationInfo = true,
                        ChangeUserDeposit = true,
                        ChangeUserEconomy = true,
                        ChangeUserInfo = true,
                        CheckGoverments = true,
                        CheckItems = true,
                        CheckNews = true,
                        CheckOrganization = true,
                        CheckProjects = true,
                        CheckTickets = true,
                        CreateArticles = true,
                        CreateGoverment = true,
                        DeleteItem = true,
                        DeleteOrganization = true,
                        DeleteTransaction = true,
                        CreateItemOrg = true,
                        CreateOrganization = true,
                        CreateProject = true,
                        CreatePromoCode = true,
                        EconomyPanel = true,
                        EditGoverment = true,
                        EditItem = true,
                        EditZamOrg = true,
                        GenerateRegCodes = true,
                        GetStatistics = true,
                        JobSettings = true,
                        ShowAdminStatistics = true,
                        ShowAllOrgOrUserTransaction = true
                    }
                };

                foreach (var perm in listRolePermission)
                {
                    var search = await bdb.PermissionAdmins.Where(m => m.RoleName == perm.RoleName).FirstOrDefaultAsync();

                    if (search != null)
                        listRolePermission.Remove(perm);
                }

                bdb.UserRoles.Add(role);
                bdb.PermissionAdmins.AddRange(listRolePermission);
                await bdb.SaveChangesAsync();
            }

            var govsys = await bdb.SystemMain.FirstOrDefaultAsync();
            if (govsys == null)
            {
                var system = new SystemMain
                {
                    CasesIsOn = false,
                    CasinoIsOn = false,
                    MoneyFromLike = 20,
                    Nalog_Item = 1,
                    Nalog_Project = 1,
                    PresidentName = "DezareD",
                    SiteIsOn = true,
                    Skrutka = 1,
                    Stavka = 1,
                    Stavka_Nalog = 1,
                    Stavka_Vlojen = 1,
                    UserGetMoneyProject = 10
                };

                bdb.SystemMain.Add(system);
                await bdb.SaveChangesAsync();
            }

            // Todo
            var weithLevel = new List<WeithLevel>
            {
                new WeithLevel { Name = "Селянин" },
                new WeithLevel { Name = "Гражданин" }
            };

            var weithLevelCount = bdb.WeithLevel.Count();

            if(weithLevelCount <= 0)
            {
                bdb.WeithLevel.AddRange(weithLevel);
                await bdb.SaveChangesAsync();
            }

            var mainOrganization = await bdb.Organizations.Where(m => m.SpecialId == "main").FirstOrDefaultAsync();

            if (mainOrganization == null)
            {
                var ownerUser = await bdb.UserRoles.Where(m => m.RoleName == "Owner").FirstOrDefaultAsync();
                var userreg = await bdb.Users.Where(m => m.Id == ownerUser.UserId).FirstOrDefaultAsync();

                var organization = new Organization
                {
                    Name = "Центральный орган Торговой Мемной Лиги",
                    Short_Desc = "Предназначен для технических выплат в орагнизации, поддержание работы казино, товаров, накопления налогов. Содержит изначальный пул денег денег для всего ТМЛ.",
                    AdminId = userreg.Id,
                    Balance = 550000,
                    GovermentId = -1,
                    ImageUrl = "https://sun9-6.userapi.com/impg/MmTYtKtKkHgD5HhsJXVVYmul5nwbiivAtndgeg/frrS0JyulGc.jpg?size=1000x1000&quality=96&sign=f2324f4c2af31dc04b672dc80785d17a&type=album",
                    Influence = 0,
                    isBuy = false,
                    isZacrep = true,
                    SpecialId = "main",
                    Status = "status_ok",
                    VkUrl = "https://vk.com/kekland_bank"
                };

                bdb.Organizations.Add(organization);
                await bdb.SaveChangesAsync();
            }

            var statisticCount = bdb.Statistics.Count();

            if(statisticCount <= 0)
            {
                var statistic = new Statistic
                {
                    Date = DateTime.Now.Ticks,
                    Recd = 0,
                    Spent = 0,
                    SpentMoneyAll = 0,
                    UniqUser = 0,
                    ViewUser = 0
                };

                bdb.Statistics.Add(statistic);
                await bdb.SaveChangesAsync();
            }
        }
    }
}
