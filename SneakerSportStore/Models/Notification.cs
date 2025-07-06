using System;

namespace SneakerSportStore.Models
{
    public class Notification
    {
        public string NotificationId { get; set; }
        public string UserId { get; set; }     
        public string Message { get; set; }
        public string ProductId { get; set; } 
        public string CommentId { get; set; }   
        public string OrderId { get; set; }     
        public bool IsRead { get; set; }       

        public DateTime CreatedAt { get; set; }
        public string Type { get; set; }
        public string RelatedOrderId { get; set; } 
        public string RedirectUrl { get; set; } 
    }
}
