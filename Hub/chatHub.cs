using TechStore.Models;
namespace TechStoreWeb.Core.Hub
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.EntityFrameworkCore;
    using TechStoreWeb.Core.AI;
    using TechStore.Models;

    public class chatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public chatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Room36");
            await base.OnConnectedAsync(); 
        }

        public async Task JoinRoom(string room)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, room);
            string userName = Context.User?.Identity?.Name ?? "Khách";
            await Clients.Group(room).SendAsync("EnteredRoom", $"{userName} đã tham gia cuộc trò chuyện.");

            if (room != "Room_AI_Support")
            {
                var history = await _context.ChatMessage
                    .Where(m => m.RoomId == room)
                    .OrderBy(m => m.SentAt)
                    .Take(50)
                    .Select(m => new {
                        content = m.Content,
                        isFromSupport = m.IsFromSupport,
                        sentAt = m.SentAt.ToString("HH:mm")
                    }).ToListAsync();

                await Clients.Caller.SendAsync("LoadHistory", history);
            }
        }

        public async Task SendMessage(string sender, string room, string msg)
{
    // LƯU TIN NHẮN VÀO SQL SERVER
    if (room != "Room_AI_Support")
    {
        bool isSupport = sender == "admin" || sender == "Chăm sóc khách hàng";
        
        // 1. Khai báo biến lưu ID
        int? idCus = null;
        int? idAdmin = null;

        // 2. Tìm ID tương ứng trong Database
        if (isSupport)
        {
            var AdminUsers = await _context.AdminUsers.FirstOrDefaultAsync(a => a.NameUser == sender);
            if (AdminUsers != null) idAdmin = AdminUsers.ID;
        }
        else
        {
            var cusUser = await _context.Customers.FirstOrDefaultAsync(c => c.NameCus == sender);
            if (cusUser != null) idCus = cusUser.IDCus;
        }

        // 3. Lưu kèm ID vào bảng ChatMessage
        var newMsg = new ChatMessage
        {
            RoomId = room, 
            Content = msg,
            IsFromSupport = isSupport,
            SentAt = DateTime.Now,
            IDCus = idCus,       // <--- Đã có ID Khách
            AdminID = idAdmin    // <--- Đã có ID Admin
        };

        _context.ChatMessage.Add(newMsg);
        await _context.SaveChangesAsync();
    }

    await Clients.Group(room).SendAsync("ReceiveMessage", sender, room, msg);

    // LOGIC AI
    if (room == "Room_AI_Support")
    {
        await Clients.Group(room).SendAsync("NotifyTyping", "TechStore AI", true);
        string aiResponse = await geminiGen.genText(msg);
        await Clients.Group(room).SendAsync("NotifyTyping", "TechStore AI", false);
        await Clients.Group(room).SendAsync("ReceiveMessage", "TechStore AI", room, aiResponse);
    }
}
        public async Task sendTyping(string sender, bool isTyping)
        {
            await Clients.Others.SendAsync("NotifyTyping", sender, isTyping); 
        }
    }
}