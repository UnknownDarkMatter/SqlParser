using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.SqlParser.Model
{
    public class SqlQuery
    {
        public SqlSelect SqlSelect { get; set; } = new SqlSelect();
        public SqlFrom SqlFrom { get; set; } = new SqlFrom();
        public SqlWhere SqlWhere { get; set; } = new SqlWhere();
    }
}
