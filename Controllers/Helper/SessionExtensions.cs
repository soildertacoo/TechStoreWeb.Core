using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Diagnostics;

namespace TechStoreWeb.Core.Helpers // Đổi namespace theo project của bạn nếu cần
{
    public static class SessionExtensions
    {
        // Hàm dùng để LƯU đối tượng vào Session
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            // Biến đối tượng thành chuỗi JSON và lưu lại
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        // Hàm dùng để LẤY đối tượng từ Session ra
        public static T? GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            
            // Nếu không có dữ liệu thì trả về null, nếu có thì dịch ngược JSON thành đối tượng
            if (value == null) return default(T);
            else
            {
                Debug.WriteLine("Đã lưu thành công");
                return JsonSerializer.Deserialize<T>(value); 
            }
        }
    }
}