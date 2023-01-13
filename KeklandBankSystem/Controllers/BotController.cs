using KeklandBankSystem.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VkNet.Abstractions;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;
using VkNet.Utils;

namespace KeklandBankSystem.Controllers
{
    public class EasyText
    {
        public int Ret { get; set; }
        public string[] Args { get; set; }
    }

    public class Command
    {
        public string[] command { get; set; }
        public int Id { get; set; }
    }

    public class MessageSender
    {
        private Message message;
        private readonly IVkApi _vkApi;
        private readonly Random rnd;

        public EasyText EasyText()
        {
            var massive = new Command[]
            {
                new Command { command = new string[] { "getVk", "!getvk", "Получить профиль", "getvk", "!getVk" }, Id = 0 }
            };

            var text = message.Text.Replace("[club191094689|@kekland_bank] ", "").Replace("[club191094689|Мемцарская Республика] ", "");

            var ret = -1;
            var rets = new EasyText();

            var args = text.Split(" ");

            foreach (var r in massive)
            {
                foreach (var s in r.command)
                {
                    if (text.Contains(s))
                    {
                        if (text.IndexOf(s) == 0)
                        {
                            ret = r.Id;
                        }
                    }
                }
            }

            rets.Ret = ret;
            rets.Args = args;

            return rets;
        }

        public MessageSender(IVkApi vkApi, Updates updates)
        {
            _vkApi = vkApi;
            message = Message.FromJson(new VkResponse(updates.Object));
            rnd = new Random();
        }

        public void Send(string msg, MessageKeyboard keyboard = null, bool mention = true)
        {
            if (keyboard == null)
            {
                _vkApi.Messages.Send(new MessagesSendParams
                {
                    RandomId = rnd.Next(0, 999999999),
                    PeerId = message.PeerId.Value,
                    Message = msg,
                    DisableMentions = mention
                });
            }
            else
            {
                _vkApi.Messages.Send(new MessagesSendParams
                {
                    RandomId = rnd.Next(0, 999999999),
                    PeerId = message.PeerId.Value,
                    Message = msg,
                    Keyboard = keyboard,
                    DisableMentions = mention
                });
            }
        }
    }

    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IVkApi _vkApi;
        private readonly IBankServices _bankServices;
        //private readonly IBotServices _botServices;
        private readonly string _sep = "\n";

        public BotController(IVkApi vkApi, IConfiguration configuration, IBankServices bankServices/*, IBotServices botServices*/)
        {
            _vkApi = vkApi;
            _configuration = configuration;
            _bankServices = bankServices;
            //_botServices = botServices;
        }

