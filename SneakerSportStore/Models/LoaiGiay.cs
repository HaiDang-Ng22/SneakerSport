using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SneakerSportStore.Models
{
    public partial class LoaiGiay
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public LoaiGiay()
        {
            this.Giays = new HashSet<Giay>();
        }

        public string LoaiGiayID { get; set; }
        //public string TenLoai { get; set; }
        public string TenLoaiGiay { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Giay> Giays { get; set; }
    }
}