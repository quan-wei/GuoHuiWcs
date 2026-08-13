using System;
using System.Linq;
using System.Text;
using SqlSugar;

namespace Models
{
    ///<summary>
    ///
    ///</summary>
    [SugarTable("Location")]
    public partial class Location
    {
           public Location(){


           }
           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:False
           /// </summary>           
           [SugarColumn(IsPrimaryKey=true,IsIdentity=true)]
           public int Id {get;set;}

           /// <summary>
           /// Desc:货架id
           /// Default:
           /// Nullable:False
           /// </summary>           
           public string LocationCode {get;set;} = null!;

           /// <summary>
           /// Desc:地库id
           /// Default:
           /// Nullable:False
           /// </summary>           
           public string ShelfCode {get;set;} = null!;

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:False
           /// </summary>           
           public string LocationType {get;set;} = null!;

           /// <summary>
           /// Desc:状态，空闲为0，有货为1
           /// Default:0
           /// Nullable:False
           /// </summary>           
           public byte Status {get;set;}

           /// <summary>
           /// Desc:批次号，来自数据库
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? PallNo {get;set;}

           /// <summary>
           /// Desc:仓位位置，1为地库，2为二层，3为二层，4为三层，5为四层
           /// Default:
           /// Nullable:False
           /// </summary>           
           [SugarColumn(IsPrimaryKey=true)]
           public byte Ranked {get;set;}

           /// <summary>
           /// Desc:限制重量，最高重量
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? LimitWeightt {get;set;}

           /// <summary>
           /// Desc:现有重量
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? TotalWeight {get;set;}

           /// <summary>
           /// Desc:预留字段
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? Reserve5 {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>           
           public bool? EnableFlag {get;set;}

           /// <summary>
           /// Desc:
           /// Default:DateTime.Now
           /// Nullable:False
           /// </summary>           
           public DateTime CreateTime {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>           
           public DateTime? UpdateTime {get;set;}

    }
}
