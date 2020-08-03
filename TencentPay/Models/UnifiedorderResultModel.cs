using Senparc.Weixin.TenPay.V3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TencentPay.Models
{
    public class UnifiedorderResultModel
    {
        // UnifiedorderResult unifiedorderResult;
        //  UnifiedorderModel UnifiedorderModel;
        public UnifiedorderResult respond { get; set; }
        public UnifiedorderModel request { get; set; }

    }
}

