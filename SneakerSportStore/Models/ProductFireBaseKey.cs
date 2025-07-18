using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SneakerSportStore.Models
{
    public class ProductFireBaseKey
    {
        public string Mau { get; set; }
        public List<string> KichThuoc { get; set; } = new List<string>();
        public string ChatLieu { get; set; }
        public double TrongLuong { get; set; }
        public string KichThuocBaoBi { get; set; }
        public bool SanPhamNoiBat { get; set; }
        public DateTime NgaySanXuat { get; set; }
        public string GiayId { get; set; }

        [Required(ErrorMessage = "Tên giày không được để trống")]
        public string TenGiay { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public decimal Gia { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn không hợp lệ")]
        public int SoLuongTon { get; set; }
        public string MoTa { get; set; }
        public string NhaSanXuatID { get; set; }
        public string LoaiGiayID { get; set; }
        //public string TenNhaSanXuat { get; set; }
        //public string TenLoaiGiay { get; set; }
        public string HinhAnh { get; set; }
        public List<string> AdditionalImages { get; set; } = new List<string>();

        public LoaiGiayInfo LoaiGiay { get; set; }
        public NhaSanXuatInfo NhaSanXuat { get; set; }

        public bool IsValid(out string errorMessage)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(TenGiay))
                errors.Add("Tên giày không được để trống");

            if (Gia <= 0)
                errors.Add("Giá phải lớn hơn 0");

            if (SoLuongTon < 0)
                errors.Add("Số lượng tồn không hợp lệ");

            if (string.IsNullOrWhiteSpace(NhaSanXuatID))
                errors.Add("Chưa chọn nhà sản xuất");

            if (string.IsNullOrWhiteSpace(LoaiGiayID))
                errors.Add("Chưa chọn loại giày");

            errorMessage = errors.Any() ? string.Join("; ", errors) : null;
            return !errors.Any();
        }

        public string GetSizeString() => KichThuoc != null ? string.Join(", ", KichThuoc) : "";

        public string GetAdditionalImagesString() => AdditionalImages != null ? string.Join("\n", AdditionalImages) : "";

        public void UpdateSizesFromString(string sizes)
        {
            if (!string.IsNullOrEmpty(sizes))
            {
                KichThuoc = sizes.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }
            else
            {
                KichThuoc = new List<string>();
            }
        }

        public void UpdateAdditionalImagesFromString(string images)
        {
            if (!string.IsNullOrEmpty(images))
            {
                AdditionalImages = images.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }
            else
            {
                AdditionalImages = new List<string>();
            }
        }
    }

    public class LoaiGiayInfo
    {
        public string LoaiGiayID { get; set; }
        public string TenLoaiGiay { get; set; }
    }

    public class NhaSanXuatInfo
    {
        public string NhaSanXuatID { get; set; }
        public string TenNhaSanXuat { get; set; }
    }

    // Đã xóa các lớp trùng lặp và không sử dụng
}