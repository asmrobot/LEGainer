using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LEGainer.Models
{
    public class LEGainerOptions
    {
        public string Email { get; set; }


        /// <summary>
        /// 每分钟创建订单数限制
        /// </summary>
        public Int32 CreateOrderCountPerMin { get; set; }


        
    }
}
