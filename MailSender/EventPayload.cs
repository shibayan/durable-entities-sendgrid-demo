using Newtonsoft.Json;

namespace FunctionApp63
{
    public class EventPayload
    {
        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("entitykey")]
        public string EntityKey { get; set; }
    }
}