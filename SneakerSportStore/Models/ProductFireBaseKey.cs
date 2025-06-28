using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;

namespace SneakerSportStore.Models
{
    public class ProductFireBaseKey
    {
        public string TenGiay { get; set; }
        public decimal Gia { get; set; }
        public int SoLuongTon { get; set; }
        public string MoTa { get; set; }
        public Nullable<int> NhaSanXuatID { get; set; }
        public Nullable<int> LoaiGiayID { get; set; }
        public string HinhAnh { get; set; }
    }
}