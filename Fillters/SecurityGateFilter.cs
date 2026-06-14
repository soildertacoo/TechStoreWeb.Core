using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TechStore.Filters
{
    public class SecurityGateFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // 1. Xem người dùng đang muốn đi vào Controller nào
            var controllerName = context.RouteData.Values["controller"]?.ToString();

            // 2. NGOẠI LỆ ĐẶC BIỆT: Nếu họ đang ở đúng cái phòng Xác thực (GateKeeper) 
            // thì phải thả cho họ đi, nếu không sẽ bị lặp vô hạn (chuyển hướng vòng tròn)
            if (string.Equals(controllerName, "GateKeeper", StringComparison.OrdinalIgnoreCase))
            {
                base.OnActionExecuting(context);
                return;
            }

            // 3. Kiểm tra xem trong túi (Cookie) có thẻ thông hành chưa
            var hasPassed = context.HttpContext.Request.Cookies["IsHumanValidated"];

            if (string.IsNullOrEmpty(hasPassed))
            {
                // Chưa có thẻ -> trang Xác thực Captcha
                context.Result = new RedirectToActionResult("Index", "GateKeeper", null);
            }

            base.OnActionExecuting(context);
        }
    }
}