using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using SneakerSportStore.Models;
namespace SneakerSportStore.Models
{
    //public class DiscountCode
    //{
    //    public string IdVoucher { get; set; } 
    //    public string CodeName { get; set; }
    //    public string Condition { get; set; }
    //    public DiscountType DiscountType { get; set; }
    //    public decimal DiscountValue { get; set; } 
    //    public DateTime StartDate { get; set; } 
    //    public DateTime EndDate { get; set; }
    //    public bool Active { get; set; }
    //    public bool IsPublic { get; set; } 
    //    public decimal MinimumOrderValue { get; set; } 
    //    public int UsageLimit { get; set; } 
    //    public bool IsUsed { get; set; } 
    //    public bool IsValid(DateTime currentDate)
    //    {
    //        return Active && currentDate >= StartDate && currentDate <= EndDate;
    //    }
    //}

    // Enum cho loại giảm giá
    //public enum DiscountType
    //{
    //    Percentage, // Giảm theo phần trăm
    //    FixedAmount // Giảm theo số tiền cố định
    //}


    public enum DiscountType
    {
        Percentage,
        FixedAmount 
    }
    public class DiscountCode
    {
        public bool IsValid(DateTime currentDate)
        {
            return Active && currentDate >= StartDate && currentDate <= EndDate;
        }
        public string Description
        {
            get; set;
        }
        public string IdVoucher { get; set; }

        [Required(ErrorMessage = "Tên mã là bắt buộc")]
        public string CodeName { get; set; }

        [Required(ErrorMessage = "Điều kiện là bắt buộc")]
        public string Condition { get; set; }

        [Required(ErrorMessage = "Loại giảm giá là bắt buộc")]
        public DiscountType DiscountType { get; set; }

        [Required(ErrorMessage = "Giá trị giảm giá là bắt buộc")]
        [Range(0.01, 100, ErrorMessage = "Giá trị giảm giá phải từ 0.01 đến 100")]
        public double DiscountValue { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        public DateTime EndDate { get; set; }

        public bool Active { get; set; }

        public bool IsPublic { get; set; }

        [Required(ErrorMessage = "Giá trị đơn hàng tối thiểu là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá trị không hợp lệ")]
        public double MinimumOrderValue { get; set; }
    }
    public static class EnumExtensions
    {
        public static SelectList GetEnumSelectList(this HtmlHelper htmlHelper, Type enumType)
        {
            var values = Enum.GetValues(enumType).Cast<Enum>();
            var items = values.Select(e => new SelectListItem
            {
                Text = e.ToString(),
                Value = e.ToString()
            }).ToList();

            return new SelectList(items, "Value", "Text");
        }
    }
}
