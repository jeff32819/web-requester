// ReSharper disable UnusedMember.Global
namespace WebRequesterDll.Models
{
    public class HttpReponseStatus
    {
        public int StatusCode { get; set; } = 0;
        public string ErrorCode { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
    }
}
