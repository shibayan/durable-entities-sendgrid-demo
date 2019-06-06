using Newtonsoft.Json;

namespace MailSender.Models
{
    public class EventPayload
    {
        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("entitykey")]
        public string EntityKey { get; set; }
    }
}