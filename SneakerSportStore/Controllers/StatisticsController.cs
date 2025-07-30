using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using SneakerSportStore.Models;

namespace SneakerSportStore.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app/";

        public async Task<ActionResult> RevenueStatistics()
        {
            var model = new RevenueStatisticsViewModel();

            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(FirebaseDbUrl + "/orders.json");
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    var ordersDict = JsonConvert.DeserializeObject<Dictionary<string, Order>>(json);

                    if (ordersDict == null)
                    {
                        TempData["InfoMessage"] = "Không có dữ liệu đơn hàng";
                        return View(model);
                    }

                    var completedOrders = ordersDict.Values
                        .Where(o => o.Status == "Hoàn thành")
                        .ToList();
                    var cancelledOrder = ordersDict.Values
                        .Where(o => o.Status == "Hủy")
                        .ToList();
                    model.TotalRevenue = completedOrders.Sum(o => o.FinalTotal); 
                    model.TotalCompletedOrders = completedOrders.Count;
                    model.TotalcancelledOrder = cancelledOrder.Count;
                    model.AverageOrderValue = completedOrders.Any()
                        ? completedOrders.Average(o => o.FinalTotal)
                        : 0;

                    model.MonthlyRevenue = completedOrders
                        .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                        .Select(g => new MonthlyRevenue
                        {
                            Year = g.Key.Year,
                            Month = g.Key.Month,
                            MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                            Revenue = g.Sum(o => o.Total), 
                            OrderCount = g.Count()
                        })
                        .OrderByDescending(m => m.Year)
                        .ThenByDescending(m => m.Month)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi tải dữ liệu: {ex.Message}";
            }

            return View(model);
        }
    }

    public class RevenueStatisticsViewModel
    {
        public double TotalRevenue { get; set; }
        public int TotalCompletedOrders { get; set; }
        public int TotalcancelledOrder { get; set; }
        public double AverageOrderValue { get; set; }
        public List<MonthlyRevenue> MonthlyRevenue { get; set; } = new List<MonthlyRevenue>();
    }

    public class MonthlyRevenue
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; }
        public double Revenue { get; set; }
        public int OrderCount { get; set; }
    }
}