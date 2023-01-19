using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KeklandBankSystem.Model.VkApi
{
    public class VkCallBackApiRequest
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("group_id")]
        public long GroupId { get; set; }

        [JsonProperty("object")]
        public JObject Object { get; set; }

        [JsonProperty("secret")]
        public string Secret { get; set; }
    }
}
