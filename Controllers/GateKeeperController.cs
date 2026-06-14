using Microsoft.AspNetCore.Mvc;
using TechStore.Models;

namespace TechStore.Controllers
{
    public class GateKeeperController: Controller 
    {
        [HttpGet]
        public ActionResult turnstileSafeGuard()
        {
            return View();
        }
        
        [HttpPost]
        public async Task<IActionResult> SubmitEndpoint([FromBody] JSONToken data)
        {
            if (data == null)
            {
                return Json(new {success =  false});
            }
            var (IsValid, Message) = await TurnstileCaptcha.IsValid(data.token);
            if (!IsValid)
            {
                Console.WriteLine(Message);
                return Json(new {success =  false, message ="Xác thực đã gửi lên server nhưng hệ thống ko chấp nhận, nhấn ok để reload lại form"});
            }
            //Tạo cookie tạm để mỗi khi vào web 
            // CookieOptions options = new CookieOptions
            // {
                
            //     HttpOnly = true // Bảo mật: Không cho JavaScript đọc được cookie này
            // };
            // Response.Cookies.Append("IsHumanValidated", "true", options);

            //Lưu session vào để lưu cái captcha 
            HttpContext.Session.SetString("IsHumanValidated", "true");
            return Json(new {success =  true});
        }

        
    }
    public class JSONToken
    {
        public string token {get;set;}
    }
}