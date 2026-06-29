using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using TechStore.Models;
using TechStore.Models.ModelShipping;
using System.Net.Http.Headers;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace TechStoreWeb.Core.ShippingServices{
    public interface IShippingService
    {
        Task<string> CreateGHN(OrderPro order,ShippingProviders? provider);
    }

    public class GhnShippingService: IShippingService
    {
        private readonly HttpClient _httpClient;
        public string token = "";

        public GhnShippingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

    public async Task<string> CreateGHN(OrderPro order, ShippingProviders? provider)
    {
        try 
        {
            // 1. KIỂM TRA ĐẦU VÀO NGAY TỪ ĐẦU (Đừng để code chạy xuống dưới mới check)
            if (order == null) 
            {
                throw new Exception($"Dữ liệu đơn hàng (order) truyền vào bị null.");
            }
            if (provider == null) 
            {
                throw new Exception("Dữ liệu ĐVVC (provider) truyền vào bị null.");
            }


            // 2. DÙNG TOÁN TỬ ?. VÀ ?? ĐỂ "BỌC THÉP" DỮ LIỆU
            string [] addresssDivided = order.AddressDeliverry.Split(',');
            //Debug 
            foreach (string i in addresssDivided)
                {
                    Console.WriteLine("Dia chi duoc cat ra 36" + i.Trim());
                }
            token = provider.ApiToken;
            ghnOrder request = new ghnOrder 
            {
                PaymentTypeId = 2,
                Note = "Tintest 123",
                RequiredNote = "KHONGCHOXEMHANG",
                ReturnPhone = "0999482999",
                ReturnAddress = "72 Thành Thái, Phường Tân Tạo, Hồ Chí Minh, Vietnam",
                FromPhone = "0999482999",
                FromAddress = "72 Thành Thái, Phường Tân Tạo, Hồ Chí Minh, Vietnam",
                FromWardName="Phường Tân Tạo",
                FromProvinceName = "Hồ Chí Minh",
                // Xử lý an toàn cho thông tin khách hàng và địa chỉ
                ToName = order.Customer?.NameCus ?? "Khách hàng",
                ToPhone = order.Customer?.PhoneCus ?? "0739992999",
                // ToPhone = "0739992999",
                IsNewToAddress = true,
                IsNewFromAddress = true,
                ToAddress = order.AddressDeliverry ?? "Chưa có địa chỉ",
                ToWardName = addresssDivided.Length >= 3 ? addresssDivided[1].Trim() : "Không có phường",
                // ToDistrictName = addresssDivided.Length >= 3 ? addresssDivided[1].Trim() : "Không có huyện quận",
                ToProvinceName = addresssDivided.Length >= 3 ? addresssDivided[2].Trim() : "Khong có tỉnh thành phố",
                ToWardCode = addresssDivided.Length >= 3 ? await getWardCode(addresssDivided[1].Trim(), addresssDivided[2].Trim()): "",
                CodAmount = 200000,
                Content = $"Don hang dummy co ma la {order.TrackingNumber} duoc dat vao ngay {order.DeliveryDate}, day la don hang dummy,test kiem thu app nen la shipper ko toi lay hang",
                Weight = 200,
                Length = 1,
                Width = 19,
                Height = 10,
                CodFailedAmount = 2000,
                // PickStationId = 1444,
                InsuranceValue = 10000000, 
                ServiceTypeId = 2,
                Coupon = null,
                PickShift = new List<int> { 2 },
                
                // Xử lý an toàn cho list sản phẩm (Items)
                Items = order.OrderDetails != null 
                    ? order.OrderDetails.Select(detail => new GhnItem
                    {
                        Name = detail.Products?.NamePro ?? "Sản phẩm",
                        Quantity = detail.Quantity,
                        Price = detail.Subtotal
                    }).ToList()
                    : new List<GhnItem>()
            };

            // // 3. XÓA HEADER CŨ TRƯỚC KHI THÊM MỚI (Cực kỳ quan trọng để tránh lỗi Duplicate Header)
            _httpClient.DefaultRequestHeaders.Remove("Token");            
            if (!string.IsNullOrEmpty(provider.ApiToken))
            {
                _httpClient.DefaultRequestHeaders.Add("Token", provider.ApiToken);
                _httpClient.DefaultRequestHeaders.Add("ShopId", "200501");
            }
            else
            {
                throw new Exception("Token của provider bị rỗng.");
            }

            // 4. GỌI API GHN, trả kết quả json csharp 
            var response = await _httpClient.PostAsJsonAsync(provider.ApiCreateOrder, request); 
            
            // Đảm bảo request thành công (Status Code 200-299)
            if (!response.IsSuccessStatusCode) 
            {
                // Đọc thẳng câu báo lỗi chi tiết từ GHN
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"GHN TỪ CHỐI ĐƠN HÀNG (Lỗi {response.StatusCode}): {errorContent}");
            }
            
            var result = await response.Content.ReadFromJsonAsync<GhnCreateOrderResponse<GhnOrderData>>();
            
            if (result?.Code == 200 && result.Data != null)
            {
                // Trả về mã vận đơn (Tracking code)
                return "TS_" + provider.ProviderCode + "_" + result.Data.OrderCode; 
            }
            else
            {          
                throw new Exception($"API GHN trả về lỗi dù đã gửi đơn lên hệ thống thành công. Code: {result?.Code}, Message: {result?.Message}");
            }
        }
        catch (Exception ex)
        {
            // In toàn bộ chi tiết lỗi (bao gồm StackTrace) ra để dễ debug
            Console.WriteLine("6336: Có lỗi thực thi khi tạo đơn GHN:");
            Console.WriteLine(ex.ToString());
            return ""; 
        }
        }
        public async Task<string> getWardCode(string ward, string province)
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            // 2. Cấu hình Header cho Token
            // CÁCH A (Phổ biến nhất): Dùng Authorization với chuẩn Bearer
            // _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            // CÁCH B (Dành cho API GHN hoặc API custom): Header tên là "Token"
            if (!_httpClient.DefaultRequestHeaders.Contains("Token"))
            {
                _httpClient.DefaultRequestHeaders.Add("Token", token);
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "e3t1c2VybmFtZX19Ont7cGFzc3dvcmR9fQ==");
            }

            try
            {
                string apiURL = "https://dev-online-gateway.ghn.vn/shiip/public-api/v3/master-data/province/all";
                // 3. Thực hiện request GET
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync(apiURL, new dataRawGHN());

                // Kiểm tra xem request có thành công không (Status Code 200-299)
                // Nếu không thành công, nó sẽ ném ra Exception
                if (!response.IsSuccessStatusCode) 
                {
                    // Đọc thẳng câu báo lỗi chi tiết từ GHN
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API GHN trả về lỗi khi lay ma tinh (Lỗi {response.StatusCode}): {errorContent}");
                }
                
                var result = await response.Content.ReadFromJsonAsync<Root>();
                if (result?.Code == 200 && !result.Data.IsNullOrEmpty())
                {
                    foreach (GHNProvinceWard pro in result.Data)
                    {
                        if (pro.ExtensionNames.Contains(province.Trim().ToLower()))
                        {
                            //Nếu tìm thấy tên thành phố trùng thì lấy ID đi 
                            Console.WriteLine("Da tim thay ma tinh" + pro.Id);
                            return await getWardCode(ward, pro.Id);
                        }
                    }
                }
                else
                {
                    throw new Exception($"API GHN trả về lỗi khi lay ma tinh. Code: {result?.Code}, Message: {result?.Message}");
                }

            }
            catch (HttpRequestException e)
            {
                throw new Exception($"\nLỗi gọi API: {e.Message}");
            }
            return "";
        }
        public async Task<string> getWardCode(string ward, int proID)
        {
             _httpClient.DefaultRequestHeaders.Accept.Clear();
            // 2. Cấu hình Header cho Token
            // CÁCH A (Phổ biến nhất): Dùng Authorization với chuẩn Bearer
            // _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            // CÁCH B (Dành cho API GHN hoặc API custom): Header tên là "Token"
            if (!_httpClient.DefaultRequestHeaders.Contains("Token"))
            {
                _httpClient.DefaultRequestHeaders.Add("Token", token);
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "e3t1c2VybmFtZX19Ont7cGFzc3dvcmR9fQ==");
            }
            try
            {
                string apiURL = "https://dev-online-gateway.ghn.vn/shiip/public-api/v3/master-data/ward/all-by-province-id";
                // 3. Thực hiện request GET
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync(apiURL, new dataRawGHN{
                    ProvinceID = proID
                });

                // Kiểm tra xem request có thành công không (Status Code 200-299)
                // Nếu không thành công, nó sẽ ném ra Exception
                if (!response.IsSuccessStatusCode) 
                {
                    // Đọc thẳng câu báo lỗi chi tiết từ GHN
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Co loi khi ma phuong ve (Lỗi {response.StatusCode}): {errorContent}");
                }
                var result = await response.Content.ReadFromJsonAsync<Root>();
                if (result?.Code == 200 && !result.Data.IsNullOrEmpty())
                {
                    //In ra noi dung 
                    Console.WriteLine("Noi dung lay duoc ma phuong" + await response.Content.ReadAsStringAsync());
                    foreach (GHNProvinceWard pro in result.Data)
                    {
                        if (pro.Name?.Trim() == ward.Trim() || pro.ExtensionNames.Contains(ward.Trim().ToLower()))
                        {
                            //Nếu tìm thấy tên thành phố trùng thì lấy ID đi 
                            Console.WriteLine($"Mã của {ward} là {pro.Id}");
                            return pro.Id.ToString();
                        }
                    }
                }
                else
                {
                    throw new Exception($"API GHN trả về lỗi khi lay ma phuong. Code: {result?.Code}, Message: {result?.Message}");
                }
            }
            catch (HttpRequestException e)
            {
                throw new Exception($"\nLỗi gọi API: {e.Message}");
            }
            return "";
        }
    }
    }
