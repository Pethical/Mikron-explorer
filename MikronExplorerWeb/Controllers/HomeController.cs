using NanoDotNet;
using System.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Http;

namespace MikronExplorerWeb.Controllers
{

    public class Data
    {
        public BigInteger all { get; set; }
        public IOrderedEnumerable<KeyValuePair<string, AccountInformationResponse>> data { get; set; }
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
                        // ls.AddRange(xl);
                    }
                }
                return ls;
            }
        }

        public async Task<ActionResult> Index()
        {
            using (var client = new NanoRpcClient("http://localhost:7043"))
            {

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

                return View(new Data{ data = or, all = all });
            }
        }

        public async Task<ActionResult> Account([FromUri] string address)
        {
            using (var client = new NanoRpcClient("http://localhost:7043"))
            {
                AccountHistory h = await client.GetAccountHistoryAsync(new NanoAccount(address), 10000);                                
                return View(h.History);
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