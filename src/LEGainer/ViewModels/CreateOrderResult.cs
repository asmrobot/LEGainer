using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LEGainer.ViewModels
{
    public class CreateOrderResult:IResultBase
    {
        public string SessionKey { get; set; }


        public string ChallengeDomain { get; set; }


        public string DnsTxtValue { get; set; }


    }
}
