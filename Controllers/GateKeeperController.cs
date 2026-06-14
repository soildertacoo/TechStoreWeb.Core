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
            return Json(new {success =  true});
        }

        
    }
    public class JSONToken
    {
        public string token {get;set;}
    }
}