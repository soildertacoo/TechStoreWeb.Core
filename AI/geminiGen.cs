namespace TechStoreWeb.Core.AI
{
    using System.Threading.Tasks;
    using Google.GenAI;
    using Google.GenAI.Types;

    public class geminiGen {
        public static async Task<string> genText(string contents) {
            // The client gets the API key from the environment variable `GEMINI_API_KEY`.
            try
            {
                string? myApiKey = "AIzaSyBlHHf0exyv5MLNf_1GWW2khp1kJy7B4_A"; 

                if (string.IsNullOrEmpty(myApiKey))
                {
                    Console.WriteLine("LỖI BẢO MẬT: Chưa cấu hình GEMINI_API_KEY");
                    return "Hệ thống AI chưa được cấu hình khóa bảo mật.";
                }
                var client = new Client(apiKey: myApiKey);
                var response = await client.Models.GenerateContentAsync(
                model: "gemini-2.5-flash-lite", contents: contents
                );
                return response.Text ?? "Không trả lời được";
            }
            catch(Exception ex)
            {
                Console.WriteLine($"GEMINI_ERROR: {ex}");
                return "Gemini bị lỗi gì đó ko xử lý được";
            }
        }
    }
}
