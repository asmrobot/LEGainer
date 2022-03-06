using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LEGainer.ViewModels
{
    public class IResultBase
    {
        /// <summary>
        /// 是否成功,0:失败，1：成功
        /// </summary>
        public Int32 Success { get; set; }

        public string Message { get; set; }
    }
}
