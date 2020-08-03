using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TencentPay.Models
{
    public class DownloadBillModel
    {
        public string NonceStr { get; set; }
        public string SignType { get; set; }
        public string DeviceInfo { get; set; }
        [Required]
        public string BillDate { get; set; }
        public string BillType { get; set; }

    }
}
