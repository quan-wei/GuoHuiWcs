using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Guohui_Wcs.Utils
{
    public class HttpUtils
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static string postType = "post";
        public static string getType = "get";
        /// <summary>
        /// 调用api返回json
        /// </summary>
        /// <param name="url">api地址</param>
        /// <param name="jsonstr">接收参数</param>
        /// <param name="type">类型</param>
        /// <returns></returns>         
        public string HttpApi(string url, string jsonstr, string type)
        {
            Encoding encoding = Encoding.UTF8;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);//webrequest请求api地址
            request.Accept = "text/html,application/xhtml+xml,*/*";
            request.ContentType = "application/json";
            //for (int i=0;i< request.Headers.Count;i++)
            //{
            //    Console.WriteLine(request.Headers[i].ToString());
            //}
            //Debug.WriteLine(jsonstr);
            request.Method = type.ToUpper().ToString();//get或者post
            byte[] buffer = encoding.GetBytes(jsonstr);
            request.ContentLength = buffer.Length;
            request.GetRequestStream().Write(buffer, 0, buffer.Length);
            //Console.WriteLine(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="obj"></param>
        /// <param name="type"></param>
        /// <param name="HasNullProperty">是否含有Null值，True则将null转为string.Empty</param>
        /// <returns></returns>
        public string HttpPost(string url, object obj, Type type, bool HasNullProperty = true)
        {

            try
            {
                //Console.WriteLine(url);
                using (HttpClient httpClient = new HttpClient())
                {
                    //HttpContent body = null;
                    //if (obj != null)
                    //{
                    //    //Type t = obj.GetType();
                    //    Dictionary<string, string> dic = new Dictionary<string, string>();
                    //    foreach (PropertyInfo pi in type.GetProperties())
                    //    {

                    //        //var name = pi.Name;//获得属性的名字,后面就可以根据名字判断来进行些自己想要的操作
                    //        //var value = pi.GetValue(obj, null);//用pi.GetValue获得值
                    //        try
                    //        {
                    //            if (HasNullProperty)
                    //            {
                    //                foreach (PropertyInfo pi2 in pi.PropertyType.GetProperties())
                    //                {
                    //                    try
                    //                    {
                    //                        dic.Add(pi2.Name, pi2.GetValue(obj, null).ToString());
                    //                    }
                    //                    catch
                    //                    {
                    //                        dic.Add(pi2.Name, string.Empty);
                    //                    }
                    //                }
                    //            }
                    //            else
                    //                dic.Add(pi.Name, pi.GetValue(obj, null).ToString());
                    //        }
                    //        catch
                    //        {
                    //            dic.Add(pi.Name, string.Empty);
                    //        }
                    //    }

                    //    body = new FormUrlEncodedContent(dic);
                    //}
                    //else
                    //{
                    //    Dictionary<string, string> dic = new Dictionary<string, string>();
                    //    dic.Add("test", "1");
                    //    body = new FormUrlEncodedContent(dic);
                    //}
                    //body = new FormUrlEncodedContent(null);
                    string requestId = Guid.NewGuid().ToString("N"); // 去掉横杠
                    httpClient.DefaultRequestHeaders.Add("X-LR-REQUEST-ID", requestId);
                    
                    var jsonStr = JsonConvert.SerializeObject(obj);
                    StringContent content = new StringContent(
                       jsonStr, Encoding.UTF8, "application/json");
                    //var jsonStr = content.ReadAsStringAsync().Result;
                    Logger.Info(
                       $"开始发送接口请求:url:{url}\r\nbody:{jsonStr}");
                    //Debug.WriteLine("url:" + url);
                    //Debug.WriteLine("body:" + jsonStr);
                    var response = httpClient.PostAsync(url, content).Result;
                    var data = response.Content.ReadAsStringAsync().Result;
                    //Logger.Default.Process(new Log(LevelType.Info,
                    //   $"接口请求结果::{data}\r\nurl:{url}\r\nbody:{jsonStr}"));
                    //Debug.WriteLine("结果:"+ data);

                    //Debug.WriteLine("------------");
                    return data;

                    //return data.Substring(1, data.Length - 2).Replace("\\", string.Empty);
                }

            }
            catch (Exception ex)
            {
                Logger.Debug(
                       $"发送接口请求失败:url:{url}\r\n 错误:{ex.ToString()}\r\nType:{type}\r\nbody:{obj.ToString()}");
                //MessageBox.Show("数据库连接失败，请检查网络连接");
                return string.Empty;
            }
        }


        public async Task<string> HttpPostAsync(string url, object obj, Type type, bool HasNullProperty = true)
        {

            try
            {
                //Console.WriteLine(url);
                using (HttpClient httpClient = new HttpClient())
                {
                    string requestId = Guid.NewGuid().ToString("N"); // 去掉横杠
                    httpClient.DefaultRequestHeaders.Add("X-LR-REQUEST-ID", requestId);

                    var jsonStr = JsonConvert.SerializeObject(obj);
                    StringContent content = new StringContent(
                       jsonStr, Encoding.UTF8, "application/json");
                    //var jsonStr = content.ReadAsStringAsync().Result;
                    Logger.Info(
                       $"开始发送接口请求:url:{url}\r\nbody:{jsonStr}");

                    var response = await httpClient.PostAsync(url, content);
                    var data = await response.Content.ReadAsStringAsync();
                    //Logger.Default.Process(new Log(LevelType.Info,
                    //   $"接口请求结果::{data}\r\nurl:{url}\r\nbody:{jsonStr}"));
                    //Debug.WriteLine("结果:"+ data);

                    //Debug.WriteLine("------------");
                    return data;

                    //return data.Substring(1, data.Length - 2).Replace("\\", string.Empty);
                }

            }
            catch (Exception ex)
            {
                Logger.Debug(
                       $"发送接口请求失败:url:{url}\r\n 错误:{ex.ToString()}\r\nType:{type}\r\nbody:{obj.ToString()}");
                //MessageBox.Show("数据库连接失败，请检查网络连接");
                return string.Empty;
            }
        }

        /// <summary>
        /// formrulencoded的形式获取数据
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string PostFormUrlEncoded<TResult>(string url, Dictionary<string, string> parameters)
        {
            try
            {
                // 创建请求对象
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";

                // 将参数转换为表单数据格式
                var postData = string.Join("&", parameters.Select(p => $"{p.Key}={HttpUtility.UrlEncode(p.Value)}"));

                // 将表单数据写入请求正文
                var data = Encoding.UTF8.GetBytes(postData);
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                // 发送请求并获取响应
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    var responseStream = response.GetResponseStream();
                    using (var reader = new StreamReader(responseStream, Encoding.GetEncoding("gb2312")))
                    {
                        var responseContent = reader.ReadToEnd();
                        return responseContent;
                    }
                }
            }
            catch (Exception ex)
            {
                // 处理异常
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Http的Get请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <returns></returns>
        public string GetHttpGet(string url, object postData = null)
        {
            //接口返回报文
            string result = string.Empty;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url + (postData == null ? string.Empty : "?" + GetParm(postData)));
                request.Method = "GET";
                request.ContentType = "text/html;charset=UTF-8";
                request.Accept = "text/html;charset=UTF-8";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                result = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                myResponseStream.Close();
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                {
                    return ex.ToString();
                }
                var errorSr = new StreamReader(ex.Response.GetResponseStream());
                result = errorSr.ReadToEnd();
            }

            return result;
        }
        /// <summary>
        /// post带token
        /// </summary>
        /// <param name="url"></param>
        /// <param name="sendInfo"></param>
        /// <param name="token"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public static string GetHttpPostByToken(string url, string sendInfo, string token, string contentType = null)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Headers.Add("token", token);
            request.Method = "POST";
            if (contentType == null)
            {
                request.ContentType = "application/json;charset=UTF-8";
            }
            else
            {
                request.ContentType = contentType;
            }

            byte[] data = Encoding.UTF8.GetBytes(sendInfo);
            request.ContentLength = data.Length;
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            //接口返回
            string result = string.Empty;
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                string encoding = response.ContentEncoding;
                if (encoding == null || encoding.Length < 1)
                {
                    encoding = "UTF-8"; //默认编码 
                }
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding));
                result = reader.ReadToEnd();

            }
            catch (WebException ex)
            {
                var errorSr = new StreamReader(ex.Response.GetResponseStream());
                result = errorSr.ReadToEnd();
            }
            return result;

        }
        /// <summary>
        /// 将对象转换为get参数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parm"></param>
        /// <returns></returns>
        public string GetParm<T>(T parm)
        {
            var dic = new Dictionary<string, string>();
            ObjectConvertUtils objectConvertUtils = new ObjectConvertUtils();

            dic = objectConvertUtils.ObjectToMap(parm);
            List<string> strings = new List<string>();
            foreach (var item in dic)
            {
                string key = item.Key ?? string.Empty;
                string value = item.Value ?? string.Empty;
                strings.Add(key + "=" + value);
            }



            //Array.Sort(string, string.CompareOrdinal); //ASCII排序
            string str = string.Join("&", strings);

            return str;
        }
    }

}
