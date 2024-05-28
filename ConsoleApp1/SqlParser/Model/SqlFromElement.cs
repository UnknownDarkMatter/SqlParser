using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.SqlParser.Model
{
    public class SqlFromElement
    {
        public SqlFromElementType SqlFromElementType { get; set; }

        public SqlTable SqlTable { get; set; }

        public SqlQuery SqlQuery { get; set; }

        public SqlJoinType SqlJoinType { get; set; }

        public string JoinOnRawSql { get; set; }
        public string Alias { get; set; }

    }
}