        [NonAction]
        public async Task CheckMessage(Updates updates)
        {
            var vkResponse = new VkResponse(updates.Object);
            var message = Message.FromJson(vkResponse);
            var user = _vkApi.Users.Get(new long[] { (long)message.FromId }).FirstOrDefault();

            var ms = new MessageSender(_vkApi, updates);

            var et = ms.EasyText();

            switch (et.Ret)
            {
                case 0:
                    try
                    {
                        ms.Send("test");
                    }
                    catch(Exception e)
                    {
                        ms.Send(e.Message);
                    }

                    break;
            }

            //var botUser = _botServices.GetBotUser((long)message.FromId);

            /*if (botUser == null)
            {
                ms.Send("⛔ [id" + message.FromId + "|" + user.FirstName + "], вы впервые в системе, проходим регистрацию...");
                _botServices.CreateUserBot(new BotUser()
                {
                    Gold = 0,
                    Titul = "black",
                    VkId = (long)message.FromId,
                    Legs = 2,
                    BaseDamage = 1,
                    BaseHealth = 1,
                    BaseIntellect = 0
                });

                var botUserCreated = _botServices.GetBotUser((long)message.FromId);

                _botServices.CreateUserItem(new UserItem()
                {
                    ItemId = 2,
                    UserId = botUserCreated.Id
                });


                ms.Send("✅ [id" + message.FromId + "|" + user.FirstName + "], регистрация пройдена! Ваш уникальный индификатор: " + botUserCreated.Id);
            }

            if (ms.EasyText(new string[] { "profile", "!profile", "Профиль", "профиль" }).Ret)
            {
                string str = "";
                switch (botUser.Titul)
                {
                    case "black":
                        str = "👑 [id" + message.FromId + "|" + user.FirstName + "], Эээ... Ты кто? От тебя воняет, долго тут не задерживайся!" + _sep + _sep +
                        "Титул: 🧦 Чёрнь" + _sep;
                        break;
                    case "robo":
                        str = "👑 [id" + message.FromId + "|" + user.FirstName + "], Работяга, заходи в нашу таверну, попей хмельного пива!" + _sep + _sep +
                        "Титул: ⛏ Рабочий класс" + _sep;
                        break;
                    case "arist":
                        str = "👑 [id" + message.FromId + "|" + user.FirstName + "], Милорд, проходите, присаживайтесь, мы рады вас видеть!" + _sep + _sep +
                        "Титул: 🎩 Аристократия" + _sep;
                        break;
                    case "king":
                        str = "👑 [id" + message.FromId + "|" + user.FirstName + "], Король! *Падает в ноги* Мой Повелитель..." + _sep + _sep +
                        "Титул: 👑 Король" + _sep;
                        break;
                    case "admin":
                        str = "👑 [id" + message.FromId + "|" + user.FirstName + "], Синхронизация с администрацией..." + _sep + _sep +
                        "Титул: 💻 Администратор" + _sep;
                        break;
                }

                str += "💰 Золото: " + botUser.Gold + _sep;
                str += "↔ Уникальный индификатор: " + botUser.Id + _sep + _sep;
                str += "Статистика персонажа:" + _sep;

                var items = _botServices.GetUserEq(botUser);

                int heal = items.Sum(m => m.AddedHealth);
                str += "❤ Жизни: " + (botUser.BaseHealth + heal) + " ( Базовая: " + botUser.BaseHealth + " )" + _sep;

                int damage = items.Sum(m => m.AddedDamage);
                str += "⚔ Урон: " + (botUser.BaseDamage + damage) + " ( Базовая :" + botUser.BaseDamage + " )" + _sep;

                int intelect = items.Sum(m => m.AddedIntelect);
                str += "🎓 Интелект: " + (botUser.BaseIntellect + intelect) + " ( Базовая: " + botUser.BaseIntellect + " )" + _sep;
                str += "Экипировка ( !экипировка или !equipment )";

                var keyboard = new MessageKeyboard
                {
                    Inline = true,
                    OneTime = false,
                    Buttons = new List<List<MessageKeyboardButton>>
                            {
                                new List<MessageKeyboardButton>
                                {
                                    new MessageKeyboardButton
                                    {
                                        Action = new MessageKeyboardButtonAction
                                        {
                                            Type = KeyboardButtonActionType.Text,
                                            Label = "!экипировка",
                                        },
                                        Color = KeyboardButtonColor.Default
                                    }

                                }

                            }
                };

                ms.Send(str, keyboard);
            }
            else if (ms.EasyText(new string[] { "!экипировка", "!equipment" }).Ret)
            {
                string str = "👑 [id" + message.FromId + "|" + user.FirstName + "], ваша экпировка:" + _sep + _sep;

                var head = _botServices.GetItem(botUser.Head);
                if (head != null)
                    str += "Голова: " + head.Name + " ( " + head.Id + " ). Описание: " + head.Info + _sep;
                else
                    str += "Голова: ничего" + _sep;

                var body = _botServices.GetItem(botUser.Body);
                if (body != null)
                    str += "Тело: " + body.Name + " ( " + body.Id + " ). Описание: " + body.Info + _sep;
                else
                    str += "Тело: ничего" + _sep;

                var legs = _botServices.GetItem(botUser.Legs);
                if (legs != null)
                    str += "Ноги: " + legs.Name + " ( " + legs.Id + " ). Описание: " + legs.Info + _sep;
                else
                    str += "Ноги: ничего" + _sep;

                var weapon = _botServices.GetItem(botUser.Weapon);
                if (weapon != null)
                    str += "Оружие: " + weapon.Name + " ( " + weapon.Id + " ). Описание: " + weapon.Info + _sep;
                else
                    str += "Оружие: ничего" + _sep;

                var accesuar = _botServices.GetItem(botUser.Accesuar);
                if (accesuar != null)
                    str += "Аксессуар: " + accesuar.Name + " ( " + accesuar.Id + " ). Описание: " + accesuar.Info + _sep + _sep;
                else
                    str += "Аксессуар: ничего" + _sep + _sep;

                str += "Что бы посмотреть полный список ваших предметов введите: '!Мои предметы'. Что бы посмотреть полную информацию о предмете или же продать его введите: '!Вещь <id>'";

                var keyboard = new MessageKeyboard
                {
                    Inline = true,
                    OneTime = false,
                    Buttons = new List<List<MessageKeyboardButton>>
                            {
                                new List<MessageKeyboardButton>
                                {
                                    new MessageKeyboardButton
                                    {
                                        Action = new MessageKeyboardButtonAction
                                        {
                                            Type = KeyboardButtonActionType.Text,
                                            Label = "!Мои предметы",
                                        },
                                        Color = KeyboardButtonColor.Default
                                    }

                                }

                            }
                };

                ms.Send(str, keyboard);

            }
            else if (ms.EasyText(new string[] { "!Надеть" }).Ret)
            {
                var args = ms.EasyText(new string[] { "!Надеть" }).Args;

                string str = "";

                if (args.Count() > 2)
                {
                    str = "⛔ [id" + message.FromId + "|" + user.FirstName + "], неправильные аргументы.";
                    ms.Send(str);
                    return;
                }

                int id; bool isn = Int32.TryParse(args[1], out id);

                if (!isn)
                {
                    str = "⛔ [id" + message.FromId + "|" + user.FirstName + "], id должено быть числом.";
                    ms.Send(str);
                    return;
                }

                var item = _botServices.GetItem(Convert.ToInt32(args[1]));

                var keyboard = new MessageKeyboard
                {
                    Inline = true,
                    OneTime = false,
                    Buttons = new List<List<MessageKeyboardButton>>
                            {
                                new List<MessageKeyboardButton>
                                {
                                    new MessageKeyboardButton
                                    {
                                        Action = new MessageKeyboardButtonAction
                                        {
                                            Type = KeyboardButtonActionType.Text,
                                            Label = "!экипировка",
                                        },
                                        Color = KeyboardButtonColor.Default
                                    }

                                }

                            }
                };

                if (item.Type == "Head")
                    botUser.Head = item.Id;
                else if (item.Type == "Body")
                    botUser.Body = item.Id;
                else if (item.Type == "Legs")
                    botUser.Legs = item.Id;
                else if (item.Type == "Weapon")
                    botUser.Weapon = item.Id;
                else if (item.Type == "Accesuar")
                    botUser.Accesuar = item.Id;

                _botServices.SaveUser(botUser);

                ms.Send("✅ [id" + message.FromId + "|" + user.FirstName + "], успешно надето!", keyboard);
            }
            else if (ms.EasyText(new string[] { "!Вещь" }).Ret)
            {
                var args = ms.EasyText(new string[] { "!Вещь" }).Args;

                string str = "";

                if (args.Count() > 2)
                {
                    str = "⛔ [id" + message.FromId + "|" + user.FirstName + "], неправильные аргументы.";
                    ms.Send(str);
                    return;
                }

                int id; bool isn = Int32.TryParse(args[1], out id);

                if (!isn)
                {
                    str = "⛔ [id" + message.FromId + "|" + user.FirstName + "], id должено быть числом.";
                    ms.Send(str);
                    return;
                }

                var userItem = _botServices.GetUserItem(Convert.ToInt32(args[1]));

                if (userItem.UserId != botUser.Id)
                {
                    str = "⛔ [id" + message.FromId + "|" + user.FirstName + "], вы не владеете данным предметом.";
                    ms.Send(str);
                    return;
                }

                var item = _botServices.GetItem(Convert.ToInt32(args[1]));


                str += "👑 [id" + message.FromId + "|" + user.FirstName + "], ваш предмет: " + _sep;
                str += "Название: " + item.Name + " ( " + item.Id + " )" + _sep;
                str += "Информация: " + item.Info + _sep;
                str += "Стоимость: " + item.Cost + _sep + _sep;
                str += "❤ Жизни: " + item.AddedHealth + _sep;
                str += "⚔ Урон: " + item.AddedDamage + _sep;
                str += "🎓 Интелект: " + item.AddedIntelect + _sep + _sep;

                str += "Что бы надеть предмет введите: !Надеть <id>";

                ms.Send(str);
            }
            else if (ms.EasyText(new string[] { "!Мои предметы" }).Ret)
            {
                string str = "👑 [id" + message.FromId + "|" + user.FirstName + "], ваша кладовая:" + _sep + _sep;

                var items = _botServices.GetUserItems(botUser);

                if (items.Count() > 0)
                {
                    foreach (var i in items)
                    {
                        str += i.Name + " ( " + i.Id + " )" + _sep;
                    }
                }
                else
                {
                    str += "У вас тут пусто..." + _sep;
                }

                str += _sep + "Что бы посмотреть полную информацию о предмете или же продать его введите: '!Вещь <id>'";

                ms.Send(str);

            }
            else if (ms.EasyText(new string[] { "!FAQ" }).Ret)
            {
                var keyboard = new MessageKeyboard
                {
                    Inline = true,
                    OneTime = false,
                    Buttons = new List<List<MessageKeyboardButton>>
                            {
                                new List<MessageKeyboardButton>
                                {
                                    new MessageKeyboardButton
                                    {
                                        Action = new MessageKeyboardButtonAction
                                        {
                                            Type = KeyboardButtonActionType.Text,
                                            Label = "!Основная информация",
                                        },
                                        Color = KeyboardButtonColor.Default
                                    },
                                    new MessageKeyboardButton
                                    {
                                        Action = new MessageKeyboardButtonAction
                                        {
                                            Type = KeyboardButtonActionType.Text,
                                            Label = "!Титулы и развитие",
                                        },
                                        Color = KeyboardButtonColor.Default
                                    }
                                },
                                new List<MessageKeyboardButton>
                                {
                                    new MessageKeyboardButton
                                    {
                                        Action = new MessageKeyboardButtonAction
                                        {
                                            Type = KeyboardButtonActionType.Text,
                                            Label = "!Военное дело",
                                        },
                                        Color = KeyboardButtonColor.Default
                                    },
                                    new MessageKeyboardButton
                                    {
                                        Action = new MessageKeyboardButtonAction
                                        {
                                            Type = KeyboardButtonActionType.Text,
                                            Label = "!Королевство и работы",
                                        },
                                        Color = KeyboardButtonColor.Default
                                    }
                                }
                            }
                };

                string str = "👑 Добро пожаловать в FAQ, выберите раздел:" + _sep + _sep +
                    "🔔 » Основная информация" + _sep +
                    "🎩 » Титулы и развитие" + _sep +
                    "🐲 » Королевство и работы" + _sep +
                    "⚔ » Военное дело и боссы";

                ms.Send(str, keyboard);
            }
            else if (ms.EasyText(new string[] { "!Военное дело" }).Ret)
            {
                var keyboard = new MessageKeyboard
                {
                    Inline = true,
                    OneTime = false,
                    Buttons = new List<List<MessageKeyboardButton>>
                            {
                                new List<MessageKeyboardButton>
                                {
                                    new MessageKeyboardButton
                                    {
                                    Action = new MessageKeyboardButtonAction
                                    {
                                        Type = KeyboardButtonActionType.Text, //Тип кнопки клавиатуры
                                        Label = "!FAQ", //Надпись на кнопке
                                    },
                                    Color = KeyboardButtonColor.Default //Цвет кнопки
                                    }
                                }
                            }
                };

                string str = "👑 Военное дело - одно из основных занятий игроков, в основном это походы." + _sep + _sep +
                    "Военный походы на каких-либо мобов и боссов, возглавляемые одним из полководцев, назначенных королём." + _sep +
                    "Все игроки должны иметь снаряжение, которое они могут купить в магазинах или у других игроков." + _sep +
                    "Чем сильнее снаряжение, тем больше у вас шансов выжить при битве с мобами." + _sep +
                    "За победу вам будут выдаваться награда, снаряжение и деньги." + _sep +
                    "Всё что от вас требуется - выполнять приказы военоначальника и смотреть за поведением босса." + _sep +
                    "Дело в том, что боссы имеют тексты поведение, к примеру если бот напшет 'Дракон приподнял голову и начал рычать' - скоро он начнёт жечь огнём, а значит нужно отойти." + _sep +
                    "Если вы не отойдете - вы погибните, и окончите ваш поход без награды. Полководец всегда говорит, что нужно делать.";

                ms.Send(str, keyboard);
            }
            else if (ms.EasyText(new string[] { "!Королевство и работы" }).Ret)
            {
                var keyboard = new MessageKeyboard
                {
                    Inline = true,
                    OneTime = false,
                    Buttons = new List<List<MessageKeyboardButton>>
                            {
                                new List<MessageKeyboardButton>
                                {
                                    new MessageKeyboardButton
                                    {
                                    Action = new MessageKeyboardButtonAction
                                    {
                                        Type = KeyboardButtonActionType.Text, //Тип кнопки клавиатуры
                                        Label = "!FAQ", //Надпись на кнопке
                                    },
                                    Color = KeyboardButtonColor.Default //Цвет кнопки
                                    }
                                }
                            }
                };

                string str = "👑 Все работают - от черни до короля, только все это делают в разных объёмах, и по-разому зарабатывают." + _sep + _sep +
                    "Король и аристократия зарабатывают владея землями и бизнесами, а король собирает налог в казну королевства, получая процент." + _sep +
                    "Чёрнь и рабочий класс зарабатывают работая на землях аристократов, или же выполняя военные задания полководцев.";

                ms.Send(str, keyboard);
            }
            else if (ms.EasyText(new string[] { "!Титулы и развитие" }).Ret)
            {
                var keyboard = new MessageKeyboard
                {
                    Inline = true,
                    OneTime = false,
                    Buttons = new List<List<MessageKeyboardButton>>
                            {
                                new List<MessageKeyboardButton>
                                {
                                    new MessageKeyboardButton
                                    {
                                    Action = new MessageKeyboardButtonAction
                                    {
                                        Type = KeyboardButtonActionType.Text, //Тип кнопки клавиатуры
                                        Label = "!FAQ", //Надпись на кнопке
                                    },
                                    Color = KeyboardButtonColor.Default //Цвет кнопки
                                    }
                                }
                            }
                };

                string str = "Как и полагается любому средневековью - основа жизни тут разделение людей на классы." + _sep + _sep +
                    "👑 Король - основной и главный титул, пренадлежащий одному человеку, который меняется каждую сессию. Выбирают только самых лучших" + _sep +
                    "Как и полагает королю - он может делать всё, может казнить, может помиловать, может начать военный поход и выдавать титулы." + _sep + _sep +
                    "🎩 Аристократия - лучшие бизнесмены, лучшие полководцы и владельцы земли или просто приблеженный к королю люди. В основном богаты и имеют земли с крестьянами." + _sep + _sep +
                    "⛏ Рабочий класс - трудятся на землях аристократов, зарабатывают им и себе деньги. Обычные работяги, у которых есть все шансы стать аристакратом в будущем." + _sep +
                    "Из рабочего класса можно перейти в аристократию только с разрешением Короля." + _sep + _sep +
                    "🧦 Чёрнь - самое дно, вы никто - и вы никому не нужны. Либо смиритесь, либо боритесь. Стать рабочим вы можете с разрешение Аристократии или Короля.";

                ms.Send(str, keyboard);
            }
            else if (ms.EasyText(new string[] { "!Основная информация" }).Ret)
            {
                var keyboard = new MessageKeyboard
                {
                    Inline = true,
                    OneTime = false,
                    Buttons = new List<List<MessageKeyboardButton>>
                            {
                                new List<MessageKeyboardButton>
                                {
                                    new MessageKeyboardButton
                                    {
                                    Action = new MessageKeyboardButtonAction
                                    {
                                        Type = KeyboardButtonActionType.Text, //Тип кнопки клавиатуры
                                        Label = "!FAQ", //Надпись на кнопке
                                    },
                                    Color = KeyboardButtonColor.Default //Цвет кнопки
                                    }
                                }
                            }
                };

                string str = "👑 Представте вы живете в средневековье. Ваша главная цель стать королём и стать богатым." + _sep + _sep +
                    "Для этого существует большое количество вариантов развития, к примеру вы можете стать военным и убивать боссов." + _sep +
                    "Или же вы можете стать рыцарем и выиграть турниры на деньги." + _sep +
                    "Или же создать свой бизнес, мастерскую или ферму, и зарабатывать на ней деньги. А так же спикулировать вещами." + _sep +
                    "В конце концов вы можете задонатить Мемлары и стать самым богатым пользователем. Всё в ваших руках";

                ms.Send(str, keyboard);
            }
            else if (ms.EasyText(new string[] { "помощь", "help", "!help", "!помощь" }).Ret)
            {
                var keyboard = new MessageKeyboard
                {
                    Inline = true,
                    OneTime = false,
                    Buttons = new List<List<MessageKeyboardButton>>
                            {
                                new List<MessageKeyboardButton>
                                {
                                    new MessageKeyboardButton
                                    {
                                    Action = new MessageKeyboardButtonAction
                                    {
                                        Type = KeyboardButtonActionType.Text, //Тип кнопки клавиатуры
                                        Label = "!FAQ", //Надпись на кнопке
                                    },
                                    Color = KeyboardButtonColor.Default //Цвет кнопки
                                    }
                                }
                            }
                };

                string str = "👑 Вас приветствует ТМЛ бот 1.0" + _sep +
                    "Данный бот предназначен для небольшой игры, по типу средневековья." + _sep +
                    "Тут можно строить здания, воевать, стать королём и убивать монстров, зарабатывая деньги." + _sep + _sep +
                    "💰 Помните о том, что вся функциональность связана с аккаунтом ТМЛ." + _sep +
                    "То есть вы сможете как зарабатывать деньги в банк, так и покупать за кеклары что-то ( снаряжение, недвижимость )" + _sep +
                    "Можете считать что это настоящая MMORPG, которая будет обновлятся со временем." + _sep +
                    "Что бы узнать более подробную информация введите: '!FAQ'" + _sep + _sep +
                    "Автор и разработчик: [dezare|Павел Павлов].";

                ms.Send(str, keyboard);
            }*/
        }

        /*[HttpPost("callback")]
        public async Task<IActionResult> Callback([FromBody] Updates updates)
        {
            if (updates.Secret == "dezare_kekbank_0183kJFM4ND0dj")
            {
                switch (updates.Type)
                {
                    case "confirmation":
                        return Ok(_configuration["ConfigBot:Confirmation"]);

                    case "message_new":
                        {
                            await CheckMessage(updates);
                            break;
                        }
                }
                return Ok("ok");
            }
            else return StatusCode(401);
        }*/
    }
}
