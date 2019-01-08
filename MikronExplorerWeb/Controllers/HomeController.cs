using NanoDotNet;
using System.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Http;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;

namespace MikronExplorerWeb.Controllers
{

    public class Data
    {
        public BigInteger all { get; set; }
        public IOrderedEnumerable<KeyValuePair<string, AccountInformationResponse>> data { get; set; }
        public Dictionary<string, string> ips { get; internal set; }
    }

    public class AccountInfo
    {
        public AccountInformationResponse account { get; set; }
        public decimal sent { get; set; }
        public decimal received { get; set; }
        public IEnumerable<AccountHistoryItem> History { get; set; }
    }


    public class HomeController : Controller
    {

        List<string> ls = new List<string>();
        private async Task<List<string>> Accounts(string acc)
        {
            using (var client = new NanoRpcClient("http://localhost:7043"))
            {
                var h = await client.GetAccountHistoryAsync(new NanoAccount(acc), 100000);
                if (h.History == null) return ls;
                foreach (var x in h.History)
                {
                    if (!ls.Contains(x.Account))
                    {
                        ls.Add(x.Account);
                        var xl = await Accounts(x.Account);
                    }
                }
                return ls;
            }
        }

        public async Task<ActionResult> Index()
        {
            using (var client = new NanoRpcClient("http://localhost:7043"))
            {


                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://localhost:7043");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = "{\"action\":\"peers\"}";
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                var IPs = new Dictionary<string, string>();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {                    
                    var result = streamReader.ReadToEnd();
                    try
                    {
                        JObject jo = JObject.Parse(result);
                        var ja = (JArray)jo["peers"];
                        foreach (var a in ja)
                        {
                            var end = (string)a["endpoint"];
                            var p = end.Replace("[", "").Replace("]", "").Replace("::ffff:", "");                            
                            var addr = IPAddress.Parse(p.Split(':')[0]);
                            IPs.Add((string)a["node_id"], addr.ToString());
                        }
                    }
                    catch { }
                }

                var h = await client.GetAccountHistoryAsync(new NanoAccount("mik_1yumkjq6xscowmmz6sca79sycoup1tpqabnc3zdqgpcd1756xgo1k53z7yeg"), 10000);
                foreach (var x in h.History)
                {
                    ls.AddRange(await Accounts(x.Account));
                }
                Dictionary<string, AccountInformationResponse> ir = new Dictionary<string, AccountInformationResponse>();
                BigInteger all = 0;
                foreach (var a in ls)
                {
                    if (ir.ContainsKey(a)) continue;
                    var ai = await client.GetAccountInformationAsync(new NanoAccount(a));
                    ir.Add(a, ai);                    
                    all += ai.Balance.Raw;                    
                }
                IOrderedEnumerable<KeyValuePair<string, AccountInformationResponse>> or = ir.OrderByDescending(p => p.Value.Balance.Raw);
                
                return View(new Data{ data = or, all = all, ips = IPs });
            }
        }

        public async Task<ActionResult> Account([FromUri] string address)
        {
            using (var client = new NanoRpcClient("http://localhost:7043"))
            {
                
                decimal sent = 0;
                decimal received = 0;
                AccountHistory h = await client.GetAccountHistoryAsync(new NanoAccount(address.Trim()), 10000);
                AccountInformationResponse account = await client.GetAccountInformationAsync(new NanoAccount(address.Trim()));
                if(h.History!=null)
                foreach(var a in h.History)
                {
                    if(a.Type == "send")
                    {
                        sent += (decimal)a.Amount.Raw / 10000000000M;
                    }
                    else
                    {
                        received += (decimal)a.Amount.Raw / 10000000000M;
                    }
                }
                return View(new AccountInfo
                {
                    account = account,
                    History = h.History,
                    sent = sent,
                    received = received
                });
            }                
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}