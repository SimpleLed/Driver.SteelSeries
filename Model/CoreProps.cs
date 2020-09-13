using Newtonsoft.Json;

namespace SteelSeriesSLSProvider.Model
{
    internal class CoreProps
    {
        [JsonProperty(PropertyName = "address")]
        public string Address { get; set; }
    }
}
