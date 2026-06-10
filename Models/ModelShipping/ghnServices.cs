using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TechStore.Models.ModelShipping
{
    public class ghnOrder
    {
        
        [Required]
        [MaxLength(1024)]
        [JsonPropertyName("to_name")]
        public string?  ToName { get; set; }

        [Required]
        [JsonPropertyName("to_phone")]
        public string?  ToPhone { get; set; }

        [Required]
        [MaxLength(1024)]
        [JsonPropertyName("to_address")]
        public string?  ToAddress { get; set; }

        [Required]
        [JsonPropertyName("to_ward_code")]
        public string? ToWardCode { get; set; }

        [Required]
        [JsonPropertyName("to_district_id")]
        public int ToDistrictId { get; set; }

        [JsonPropertyName("return_phone")]
        public string? ReturnPhone { get; set; }

        [MaxLength(1024)]
        [JsonPropertyName("return_address")]
        public string? ReturnAddress { get; set; }

        [JsonPropertyName("return_district_id")]
        public int? ReturnDistrictId { get; set; }

        [JsonPropertyName("return_ward_code")]
        public string? ReturnWardCode { get; set; }

        [MaxLength(50)]
        [JsonPropertyName("client_order_code")]
        public string? ClientOrderCode { get; set; }

        [Range(0, 10000000)]
        [JsonPropertyName("cod_amount")]
        public int CodAmount { get; set; } = 0;

        [MaxLength(2000)]
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [Required]
        [Range(0, 30000)]
        [JsonPropertyName("weight")]
        public int Weight { get; set; }

        [Required]
        [Range(0, 150)]
        [JsonPropertyName("length")]
        public int Length { get; set; }

        [Required]
        [Range(0, 150)]
        [JsonPropertyName("width")]
        public int Width { get; set; }

        [Required]
        [Range(0, 150)]
        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("pick_station_id")]
        public int? PickStationId { get; set; }

        [Range(0, 5000000)]
        [JsonPropertyName("insurance_value")]
        public int InsuranceValue { get; set; } = 0;

        [JsonPropertyName("coupon")]
        public string? Coupon { get; set; }

        [JsonPropertyName("service_type_id")]
        public int? ServiceTypeId { get; set; }

        [JsonPropertyName("service_id")]
        public int? ServiceId { get; set; }

        [Required]
        [JsonPropertyName("payment_type_id")]
        public int PaymentTypeId { get; set; }

        [MaxLength(5000)]
        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [Required]
        [MaxLength(500)]
        [JsonPropertyName("required_note")]
        public string? RequiredNote { get; set; }

        [JsonPropertyName("pick_shift")]
        public List<int>? PickShift { get; set; }

        [Required]
        [JsonPropertyName("items")]
        public List<GhnItem> Items { get; set; }

        [JsonPropertyName("cod_failed_amount")]
        public int? CodFailedAmount { get; set; }
    }

    public class GhnItem
    {
        [Required]
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [Required]
        [JsonPropertyName("quantity")]
        public int? Quantity { get; set; }

        [JsonPropertyName("price")]
        public double? Price { get; set; }

        [JsonPropertyName("length")]
        public int? Length { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("weight")]
        public int? Weight { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("category")]
        public GhnCategory? Category { get; set; }
    }

    public class GhnCategory
    {
        [JsonPropertyName("level1")]
        public string? Level1 { get; set; }

        [JsonPropertyName("level2")]
        public string? Level2 { get; set; }

        [JsonPropertyName("level3")]
        public string? Level3 { get; set; }
    }


    // 1. Lớp bọc ngoài cùng (Root Response)
    public class GhnCreateOrderResponse <T>
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("message_display")]
        public string? MessageDisplay { get; set; }
    }

    // 2. Lớp chứa dữ liệu chính của đơn hàng
    public class GhnOrderData
    {
        [JsonPropertyName("order_code")]
        public string OrderCode { get; set; }

        [JsonPropertyName("sort_code")]
        public string? SortCode { get; set; }

        [JsonPropertyName("trans_type")]
        public string? TransType { get; set; }

        [JsonPropertyName("ward_encode")]
        public string? WardEncode { get; set; }

        [JsonPropertyName("district_encode")]
        public string? DistrictEncode { get; set; }

        [JsonPropertyName("fee")]
        public GhnFee? Fee { get; set; }

        // Lưu ý: Trong JSON bạn gửi, total_fee đang là chuỗi ("33000") chứ không phải số
        [JsonPropertyName("total_fee")]
        public string? TotalFee { get; set; } 

        [JsonPropertyName("expected_delivery_time")]
        public DateTime ExpectedDeliveryTime { get; set; }
    }

    // 3. Lớp chứa chi tiết các loại phí
    public class GhnFee
    {
        [JsonPropertyName("main_service")]
        public int MainService { get; set; }

        [JsonPropertyName("insurance")]
        public int Insurance { get; set; }

        [JsonPropertyName("station_do")]
        public int StationDo { get; set; }

        [JsonPropertyName("station_pu")]
        public int StationPu { get; set; }

        [JsonPropertyName("return")]
        public int Return { get; set; }

        [JsonPropertyName("r2s")]
        public int R2s { get; set; }

        [JsonPropertyName("coupon")]
        public int Coupon { get; set; }

        [JsonPropertyName("cod_failed_fee")]
        public int CodFailedFee { get; set; }
    }
}
