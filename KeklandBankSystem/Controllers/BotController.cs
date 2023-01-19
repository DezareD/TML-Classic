using KeklandBankSystem.Infrastructure;
using KeklandBankSystem.Model.VkApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using VkNet.Abstractions;
using VkNet.Model;
using VkNet.Model.GroupUpdate;
using VkNet.Model.RequestParams;
using VkNet.Utils;

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
        public IActionResult CallBack([FromBody] VkCallBackApiRequest model)
        {
            if (model == null)
                return Ok("[error] Bad model");

            var groupId = Convert.ToInt64(Environment.GetEnvironmentVariable("API_VKCALLBACKAPI_GROUPID"));

            if (model.GroupId == groupId)
            {
                if (model.Secret != Environment.GetEnvironmentVariable("API_VKCALLBACKAPI_SECRETSTRING"))
                    return Ok("[error] error secret");

                if (model.Type == "message_new")
                {
                    var msg = Message.FromJson(new VkResponse(model.Object));
                    _vkApi.Messages.Send(new MessagesSendParams
                    {
                        RandomId = new DateTime().Millisecond,
                        PeerId = -1 * groupId,
                        UserId = msg.UserId,
                        Message = msg.Text
                    });
                }
                    
                if (model.Type == "confirmation")
                {
                    return Ok(Environment.GetEnvironmentVariable("API_VKCALLBACKAPI_STRINGREQUEST"));
                }
                else if (model.Type == "donut_subscription_create")
                {
                    var objectResponse = DonutNew.FromJson(new VkResponse(model.Object));
                    _vkApi.Messages.Send(new MessagesSendParams()
                    {
                        RandomId = new DateTime().Millisecond,
                        PeerId = -1 * groupId,
                        UserId = objectResponse.UserId,
                        Message = "Подписка куплена."
                    });
                }   
                else if (model.Type == "donut_subscription_prolonged")
                {

                }
                else if (model.Type == "donut_subscription_expired")
                {

                }
                else if (model.Type == "donut_subscription_cancelled")
                {

                }    
                else if (model.Type == "donut_subscription_price_changed")
                {

                }
            }

            return BadRequest();
        }
    }
}
