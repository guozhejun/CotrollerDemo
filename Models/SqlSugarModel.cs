using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CotrollerDemo.Models
{
    public static class SqlSugarModel
    {
        public static SqlSugarClient Db = new(new ConnectionConfig()
        {
            ConnectionString = "datasource=demo.db",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        },
        db =>
        {

            db.Aop.OnLogExecuting = (sql, pars) =>
            {

                //获取原生SQL推荐 5.1.4.63  性能OK
                UtilMethods.GetNativeSql(sql, pars);
            };

        });
    }
}
