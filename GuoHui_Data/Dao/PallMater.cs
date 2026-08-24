using System;
using System.Linq;
using System.Text;
using SqlSugar;

namespace Models
{
    ///<summary>
    ///
    ///</summary>
    [SugarTable("PallMater")]
    public partial class PallMater
    {
           public PallMater(){


           }
           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:False
           /// </summary>           
           [SugarColumn(IsPrimaryKey=true,IsIdentity=true)]
           public int Id {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? PallNo {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weight {get;set;}

           /// <summary>
           /// Desc:位置id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? LocationCode {get;set;}

           /// <summary>
           /// Desc:库位id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? ShelfCode {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? Matet4 {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? Mater5 {get;set;}

           /// <summary>
           /// Desc:备注
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? Remark {get;set;}

           /// <summary>
           /// Desc:创建时间
           /// Default:
           /// Nullable:True
           /// </summary>           
           public DateTime? CreateTime {get;set;}

           /// <summary>
           /// Desc:更新时间
           /// Default:
           /// Nullable:True
           /// </summary>           
           public DateTime? UpdateTime {get;set;}

           /// <summary>
           /// Desc:产品1标识id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle1 {get;set;}

           /// <summary>
           /// Desc:产品1毛重量
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh1 {get;set;}

           /// <summary>
           /// Desc:产品2标识id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle2 {get;set;}

           /// <summary>
           /// Desc:产品2毛重量
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh2 {get;set;}

           /// <summary>
           /// Desc:产品3标识id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle3 {get;set;}

           /// <summary>
           /// Desc:产品3毛重量
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh3 {get;set;}

           /// <summary>
           /// Desc:产品4标识id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle4 {get;set;}

           /// <summary>
           /// Desc:产品4毛重量
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh4 {get;set;}

           /// <summary>
           /// Desc:产品5标识id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle5 {get;set;}

           /// <summary>
           /// Desc:产品5毛重量
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh5 {get;set;}

           /// <summary>
           /// Desc:产品6标识id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle6 {get;set;}

           /// <summary>
           /// Desc:产品6毛重量
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh6 {get;set;}

           /// <summary>
           /// Desc:产品7标识id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle7 {get;set;}

           /// <summary>
           /// Desc:产品7毛重量
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh7 {get;set;}

           /// <summary>
           /// Desc:产品8标识id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle8 {get;set;}

           /// <summary>
           /// Desc:产品8毛重量
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh8 {get;set;}

           /// <summary>
           /// Desc:产品9标识id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle9 {get;set;}

           /// <summary>
           /// Desc:产品9毛重量
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh9 {get;set;}

           /// <summary>
           /// Desc:产品10标识id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle10 {get;set;}

           /// <summary>
           /// Desc:产品10毛重量
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh10 {get;set;}

           /// <summary>
           /// Desc:产品11标识id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle11 {get;set;}

           /// <summary>
           /// Desc:产品11毛重量
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh11 {get;set;}

           /// <summary>
           /// Desc:产品12标识id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle12 {get;set;}

           /// <summary>
           /// Desc:产品12毛重量
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh12 {get;set;}

           /// <summary>
           /// Desc:产品13标识id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle13 {get;set;}

           /// <summary>
           /// Desc:产品13毛重量
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh13 {get;set;}

           /// <summary>
           /// Desc:产品14标识id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle14 {get;set;}

           /// <summary>
           /// Desc:产品14毛重量
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh14 {get;set;}

           /// <summary>
           /// Desc:产品15标识id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle15 {get;set;}

           /// <summary>
           /// Desc:产品15毛重量
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh15 {get;set;}

    }
}
