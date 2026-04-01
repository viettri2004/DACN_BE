using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace src.Shared.Infrastructure
{
    public static class JsonSettings
    {
        public static readonly JsonSerializerSettings CamelCase = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
    }
}
