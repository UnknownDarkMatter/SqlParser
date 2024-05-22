using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.SqlParser.Model
{
    public class SqlFrom
    {
        public List<SqlFromElement> SqlFromElements { get; set; } = new List<SqlFromElement>();
    }
}
