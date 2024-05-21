using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.SqlParser.Model
{
    public class SqlSelect
    {
        public List<SqlSelectElement> SqlSelectElements { get; set; } = new List<SqlSelectElement>();
        public string RawSql { get; set; }
    }
}
