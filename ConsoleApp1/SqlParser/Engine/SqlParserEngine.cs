using ConsoleApp1.SqlParser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.SqlParser.Engine
{
    public class SqlParserEngine
    {
        private List<char> _spaceChars = new List<char>() { ' ', '\t', '\r', '\n' };
        private List<char> _wordSeparators = new List<char>() { ' ', '\t', '\r', '\n', '(', ',' };
        public SqlQuery Parse(string sql)
        {
            int index = 0;
            var sqlQuery = new SqlQuery();
            var sqlPart = SqlPart.UNKNOWN;

            while (TryGetNextWord(sql, ref index, out string word, out bool isParenthesis, out bool isComma))
            {
                if(word.ToLower() == "select")
                {
                    sqlPart = SqlPart.SELECT;
                    if(!TryGetNextWord(sql, ref index, out word, out isParenthesis, out isComma))
                    {
                        sqlPart = SqlPart.UNKNOWN;
                    }
                }
                else if(sqlPart == SqlPart.SELECT && word.ToLower() == "from")
                {
                    sqlPart = SqlPart.FROM;
                    if (!TryGetNextWord(sql, ref index, out word, out isParenthesis, out isComma))
                    {
                        sqlPart = SqlPart.UNKNOWN;
                    }
                }
                else if (sqlPart == SqlPart.FROM && word.ToLower() == "where")
                {
                    sqlPart = SqlPart.WHERE;
                    string rawSql = sql.Substring(index);
                    sqlQuery.SqlWhere = new SqlWhere()
                    {
                        RawSql = rawSql
                    };
                    index = sql.Length;
                }
                switch (sqlPart)
                {
                    case SqlPart.SELECT:
                        {
                            if (isParenthesis)
                            {
                                continue;
                            }
                            else if (!isComma)
                            {
                                sqlQuery.SqlSelect.SqlSelectElements.Add(
                                    new SqlSelectElement()
                                    {
                                        SqlSelectElementType = SqlSelectElementType.Column,
                                        SqlColumn = new SqlColumn()
                                        {
                                            Name = word,
                                        }
                                    });
                            }
                            else if (isComma)
                            {
                                index++;
                            }
                            break;
                        }
                    case SqlPart.FROM:
                        {
                            if (isComma)
                            {
                                index++;
                            }
                            else if (!isParenthesis)
                            {
                                sqlQuery.SqlFrom.SqlFromElements.Add(
                                    new SqlFromElement()
                                    {
                                        SqlFromElementType = SqlFromElementType.Table,
                                        SqlTable = new SqlTable()
                                        {
                                            Name = word,
                                        }
                                    });
                            }
                            else
                            {
                                index++;
                                GetNextNonSpace(sql, ref index);
                                string parenthesis = GetParenthesis(sql, ref index, 1);
                                var subQuery = Parse(parenthesis);
                                GetNextNonSpace(sql, ref index);
                                index++;//move after ')'
                                if (TryGetNextWord(sql, ref index, out word, out isParenthesis, out isComma))
                                {
                                    sqlQuery.SqlFrom.SqlFromElements.Add(
                                        new SqlFromElement()
                                        {
                                            SqlFromElementType = SqlFromElementType.Query,
                                            SqlQuery = subQuery
                                        });
                                }
                                else
                                {
                                    return sqlQuery;
                                }
                            }
                            break;
                        }
                    default:
                        {
                            return sqlQuery;
                        }
                }
            }
            return sqlQuery;
        }

        private bool TryGetNextWord(string sql, ref int index, out string word, out bool isParenthesis, out bool isComma)
        {
            var res = new StringBuilder();
            int i = index;
            isParenthesis = false;
            isComma = false;
            if (i >= sql.Length)
            {
                word = null;
                return false;
            }
            char c = sql[i];
            while (i < sql.Length)
            {
                c = sql[i];
                if (!_spaceChars.Contains(c))
                {
                    break;
                }
                i++;
            }
            isParenthesis = c == '(';
            isComma = c == ',';
            while (i < sql.Length)
            {
                c = sql[i];
                if (_wordSeparators.Contains(c))
                {
                    break;
                }
                isParenthesis = c == '(';
                isComma = c == ',';
                res.Append(c);
                i++;
            }
            index = i;
            word = res.ToString();
            return true;
        }
        
        private string GetParenthesis(string sql, ref int index, int parenthesisCount)
        {
            int i = index;
            if (i >= sql.Length )
            {
                return "";
            }
            char c = sql[i];
            while (i < sql.Length)
            {
                c = sql[i];
                if (c == '(')
                {
                    parenthesisCount++;
                }
                else if(c == ')')
                {
                    parenthesisCount--;
                }
                if(parenthesisCount == 0)
                {
                    break;
                }
                i++;
            }
            string res = sql.Substring(index, i - index);
            index = i;
            return res;
        }

        private void GetNextNonSpace(string sql, ref int index)
        {
            int i = index;
            if (i >= sql.Length)
            {
                return;
            }
            char c = sql[i];
            while (i < sql.Length)
            {
                c = sql[i];
                if (!_spaceChars.Contains(c))
                {
                    break;
                }
                i++;
            }
            index = i;
        }

        public string GetTestSql1()
        {

            return @"
select *
from (select * from public.personne_src) as a
inner join public.personne c on a.nom = coalesce(c.nom, '')
, public.personne as b  
where a.nom = b.nom

";

            return @" select toto, age from 
                        (select titi as toto, age from 
                            (select matable.nom as titi, age from personne_src matable)
                        ) a, personne_dest dest
where a.age > 50 and a.toto = dest.nom;
   
";
        }
    }
}
