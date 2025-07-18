using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SneakerSportStore.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private const string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app/";

        private async Task<HttpResponseMessage> SendPatchAsync(string url, string jsonContent)
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };
                return await client.SendAsync(request);
            }
        }



        [Authorize(Roles = "Admin")]
        public ActionResult AdminChat()
        {
            // Sử dụng User.Identity.Name thay Session
            ViewBag.CurrentUserId = User.Identity.Name;
            return View();
        }

        public ActionResult UserChat()
        {
            var userId = User.Identity.Name;
            ViewBag.UserId = userId;
            ViewBag.ConversationId = $"user_{userId}_admin"; // Chuẩn hóa cấu trúc
            return View();
        }
        [HttpGet]
        public async Task<JsonResult> GetConversations()
        {
            try
            {
                // Kiểm tra quyền
                if (Session["UserRole"]?.ToString() != "Admin")
                {
                    Response.StatusCode = 403;
                    return Json(new { error = "Forbidden" }, JsonRequestBehavior.AllowGet);
                }

                var response = await new HttpClient().GetAsync($"{FirebaseDbUrl}conversations.json");
                if (!response.IsSuccessStatusCode)
                {
                    Response.StatusCode = (int)response.StatusCode;
                    return Json(new { error = "Firebase error" }, JsonRequestBehavior.AllowGet);
                }

                var json = await response.Content.ReadAsStringAsync();
                var conversations = JsonConvert.DeserializeObject<Dictionary<string, Conversation>>(json) ??
                                  new Dictionary<string, Conversation>();

                // Lọc chỉ lấy hội thoại có admin
                var adminConversations = conversations
                    .Where(c => c.Value.Participants?.ContainsKey("Admin") == true)
                    .ToDictionary(c => c.Key, c => c.Value);

                return Json(adminConversations, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetMessages(string conversationId)
        {
            try
            {
                var userId = User.Identity.Name;
                if (string.IsNullOrEmpty(userId))
                {
                    Response.StatusCode = 401;
                    return Json(new { error = "Unauthorized" }, JsonRequestBehavior.AllowGet);
                }

                // Kiểm tra người dùng có trong hội thoại không
                var response = await new HttpClient().GetAsync($"{FirebaseDbUrl}conversations/{conversationId}.json");
                var participants = await response.Content.ReadAsAsync<Dictionary<string, bool>>();
                if (!response.IsSuccessStatusCode)
                {
                    // Tạo hội thoại mới nếu không tồn tại
                     participants = new Dictionary<string, bool>
                    {
                        [userId] = true,
                        [userId.Contains("admin") ? userId : "admin"] = true
                    };

                    var participantsJson = JsonConvert.SerializeObject(participants);
                    await SendPatchAsync(
                        $"{FirebaseDbUrl}conversations/{conversationId}/participants.json",
                        participantsJson
                    );

                    return Json(new List<Message>(), JsonRequestBehavior.AllowGet);
                }

                if (!participants.ContainsKey(userId) && !participants.ContainsKey("Admin"))
                {
                    Response.StatusCode = 403;
                    return Json(new { error = "Access denied" }, JsonRequestBehavior.AllowGet);
                }

                // Lấy tin nhắn
                var msgResponse = await new HttpClient().GetAsync(
     $"{FirebaseDbUrl}conversations/{conversationId}/messages.json?orderBy=\"timestamp\"");

                if (!msgResponse.IsSuccessStatusCode)
                {
                    Response.StatusCode = (int)msgResponse.StatusCode;
                    return Json(new { error = "Messages not found" }, JsonRequestBehavior.AllowGet);
                }

                var json = await msgResponse.Content.ReadAsStringAsync();
                var messages = JsonConvert.DeserializeObject<Dictionary<string, Message>>(json) ??
                             new Dictionary<string, Message>();

                return Json(messages.Values.OrderBy(m => m.Timestamp), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public async Task<JsonResult> SendMessage(string conversationId, string content)
        {
            try
            {
                var userId = User.Identity.Name;
                if (string.IsNullOrEmpty(userId))
                {
                    Response.StatusCode = 401;
                    return Json(new { error = "Unauthorized" });
                }

                // Sử dụng User.IsInRole để kiểm tra quyền
                var isAdmin = User.IsInRole("Admin");
                var receiverId = isAdmin ?
        conversationId.Replace("user_", "").Replace("_admin", "") :
        "admin";

                // Tạo tin nhắn
                var message = new Message
                {
                    SenderId = userId,
                    Content = content,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Read = false
                };

                // Gửi tin nhắn
                using (var client = new HttpClient())
                {
                    // Gửi tin nhắn mới
                    var jsonContent = new StringContent(JsonConvert.SerializeObject(message),
                        Encoding.UTF8, "application/json");

                    var postResponse = await client.PostAsync(
                        $"{FirebaseDbUrl}conversations/{conversationId}/messages.json",
                        jsonContent);

                    if (!postResponse.IsSuccessStatusCode)
                    {
                        Response.StatusCode = (int)postResponse.StatusCode;
                        return Json(new { error = "Send failed" });
                    }

                    // Đảm bảo participants - SỬA CHỖ NÀY
                    var participants = new Dictionary<string, bool>
                    {
                        [userId] = true,
                        [receiverId] = true
                    };

                    var participantsJson = JsonConvert.SerializeObject(participants);
                    await SendPatchAsync(
                        $"{FirebaseDbUrl}conversations/{conversationId}/participants.json",
                        participantsJson
                    );
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { error = ex.Message });
            }
        }

        public class Conversation
        {
            public Dictionary<string, bool> Participants { get; set; }
            public Dictionary<string, Message> Messages { get; set; }
        }

        public class Message
        {
            public string SenderId { get; set; }
            public string Content { get; set; }
            public long Timestamp { get; set; }
            public bool Read { get; set; }
        }
    }
}