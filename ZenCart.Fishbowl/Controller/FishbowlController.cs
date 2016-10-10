using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using FirebirdSql.Data.FirebirdClient;
using FishbowlSDK;
using ZenCart.Fishbowl.Configuration;
using ZenCart.Fishbowl.Models;
using ZenCart.Fishbowl.Extensions;

namespace ZenCart.Fishbowl.Controller
{
    public class FishbowlController : IDisposable
    {
        private Config cfg;
        private FbConnection db { get; set; }
        public FishbowlSDK.Fishbowl api { get; set; }
        public FishbowlController(Config cfg)
        {
            this.cfg = cfg;
            db = InitDB();
            api = InitAPI();
        }
        private FbConnection InitDB()
        {
            String CSB = InitCSB();
            FbConnection db = new FbConnection(CSB);
            db.Open();
            return db;
        }

        private string InitCSB()
        {
            FbConnectionStringBuilder csb = new FbConnectionStringBuilder();
            csb.DataSource = cfg.FB.ServerAddress;
            csb.Database = cfg.FB.DBPath;
            csb.UserID = cfg.FB.DBUser;
            csb.Password = cfg.FB.DBPass;
            csb.Port = cfg.FB.DBPort;
            csb.ServerType = FbServerType.Default;
            return csb.ToString();
        }

        private FishbowlSDK.Fishbowl InitAPI()
        {
            var newfb = new FishbowlSDK.Fishbowl(cfg.FB.ServerAddress, cfg.FB.ServerPort, cfg.FB.FBIAKey, cfg.FB.FBIAName, cfg.FB.FBIADesc, cfg.FB.Persistent, cfg.FB.Username, cfg.FB.Password);
            return newfb;
        }
        
        public CountryAndState GetCountryState(string Country, string State)
        {
            CountryAndState cas = new CountryAndState();

            Countryconst ct;
            Stateconst st;

            /// Get the country
            ct = db.Query<Countryconst>("select first 1 * from countryconst where UPPER(abbreviation) containing UPPER(@abb) or UPPER(name) containing UPPER(@n) ", new { n = Country, abb = Country.Truncate(10) }).FirstOrDefault();

            // If we have no country, lookup just by state
            if (ct == null || ct.ID == null)
            {
                st = db.Query<Stateconst>("select first 1 * from stateconst where UPPER(name) containing UPPER(@st) or UPPER(code) containing UPPER(@abb)  ", new { st = State, abb = State.Truncate(21) }).FirstOrDefault();
            }
            else // If we have a country, include that in the lookup
            {
                st = db.Query<Stateconst>("select first 1 * from stateconst where UPPER(name) containing UPPER(@st) or UPPER(code) containing UPPER(@abb) and countryconstid = @cid ", new { st = State, abb = State.Truncate(21), cid = ct.ID }).FirstOrDefault();
            }

            // If we have a state and no country
            if (st != null && ct == null)
            {
                // Lookup the country
                ct = db.Query<Countryconst>("select first 1 * from countryconst where id = @cid", new { cid = st.COUNTRYCONSTID }).FirstOrDefault();
            }

            if (st == null || ct == null)
            {
                throw new Exception("Cant find Country and Or State. [" + Country + "] [" + State + "] ");
            }

            cas.State = st;
            cas.Country = ct;

            return cas;
        }

         public void CreateCustomer(Customer customer)
         {
             api.SaveCustomer(customer, true);
         }

         public object LoadCustomer(string customerName)
         {
             return api.GetCustomer(customerName);
         }
         

        public string FindCustomerNameByEmail(object email)
        {
            return db.Query<String>(SQL.FB.FindCustomerByEmail, new { eml = email }).SingleOrDefault();
        }
        public List<ZCFBOrder> MapCustomerID(List<ZCFBOrder> orders)
        {
            var ret = new List<ZCFBOrder>();
            foreach (var o in orders)
            {
                string id = db.Query<string>((String.Format("select ID from customer where name = '{0}'", o.CustomerName))).FirstOrDefault();
                o.CustomerID = id;
                ret.Add(o);
            }
            return ret;
        }

        public bool CustomerExists(object customerName)
        {
            var text = db.Query<String>("select name from customer where name = @c", new { c = customerName }).SingleOrDefault();
            return customerName.Equals(text);
        }

        public List<String> GetAllProducts()
        {
            return db.Query<String>("select num from product").ToList();
        }
         
        public bool SaveSalesOrder(SalesOrder fbOrder, out String SONum, out String msg, out Double OrderTotal)
        {

            try
            {
                var newSO = api.SaveSO(fbOrder, true);
                SONum = newSO.Number;
                OrderTotal = newSO.Items.Where(k => k.ItemType != "40").Select(k => k.TotalPrice).Sum() + newSO.TotalTax; // Fix Payments To Have Tax Amounts
                msg = "Created OK";
                return newSO != null;
            }
            catch (Exception ex)
            {
                SONum = "";
                msg = ex.Message;
                OrderTotal = 0;
                return false;
            }

        }

        public bool CheckSoExists(string customerPo)
        {
            String so =
                db.Query<String>("select first 1 num from so where customerpo = @cpo", new { cpo = customerPo })
                    .SingleOrDefault();

            return !(string.IsNullOrEmpty(so));
        }

        public void Dispose()
        {
            if (api != null)
                api.Dispose();

            if (db != null)
                db.Dispose();
        }
    }
}

