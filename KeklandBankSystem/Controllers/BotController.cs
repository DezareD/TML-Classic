using KeklandBankSystem.Infrastructure;
using KeklandBankSystem.Model.VkApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using VkNet.Abstractions;
using VkNet.Model;
using VkNet.Model.GroupUpdate;
using VkNet.Model.RequestParams;
using VkNet.Utils;
using Transaction = KeklandBankSystem.Infrastructure.Transaction;

namespace KeklandBankSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IVkApi _vkApi;
        private readonly IBankServices _bankServices;

        public BotController(IVkApi vkApi, IConfiguration configuration, IBankServices bankServices)
        {
            _vkApi = vkApi;
            _configuration = configuration;
            _bankServices = bankServices;
        }

        [HttpPost("callback")]
        public async Task<IActionResult> CallBack([FromBody] VkCallBackApiRequest model)
        {
            if (model == null)
                return Ok("[error] Bad model");

            var groupId = Convert.ToInt64(Environment.GetEnvironmentVariable("API_VKCALLBACKAPI_GROUPID"));

            if (model.Secret != Environment.GetEnvironmentVariable("API_VKCALLBACKAPI_SECRETSTRING"))
                return Ok("[error] error secret");


            if (model.Type == "confirmation")
            {
                return Ok(Environment.GetEnvironmentVariable("API_VKCALLBACKAPI_STRINGREQUEST"));
            }

            if (model.Type is "donut_subscription_create" or "donut_subscription_prolonged")
            {
                var objectResponse = DonutNew.FromJson(new VkResponse(model.Object));

                var findUser = await _bankServices.FindByVKID(objectResponse.UserId ?? 0);

                if (findUser == null)
                {
                    _vkApi.Messages.Send(new MessagesSendParams
                    {
                        RandomId = new DateTime().Millisecond,
                        PeerId = objectResponse.UserId,
                        Message = $"⚠ Вам не выдан премиум на сайте по определённым причинам, срочно отпишитесь тут для связи с администрацией."
                    });

                    return Ok("ok");
                }

                var message =
                    $"⭐ Поздравляю, вы поддержали сообщество TML Classic на {objectResponse.AmountWithoutFee} рублей! " +
                    "Спасибо вам огромное, вы сталью частью активно развивающегося комьюнити TML Rewneal. " +
                    "За это вы получите следующие бонусы:";

                findUser.PremiumDay += 30;

                message +=
                    $"\n\n- На ваш аккаунт `{findUser.Name}` на сайте уже был начислен премиум на время, пока действует подписка.";

                if (objectResponse.AmountWithoutFee > 200)
                {
                    var addedMoney = Convert.ToInt32(objectResponse.AmountWithoutFee * 5);
                    message +=
                        $"\n-В связи с тем, что вы платите за подписку больше, чем 200 рублей, вы получите бонусные: {addedMoney} мемлар.";

                    await _bankServices.CreateTransaction(new Transaction
                    {
                        Date = _bankServices.NowDateTime(),
                        Id1 = -1,
                        Id2 = findUser.Id,
                        Text = "Дополнительные деньги при покупке премиума.",
                        Value = addedMoney
                    });

                    findUser.Money += addedMoney;
                }

                message += "\n-Если вы впервые задонатили - вы получите доступ к уникальным постам и беседе донов.";

                _vkApi.Messages.Send(new MessagesSendParams
                {
                    RandomId = new DateTime().Millisecond,
                    PeerId = objectResponse.UserId,
                    Message = message
                });

                await _bankServices.UpdateUser(findUser);

                return Ok("ok");
            }
            if (model.Type is "donut_subscription_expired" or "donut_subscription_cancelled")
            {
                var objectResponse = DonutEnd.FromJson(new VkResponse(model.Object));

                _vkApi.Messages.Send(new MessagesSendParams
                {
                    RandomId = new DateTime().Millisecond,
                    PeerId = objectResponse.UserId,
                    Message = $"⚠ У вас закончилась подписка или вы отменили её. Если подписка не будет действовать - " +
                              $"ваш аккаунт потеряет статус премиум. Спасибо что были с нами и поддерживали проект. " +
                              $"До новых встреч!"
                });

                return Ok("ok");
            }

            if (model.Type == "donut_subscription_price_changed")
            {
                var objectResponse = DonutChanged.FromJson(new VkResponse(model.Object));

                _vkApi.Messages.Send(new MessagesSendParams
                {
                    RandomId = new DateTime().Millisecond,
                    PeerId = objectResponse.UserId,
                    Message = $"⚠ Вы изменили тип подписки. " +
                              $"Если по какой-то причине у вас не работает премиум обратитесь к администрации!"
                });

                return Ok("ok");
            }

            return Ok("[error] not found type");
        }
    }
}