using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Specflow.Converters;
using TechTalk.SpecFlow;

namespace Specflow.Utils.Tests
{
    class TableRowConverterTests
    {
        public static VanillaOption ToVanillaOption(TableRow tableRow)
        {
            return TableRowConverter.ToObject<VanillaOption>(tableRow);
        }

        public static VanillaOption ToVanillaOption2(TableRow tableRow)
        {
            return tableRow.CreateInstance<VanillaOption>();
        }

        public static VanillaOption ToVanillaOption3(TableRow tableRow)
        {
            return tableRow.CreateInstance<VanillaOption>(MappingBehaviour.Loose);
        }
    }
}
