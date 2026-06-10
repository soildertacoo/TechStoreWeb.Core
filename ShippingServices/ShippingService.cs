using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using TechStore.Models;
using TechStore.Models.ModelShipping;

namespace TechStoreWeb.Core.ShippingServices{
    public interface IShippingService
    {
        Task<string> CreateGHN(OrderPro order,ShippingProvider? provider);
    }

    public class GhnShippingService: IShippingService
    {
        private readonly HttpClient _httpClient;

        public GhnShippingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> CreateGHN(OrderPro order, ShippingProvider? provider)
        {
            ghnOrder request = new ghnOrder {
                PaymentTypeId = 2,
                Note = "Tintest 123",
                RequiredNote = "KHONGCHOXEMHANG",
                ReturnPhone = "0123456784",
                ReturnAddress = "TNMMT TechSoTo, 1234 Ly Kiet, p.My Hanh",
                ReturnDistrictId = null,
                ReturnWardCode = null, 
                ClientOrderCode = null,
                ToName = order.Customer.NameCus,
                ToPhone = order.Customer.PhoneCus,
                ToAddress = order.AddressDeliverry,
                ToWardCode = "20308",
                ToDistrictId = 1444,
                CodAmount = 200000,
                Content = $"Don hang dummy co ma la {order.TrackingNumber} duoc dat vao ngay {order.DeliveryDate}, day la don hang dummy,test kiem thu app nen la shipper ko toi lay hang ",
                Weight = 200,
                Length = 1,
                Width = 19,
                Height = 10,
                CodFailedAmount = 2000,
                PickStationId = 1444,
                InsuranceValue = 10000000, 
                ServiceId = 0, 
                ServiceTypeId = 2,
                Coupon = null,
                PickShift = new List<int> { 2 },
                Items =  order.OrderDetails.Select(detail => new GhnItem
                {
                    Name = detail.Products.NamePro,
                    Quantity = detail.Quantity,
                    Price = detail.Subtotal
                }).ToList()
            };

            //Thêm header Token vào 
            _httpClient.DefaultRequestHeaders.Add("token",provider?.ApiToken);
            // Gọi API tạo đơn hàng, tra json ve
            var response = await _httpClient.PostAsJsonAsync(provider?.ApiCreateOrder, request); // trả kết quả về full json khi dùng PostAsSync 
            var result = await response.Content.ReadFromJsonAsync<GhnCreateOrderResponse<GhnOrderData>>();
            response.EnsureSuccessStatusCode();
            if (result?.Code == 200 && result.Data != null)
            {
                // Trả về mã vận đơn (Tracking code)
                return result.Data.OrderCode; 
            }

            return "";
        }
    }
}
