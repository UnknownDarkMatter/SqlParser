using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.SqlParser.Model
{
    public enum SqlSelectElementType
    {
        Column,
        Function
    }

    public enum SqlFromElementType
    {
        Table,
        Query
    }

    public enum SqlPart
    {
        UNKNOWN,
        SELECT,
        FROM,
        WHERE
    }

}
