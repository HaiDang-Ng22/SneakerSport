using System;

namespace SneakerSportStore.Models
{
    public class Notification
    {
        public string NotificationId { get; set; }
        public string UserId { get; set; }      // Ai nhận thông báo
        public string Message { get; set; }
        public string ProductId { get; set; }   // (Nếu liên quan sản phẩm)
        public string CommentId { get; set; }   // (Nếu liên quan comment)
        public string OrderId { get; set; }     // (Nếu là thông báo đặt hàng)
        public bool IsRead { get; set; }        // Đã đọc hay chưa
        public DateTime CreatedAt { get; set; }
    }
}
