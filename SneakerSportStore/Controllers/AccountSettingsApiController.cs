using SneakerSportStore.Models;
using System.Linq;
using System.Web.Http;

namespace SneakerSportStore.ApiControllers
{
    [RoutePrefix("api/account-settings")]
    public class AccountSettingsApiController : ApiController
    {
        private readonly Sneaker1Entities _db = new Sneaker1Entities();

        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult GetAccountInfo(int id)
        {
            var user = _db.KhachHangs.Find(id);
            if (user == null)
                return NotFound();

            var result = new
            {
                user.KhachHangID,
                user.TenDangNhap,
                user.HoTen,
                user.Email,
                user.SoDienThoai,
                user.DiaChi
            };

            return Ok(result);
        }

        // PUT: api/account-settings/{id}
        [HttpPut]
        [Route("{id:int}")]
        public IHttpActionResult UpdateAccountInfo(int id, KhachHang updated)
        {
            var user = _db.KhachHangs.Find(id);
            if (user == null)
                return NotFound();

            user.HoTen = updated.HoTen;
            user.Email = updated.Email;
            user.SoDienThoai = updated.SoDienThoai;
            user.DiaChi = updated.DiaChi;

            _db.SaveChanges();
            return Ok("Cập nhật thành công");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
