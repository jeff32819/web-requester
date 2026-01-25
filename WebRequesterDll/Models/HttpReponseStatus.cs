// ReSharper disable UnusedMember.Global
namespace WebRequesterDll.Models
{
    public class HttpReponseStatus
    {
        public int StatusCode { get; set; } = 0;
        public MyEnum.RequestErrorCodeEnum ErrorCode { get; set; } = MyEnum.RequestErrorCodeEnum.None;
        public string ErrorMessage { get; set; } = "";
    }
}
