using SelectPdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks; // Thêm thư viện này để chạy bất đồng bộ
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore; // Thêm thư viện này để dùng .Include()
using TechStore.Models;

namespace TechStore.Controllers
{
    public class PrintController : Controller
    {
        private readonly DBTechStoreEntities db;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;

        // BƯỚC 1: Tiêm DbContext và các công cụ Render View của .NET Core vào Constructor
        public PrintController(
            DBTechStoreEntities dbContext, 
            ICompositeViewEngine viewEngine, 
            ITempDataProvider tempDataProvider)
        {
            db = dbContext;
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
        }

        // GET: Print
        // Đổi thành hàm bất đồng bộ (async Task) vì render trong .NET Core bắt buộc là async
        public async Task<ActionResult> HTML2PDF_Order(string FileName, int idOrder, string viewName= "OrderInvoice")
        {
            var model = GetHoaDonModelByOrderId(idOrder);
            if (model == null) return NotFound();

            // Render view html truc tiep sang string 
            string html = await RenderViewToStringAsync(viewName, model);
            
            HtmlToPdf converter = new HtmlToPdf();
            PdfDocument doc = converter.ConvertHtmlString(html);

            byte[] pdf = doc.Save();
            doc.Close();
            return File(pdf, "application/pdf", FileName);
        }

        //  hàm chuyen view html sang string 
        private async Task<string> RenderViewToStringAsync(string viewName, object model)
        {
            ViewData.Model = model;

            using (var sw = new StringWriter())
            {
                // Tìm file giao diện (.cshtml)
                var viewResult = _viewEngine.FindView(ControllerContext, viewName, false);

                if (viewResult.View == null)
                {
                    throw new ArgumentNullException($"{viewName} không tồn tại trong thư mục Views!");
                }

                var viewDictionary = new ViewDataDictionary(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
                {
                    Model = model
                };

                // Cấu hình môi trường giả lập để vẽ HTML
                var viewContext = new ViewContext(
                    ControllerContext,
                    viewResult.View,
                    viewDictionary,
                    new TempDataDictionary(ControllerContext.HttpContext, _tempDataProvider),
                    sw,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return sw.ToString();
            }
        }

        public HoaDonModel GetHoaDonModelByOrderId(int orderId)
        {
       
            var order = db.OrderPro.Include(o => o.Customer).FirstOrDefault(o => o.ID == orderId);
            if (order == null) return null;

            var OrderDetails = db.OrderDetails.Include(od => od.Products).Where(od => od.IDOrder == orderId).ToList();

            var model = new HoaDonModel
            {
                ID = order.ID,
                TrackingNumber = order.TrackingNumber,
                CustomerName = order.Customer?.NameCus, // Dùng ?. cho an toàn
                AddressDeliverry = order.AddressDeliverry,
                DateOrder = order.DateOrder,
                DeliveryDate = order.DeliveryDate,
                Status = order.Status,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                TotalAmount = order.TotalAmount,
                ShippingCost = order.ShippingCost,
                Products = OrderDetails.Select(od => new HoaDonProductsModel
                {
                    ProductsName = od.Products?.NamePro, // Dùng ?. cho an toàn
                    ImagePro = od.Products?.ImagePro,
                    UnitPrice = (double)od.UnitPrice,
                    Quantity = (int)od.Quantity
                }).ToList()
            };

            return model;
        }
    }
}