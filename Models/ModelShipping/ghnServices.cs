using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TechStore.Models.ModelShipping
{
    public class ghnOrder
    {
        [Required]
        [JsonPropertyName("token")]
        public string? Token;

        [Required]
        [JsonPropertyName("shop_id")]
        public int? ShopID;

        [Required]
        [MaxLength(1024)]
        [JsonPropertyName("from_name")]
        public string? FromName {get;set;}

        [Required]
        [JsonPropertyName("from_phone")]
        public string?  FromPhone { get; set; }

        [Required]
        [MaxLength(1024)]
        [JsonPropertyName("from_address")]
        public string? FromAddress {get;set;}
        [Required]
        [JsonPropertyName("from_ward_name")]
        public string? FromWardName {get;set;}
        [Required]
        [JsonPropertyName("from_district_name")]
        public string? FromDistrictName {get;set;}
        [Required]
        [JsonPropertyName("from_province_name")]
        public string? FromProvinceName {get;set;}
      
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
        [JsonPropertyName("to_ward_name")]
        public string? ToWardName { get; set; }

        [Required]
        [JsonPropertyName("to_ward_code")]
        public string? ToWardCode { get; set; }

        [Required]
        [JsonPropertyName("to_district_name")]
        public string? ToDistrictName { get; set; }
        [Required]
        [JsonPropertyName("to_province_name")]
        public string? ToProvinceName { get; set; }

        [Required]
        [JsonPropertyName("is_new_to_address")]
        public bool? IsNewToAddress { get; set; }
        [Required]
        [JsonPropertyName("is_new_from_address")]
        public bool? IsNewFromAddress { get; set; }

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
        public string? OrderCode { get; set; }

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
        public int? TotalFee { get; set; } 

        [JsonPropertyName("expected_delivery_time")]
        public DateTime ExpectedDeliveryTime { get; set; }
    }

    public class GHNProvinceWard
    {
        [JsonPropertyName("_id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("extension_names")]
        public List<string> ExtensionNames { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("parent_id")]
        public int ParentId { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("updated_ip")]
        public string? UpdatedIp { get; set; }

        [JsonPropertyName("updated_employee")]
        public int UpdatedEmployee { get; set; }

        [JsonPropertyName("updated_source")]
        public string? UpdatedSource { get; set; }

        [JsonPropertyName("updated_date")]
        public DateTime UpdatedDate { get; set; }

        [JsonPropertyName("created_ip")]
        public string? CreatedIp { get; set; }

        [JsonPropertyName("created_employee")]
        public int CreatedEmployee { get; set; }

        [JsonPropertyName("created_source")]
        public string? CreatedSource { get; set; }

        [JsonPropertyName("created_date")]
        public DateTime CreatedDate { get; set; }
    }

    public class Root
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public List<GHNProvinceWard> Data { get; set; }
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
    public class dataRawGHN()
    {
        [JsonPropertyName("province_id")]
        public int ProvinceID {get;set;}

        [JsonPropertyName("offset")]
        public int Offset {get;set;} = 0;
        [JsonPropertyName("limit")]
        public int Limit {get;set;} = 200;

    }
}
