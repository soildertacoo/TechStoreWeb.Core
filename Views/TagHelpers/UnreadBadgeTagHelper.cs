using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TechStoreWeb.Core.TagHelpers
{
    // Bắt các thẻ HTML có tên là <unread-badge>
    [HtmlTargetElement("unread-badge")]
    public class UnreadBadgeTagHelper : TagHelper
    {
        // Thuộc tính: Số lượng tin nhắn chưa đọc
        public int Count { get; set; }

        // Thuộc tính: "fab" (nút tổng) hoặc "item" (danh sách user)
        public string Type { get; set; } = "item";

        // Thuộc tính: Dành cho thẻ nào cần ID để JS gọi (VD: main-unread-badge)
        public string? ElementId { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // Chuyển thẻ custom <unread-badge> thành thẻ <div> chuẩn
            output.TagName = "div";

            // Nếu gán ID thì thêm vào DOM
            if (!string.IsNullOrEmpty(ElementId))
            {
                output.Attributes.SetAttribute("id", ElementId);
            }

            if (Type.ToLower() == "fab")
            {
                output.Attributes.Add("class", "unread-badge-fab");

                // Trạng thái nút tổng ban đầu (ẩn nếu Count = 0)
                if (Count <= 0)
                {
                    output.Attributes.Add("style", "display: none;");
                }
            }
            else // Type = "item"
            {
                output.Attributes.Add("class", "unread-badge-item");

                // Ở danh sách user, nếu không có tin nhắn mới thì không render ra thẻ này luôn cho nhẹ DOM
                if (Count <= 0)
                {
                    output.SuppressOutput();
                    return;
                }
            }

            // Ghi số lượng tin nhắn vào trong thẻ div
            output.Content.SetContent(Count.ToString());
        }
    }
}