using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SneakerSportStore.Models
{
    public class Notification
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("isRead")]
        public bool IsRead { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } // "order", "promotion", "system"

        //[JsonProperty("relatedId")]
        //public string RelatedId { get; set; } // ID đơn hàng hoặc đối tượng liên quan

        //[JsonProperty("icon")]
        //public string Icon { get; set; } // "bell", "shopping-cart", "truck"
        public string NotificationId { get; set; }

        public string UserId { get; set; }     
        //public string Message { get; set; }
        public string ProductId { get; set; } 
        public string CommentId { get; set; }   
        //public string OrderId { get; set; }     
        //public bool IsRead { get; set; }       

        //public DateTime CreatedAt { get; set; }
        //public string Type { get; set; }
        public string RelatedOrderId { get; set; } 
        public string RedirectUrl { get; set; }



        //public DateTime? ReturnRequestDate { get; set; }
        //public string ReturnReason { get; set; }
        //public List<string> ReturnImages { get; set; } = new List<string>();
        //public string ReturnStatus { get; set; } // "Pending", "Approved", "Rejected"
    }
}
