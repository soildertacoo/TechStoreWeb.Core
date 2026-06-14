using System.Text.Json;

namespace TechStore.Models
{
    public class TurnstileCaptcha
    {
        private readonly static HttpClient _captchaClient = new HttpClient();
        // public TurnstileCaptcha()
        // {
            
        // }
        public async static Task<(bool, string)> IsValid(string token)
        {
            try
            {
                var formData = new Dictionary<string, string>
                {
                    { "secret", "0x4AAAAAADkFbn0bqqZ5zTU1dhySX2sUy2s" },
                    { "response", token }
                };

                var content = new FormUrlEncodedContent(formData);
                var postTask = await _captchaClient.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", content);

                var result = await postTask.Content.ReadAsStringAsync();
                var resultObject = JsonSerializer.Deserialize<JsonElement>(result);
                return (resultObject.GetProperty("success").GetBoolean(),"");
            }
            catch (Exception ex)
            {
                return (false, $"4033: Có lỗ i khi gửi lên server xác thực token {ex.Message}");
            }
        }
    }
}