// ReSharper disable UnusedMember.Global
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using static WebRequesterDll.MyEnum;

namespace WebRequesterDll.Models
{
    public class HttpResponseStatus
    {
        public int StatusCode { get; set; } = 0;

        [JsonConverter(typeof(StringEnumConverter))]
        public RequestErrorCodeEnum ErrorCode { get; set; } = RequestErrorCodeEnum.None;

        public string ErrorMessage { get; set; } = "";
    }
}
