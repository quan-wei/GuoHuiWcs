<<<<<<< HEAD
using System;
=======
ï»¿using System;
>>>>>>> 28c1743cce8a7eb149f164ce113c0e91048a2a1b
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
<<<<<<< HEAD
           /// Desc:Î»ÖÃid
=======
           /// Desc:ä½ç½®id
>>>>>>> 28c1743cce8a7eb149f164ce113c0e91048a2a1b
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? LocationCode {get;set;}

           /// <summary>
<<<<<<< HEAD
           /// Desc:¿âÎ»id
=======
           /// Desc:åº“ä½id
>>>>>>> 28c1743cce8a7eb149f164ce113c0e91048a2a1b
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
<<<<<<< HEAD
           /// Desc:±¸×¢
=======
           /// Desc:å¤‡æ³¨
>>>>>>> 28c1743cce8a7eb149f164ce113c0e91048a2a1b
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? Remark {get;set;}

           /// <summary>
<<<<<<< HEAD
           /// Desc:´´½¨Ê±¼ä
=======
           /// Desc:åˆ›å»ºæ—¶é—´
>>>>>>> 28c1743cce8a7eb149f164ce113c0e91048a2a1b
           /// Default:
           /// Nullable:True
           /// </summary>           
           public DateTime? CreateTime {get;set;}

           /// <summary>
<<<<<<< HEAD
           /// Desc:¸üÐÂÊ±¼ä
=======
           /// Desc:æ›´æ–°æ—¶é—´
>>>>>>> 28c1743cce8a7eb149f164ce113c0e91048a2a1b
           /// Default:
           /// Nullable:True
           /// </summary>           
           public DateTime? UpdateTime {get;set;}

           /// <summary>
<<<<<<< HEAD
           /// Desc:²úÆ·1±êÊ¶id
=======
           /// Desc:äº§å“1æ ‡è¯†id
>>>>>>> 28c1743cce8a7eb149f164ce113c0e91048a2a1b
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle1 {get;set;}

           /// <summary>
<<<<<<< HEAD
           /// Desc:²úÆ·1Ã«ÖØÁ¿
=======
           /// Desc:äº§å“1æ¯›é‡é‡
>>>>>>> 28c1743cce8a7eb149f164ce113c0e91048a2a1b
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh1 {get;set;}

           /// <summary>
<<<<<<< HEAD
           /// Desc:²úÆ·2±êÊ¶id
=======
           /// Desc:äº§å“2æ ‡è¯†id
>>>>>>> 28c1743cce8a7eb149f164ce113c0e91048a2a1b
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle2 {get;set;}

           /// <summary>
<<<<<<< HEAD
           /// Desc:²úÆ·2Ã«ÖØÁ¿
=======
           /// Desc:äº§å“2æ¯›é‡é‡
>>>>>>> 28c1743cce8a7eb149f164ce113c0e91048a2a1b
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh2 {get;set;}

           /// <summary>
<<<<<<< HEAD
           /// Desc:²úÆ·3±êÊ¶id
=======
           /// Desc:äº§å“3æ ‡è¯†id
>>>>>>> 28c1743cce8a7eb149f164ce113c0e91048a2a1b
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle3 {get;set;}

           /// <summary>
<<<<<<< HEAD
           /// Desc:²úÆ·3Ã«ÖØÁ¿
=======
           /// Desc:äº§å“3æ¯›é‡é‡
>>>>>>> 28c1743cce8a7eb149f164ce113c0e91048a2a1b
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh3 {get;set;}

           /// <summary>
<<<<<<< HEAD
           /// Desc:²úÆ·4±êÊ¶id
=======
           /// Desc:äº§å“4æ ‡è¯†id
>>>>>>> 28c1743cce8a7eb149f164ce113c0e91048a2a1b
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle4 {get;set;}

           /// <summary>
<<<<<<< HEAD
           /// Desc:²úÆ·4Ã«ÖØÁ¿
=======
           /// Desc:äº§å“4æ¯›é‡é‡
>>>>>>> 28c1743cce8a7eb149f164ce113c0e91048a2a1b
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh4 {get;set;}

           /// <summary>
<<<<<<< HEAD
           /// Desc:²úÆ·5±êÊ¶id
=======
           /// Desc:äº§å“5æ ‡è¯†id
>>>>>>> 28c1743cce8a7eb149f164ce113c0e91048a2a1b
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle5 {get;set;}

           /// <summary>
<<<<<<< HEAD
           /// Desc:²úÆ·5Ã«ÖØÁ¿
=======
           /// Desc:äº§å“5æ¯›é‡é‡
>>>>>>> 28c1743cce8a7eb149f164ce113c0e91048a2a1b
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh5 {get;set;}

           /// <summary>
<<<<<<< HEAD
           /// Desc:²úÆ·6±êÊ¶id
=======
           /// Desc:äº§å“6æ ‡è¯†id
>>>>>>> 28c1743cce8a7eb149f164ce113c0e91048a2a1b
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle6 {get;set;}

           /// <summary>
<<<<<<< HEAD
           /// Desc:²úÆ·6Ã«ÖØÁ¿
=======
           /// Desc:äº§å“6æ¯›é‡é‡
>>>>>>> 28c1743cce8a7eb149f164ce113c0e91048a2a1b
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh6 {get;set;}

<<<<<<< HEAD
           /// <summary>
           /// Desc:²úÆ·7±êÊ¶id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle7 {get;set;}

           /// <summary>
           /// Desc:²úÆ·7Ã«ÖØÁ¿
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh7 {get;set;}

           /// <summary>
           /// Desc:²úÆ·8±êÊ¶id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle8 {get;set;}

           /// <summary>
           /// Desc:²úÆ·8Ã«ÖØÁ¿
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh8 {get;set;}

           /// <summary>
           /// Desc:²úÆ·9±êÊ¶id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle9 {get;set;}

           /// <summary>
           /// Desc:²úÆ·9Ã«ÖØÁ¿
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh9 {get;set;}

           /// <summary>
           /// Desc:²úÆ·10±êÊ¶id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle10 {get;set;}

           /// <summary>
           /// Desc:²úÆ·10Ã«ÖØÁ¿
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh10 {get;set;}

           /// <summary>
           /// Desc:²úÆ·11±êÊ¶id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle11 {get;set;}

           /// <summary>
           /// Desc:²úÆ·11Ã«ÖØÁ¿
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh11 {get;set;}

           /// <summary>
           /// Desc:²úÆ·12±êÊ¶id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle12 {get;set;}

           /// <summary>
           /// Desc:²úÆ·12Ã«ÖØÁ¿
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh12 {get;set;}

           /// <summary>
           /// Desc:²úÆ·13±êÊ¶id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle13 {get;set;}

           /// <summary>
           /// Desc:²úÆ·13Ã«ÖØÁ¿
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh13 {get;set;}

           /// <summary>
           /// Desc:²úÆ·14±êÊ¶id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle14 {get;set;}

           /// <summary>
           /// Desc:²úÆ·14Ã«ÖØÁ¿
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh14 {get;set;}

           /// <summary>
           /// Desc:²úÆ·15±êÊ¶id
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string? SubTitle15 {get;set;}

           /// <summary>
           /// Desc:²úÆ·15Ã«ÖØÁ¿
           /// Default:
           /// Nullable:True
           /// </summary>           
           public decimal? Weigh15 {get;set;}

=======
>>>>>>> 28c1743cce8a7eb149f164ce113c0e91048a2a1b
    }
}
