using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Services
{
    public static class StringServices
    {
        public static string JoinStrings(this IEnumerable<string> input)
        {
            return input.Aggregate((s1, s2) => string.Format("'{0}', '{1}'", s1, s2));
        }
    }
}
