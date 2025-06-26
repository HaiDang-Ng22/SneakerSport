using SneakerSportStore.Models;
using SneakerSportStore.Models;
using System.Linq;
using System.Web.Mvc;

namespace SneakerSprotStore.Controllers
{
    public class OrderController : Controller
    {
        private Sneaker1Entities db = new Sneaker1Entities();
    
        public ActionResult OrderDetails(int id)
        {
            // Lấy đơn hàng theo ID
            var order = db.DonDatHangs
                          .Include("ChiTietDonDatHangs")
                          .FirstOrDefault(o => o.DonDatHangID == id);

            if (order == null)
            {
                return HttpNotFound(); // Trả về lỗi 404 nếu không tìm thấy đơn hàng
            }

            return View(order); // Truyền đơn hàng đến view
        }

    }
}
