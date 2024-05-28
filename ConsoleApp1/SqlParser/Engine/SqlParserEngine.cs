using ConsoleApp1.SqlParser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApp1.SqlParser.Engine
{
    public class SqlParserEngine
    {
        private List<char> _spaceChars = new List<char>() { ' ', '\t', '\r', '\n' };
        private List<char> _wordSeparators = new List<char>() { ' ', '\t', '\r', '\n', '(', ',' };
        private List<string> _wordsJoin = new List<string>() { "left", "right", "inner", "outer", "join" };
        public SqlQuery Parse(string sql)
        {
            int index = 0;
            bool continueTryNextWord = true;
            var sqlQuery = new SqlQuery();
            var sqlPart = SqlPart.UNKNOWN;
            SqlJoinType lastJoinType = SqlJoinType.NONE;
            int indexBeforeTryNextWord = index;

            while(continueTryNextWord)
            {
                indexBeforeTryNextWord = index;
                continueTryNextWord = TryGetNextWord(sql, ref index, out string word, out bool isParenthesis, out bool isComma);
                if(word is null)
                {
                    return sqlQuery;
                }
                else if (word.ToLower() == "select")
                {
                    sqlPart = SqlPart.SELECT;
                    if (!TryGetNextWord(sql, ref index, out word, out isParenthesis, out isComma))
                    {
                        sqlPart = SqlPart.UNKNOWN;
                    }
                    indexBeforeTryNextWord = index;
                }
                else if(sqlPart == SqlPart.SELECT && word.ToLower() == "from")
                {
                    sqlPart = SqlPart.FROM;
                    if (!TryGetNextWord(sql, ref index, out word, out isParenthesis, out isComma))
                    {
                        sqlPart = SqlPart.UNKNOWN;
                    }
                    indexBeforeTryNextWord = index;
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
                                string dbName = word;
                                GetNextFromAliasAndJoin(sql, ref indexBeforeTryNextWord, ref dbName, out string alias, out SqlJoinType nextJoinType, out string joinOnRawSql);
                                index = indexBeforeTryNextWord;
                                sqlQuery.SqlFrom.SqlFromElements.Add(
                                    new SqlFromElement()
                                    {
                                        SqlFromElementType = SqlFromElementType.Table,
                                        SqlTable = new SqlTable()
                                        {
                                            DbName = dbName,
                                        },
                                        Alias = alias,
                                        SqlJoinType = lastJoinType,
                                        JoinOnRawSql = joinOnRawSql
                                    });
                                lastJoinType = nextJoinType;
                            }
                            else
                            {
                                index++;
                                GetNextNonSpace(sql, ref index);
                                string parenthesis = GetParenthesis(sql, ref index, 1);
                                var subQuery = Parse(parenthesis);
                                GetNextNonSpace(sql, ref index);
                                index++;//move after ')'
                                indexBeforeTryNextWord = index;
                                if (TryGetNextWord(sql, ref index, out word, out isParenthesis, out isComma))
                                {
                                    string dbName = "";
                                    if (word.ToLower() == "as")
                                    {
                                        index = indexBeforeTryNextWord;
                                    }
                                    GetNextFromAliasAndJoin(sql, ref index, ref dbName, out string alias, out SqlJoinType nextJoinType, out string joinOnRawSql);
                                    sqlQuery.SqlFrom.SqlFromElements.Add(
                                        new SqlFromElement()
                                        {
                                            SqlFromElementType = SqlFromElementType.Query,
                                            SqlQuery = subQuery,
                                            Alias = alias,
                                            SqlJoinType = lastJoinType,
                                            JoinOnRawSql = joinOnRawSql
                                        });
                                    lastJoinType = nextJoinType;
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

        private void GetNextFromAliasAndJoin(string sql, ref int index, ref string dbName, out string alias, out SqlJoinType nextJoinType, out string joinOnRawSql)
        {
            alias = null;
            nextJoinType = SqlJoinType.NONE;
            joinOnRawSql = null;
            bool isBeginningOfNewFrom = true;

            //var regex = new Regex("([\\s]|[\\r]||[\\n])+from([\\s]|[\\r]||[\\n])+(?<From>.*)$");
            //var match = regex.Match(sql.Substring(index).ToLower());
            //if(match.Success)
            //{
            //    index = match.Groups["From"].Index;
            //}

            var indexTmp = index;
            if(TryGetNextWord(sql, ref indexTmp, out string word, out bool isParenthesis, out bool isComma))
            {
                if(word.ToLower() == "where")
                {
                    return;
                }
                else if(word.ToLower() == "as")
                {
                    if (TryGetNextWord(sql, ref indexTmp, out word, out isParenthesis, out isComma))
                    {
                        alias = word;
                        index = indexTmp;
                    }
                }
                else if(!isComma && word.ToLower() != "on" & !_wordsJoin.Contains(word.ToLower()))
                {
                    dbName = word;
                    if (TryGetNextWord(sql, ref indexTmp, out word, out isParenthesis, out isComma))
                    {
                        if(word.ToLower() != "as" && word.ToLower() != "on")
                        {
                            alias = word;
                        }
                        else if (word.ToLower() == "as" 
                            && TryGetNextWord(sql, ref indexTmp, out word, out isParenthesis, out isComma))
                        {
                            alias = word;
                        }
                    }
                    index = indexTmp;
                }
            }

            GetNextNonSpace(sql, ref index);
            int i = index;
            if (i >= sql.Length)
            {
                return;
            }

            var sb = new StringBuilder();
            char c = sql[i];
            while (i < sql.Length)
            {
                c = sql[i];
                isBeginningOfNewFrom = false;
                indexTmp = i;
                if (c == '(')
                {
                    int iBeforeParenthesis = i;
                    string parenthesis = GetParenthesis(sql, ref i, 0);
                    GetNextNonSpace(sql, ref i);
                    i++;//move after ')'
                    sb.Append(sql.Substring(iBeforeParenthesis, i - iBeforeParenthesis));
                }
                else if (c == ',')
                {
                    i++;
                    isBeginningOfNewFrom = true;
                }
                else if (TryGetNextWord(sql, ref indexTmp, out word, out isParenthesis, out isComma))
                {
                    if (word.ToLower() == "where")
                    {
                        return;
                    }
                    else if (word.ToLower() == "on")
                    {
                        i += "on".Length;
                    }
                    else if (_wordsJoin.Contains(word.ToLower()))
                    {
                        isBeginningOfNewFrom = true;
                        indexTmp = i;
                        TryGetNextWord(sql, ref indexTmp, out string word1, out isParenthesis, out isComma);
                        int indexAfterWord1 = indexTmp;
                        TryGetNextWord(sql, ref indexTmp, out string word2, out isParenthesis, out isComma);
                        int indexAfterWord2 = indexTmp;
                        TryGetNextWord(sql, ref indexTmp, out string word3, out isParenthesis, out isComma);
                        int indexAfterWord3 = indexTmp;
                        if (word1.ToLower() == "inner" && word2.ToLower() == "join")
                        {
                            nextJoinType = SqlJoinType.INNER_JOIN;
                            i = indexAfterWord2;
                        }
                        else if(word1.ToLower() == "left" && word2.ToLower() == "join")
                        {
                            nextJoinType = SqlJoinType.LEFT_JOIN;
                            i = indexAfterWord2;
                        }
                        else if (word1.ToLower() == "left" && word2.ToLower() == "outer" && word3.ToLower() == "join")
                        {
                            nextJoinType = SqlJoinType.LEFT_JOIN;
                            i = indexAfterWord3;
                        }
                        else if (word1.ToLower() == "right" && word2.ToLower() == "join")
                        {
                            nextJoinType = SqlJoinType.RIGHT_JOIN;
                            i = indexAfterWord2;
                        }
                        else if (word1.ToLower() == "right" && word2.ToLower() == "outer" && word3.ToLower() == "join")
                        {
                            nextJoinType = SqlJoinType.RIGHT_JOIN;
                            i = indexAfterWord3;
                        }
                    }
                }

                if (isBeginningOfNewFrom)
                {
                    break;
                }
                else if(i<sql.Length)
                {
                    c = sql[i];
                    sb.Append(c);
                }

                i++;
            }
            joinOnRawSql = sb.ToString();
            index = i;
        }

        public string GetTestSql1()
        {

            return @"
select *
from (select toto.* from public.personne_src as toto) as a
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
