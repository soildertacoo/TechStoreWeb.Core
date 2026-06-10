using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace TechStore.Controllers
{
    public class BaseController : Controller
    {
        // GET: Base
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            string? session = HttpContext.Session.GetString("admin");
            if (session == null)
            {
                filterContext.Result = RedirectToAction("Login", "Admins");
            }
            base.OnActionExecuting(filterContext);
        }
    }
}