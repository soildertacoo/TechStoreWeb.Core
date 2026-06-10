using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using TechStore.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Hosting; // Bắt buộc cho IWebHostEnvironment
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.AspNetCore.Mvc.Filters;
namespace TechStore.Controllers
{
    public class ShippingController : Controller
    {
        private readonly DBTechStoreEntities db;
        private readonly ApplicationDbContext _context;
        

        // Tiêm DbContext và IWebHostEnvironment vào
        public ShippingController(DBTechStoreEntities dbContext, ApplicationDbContext appContext, IWebHostEnvironment env)
        {
            db = dbContext;
            _context = appContext;
           
        }    
        [HttpGet]
        public async Task<IActionResult> testAPIProvider()
        {
            return Json(new {success = true});
        }
            
    }
}