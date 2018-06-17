using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Specflow.Converters;

namespace Specflow.Utils.Tests
{
    public enum OptionType
    {
        Call,
        Put
    }
    public class OptionProduct
    {
        public string Underlying { get; set; }
        public string Currency { get; set; }
    }

    public class VanillaOption
    {
        public OptionType OptionType { get; set; }

        public double Strike { get; set; }

        [CustomInnerProperty]
        public OptionProduct OptionProduct { get; set; }

        //public double Premium { get; set; }
        //public double SettlementPrice { get; set; }
    }
}
