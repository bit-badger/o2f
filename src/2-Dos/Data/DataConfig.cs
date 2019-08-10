using Newtonsoft.Json;

namespace Dos.Data
{
    public class DataConfig
    {
        public string Url { get; set; }
        public string Database { get; set; }
        public string Certificate { get; set; }
        public string Password { get; set; }

        [JsonIgnore]
        public string[] Urls => new[] { Url };
    }
}