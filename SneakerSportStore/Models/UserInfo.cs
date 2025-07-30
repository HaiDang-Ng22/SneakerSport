using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SneakerSportStore.Models
{
    public class UserInfo
    {
        public string UserId { get; set; }
        public string HoTen { get; set; }
        public string Email { get; set; }
        //public string MatKhau { get; set; }
        public string SoDienThoai { get; set; }
        public string DiaChi { get; set; }
        public string AvatarUrl { get; set; }
        public string TenDangNhap { get; set; }
        public DateTime NgayTao { get; set; }
        public string UserRole { get; set; }

    }
    public class UserRole
    {
        public const string Admin = "Admin";
        public const string User = "User";
        public const string Staff = "nhanvien";
        public const string Shipper = "giaohang";
        public const string Support = "chamsockhachhang";
        public const string Warehouse = "kho";
        public const string Accountant = "ketoan";
    }
}