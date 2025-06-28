using System;

namespace SneakerSportStore.Models
{
    public class Comment
    {
        public string CommentId { get; set; }
        public string ProductId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Content { get; set; }
        public string ParentCommentId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
