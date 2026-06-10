using Microsoft.AspNetCore.Mvc;

namespace TechStoreWeb.Core.ViewComponents
{
    public class MiniCartViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            // Vì bạn đang dùng JS (AJAX) để gọi API lấy dữ liệu giỏ hàng,
            // nên ở phía Server chúng ta không cần truyền Model gì cả,
            // chỉ cần render ra cái khung giao diện (View) là đủ.
            return View();
        }
    }
}