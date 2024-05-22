// See https://aka.ms/new-console-template for more information
using ConsoleApp1;
using ConsoleApp1.SqlParser.Engine;
using System.Data;
using System.Globalization;


var sqlParser = new SqlParserEngine();
string sql = sqlParser.GetTestSql1();
var query = sqlParser.Parse(sql);
query = query;


