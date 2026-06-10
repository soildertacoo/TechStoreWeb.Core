namespace TechStoreWeb.Core.Hub
{
    using Microsoft.AspNetCore.SignalR;
    // 2. Nâng cấp Hub để chat riêng 1-1
    public class notifyHub : Hub
    {
        //Bước cài đặt để mở chatHub
         public override async Task OnConnectedAsync()
        {
            string userLogged = Context.User?.Identity?.Name ?? "Khách";
            await Groups.AddToGroupAsync(Context.ConnectionId, userLogged);
            await base.OnConnectedAsync(); //Tạo phòng lên server
        }
        public async Task JoinNotifyHub(string userName)
        {
            //Dành riêng cho admin
            await Groups.AddToGroupAsync(Context.ConnectionId, userName);//vai trò vừa tạo mở cổng vừa là vào cổng 
            await Clients.Group(userName).SendAsync("EnteredRoom", $"{userName} đã được kết nối tới bạn.");
        }
        // Hàm gửi tin nhắn 1-1
        public async Task sendNotify(string sender, string room,string msg,string type = "")
        {
            //Gửi thông báo
            await Clients.All.SendAsync("ReceiveNotification", sender, room,msg,type);
           
        }
    }
}