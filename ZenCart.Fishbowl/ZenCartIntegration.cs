using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenCart.Fishbowl.Configuration;
using ZenCart.Fishbowl.Controller;
using ZenCart.Fishbowl.Models;
using ZenCart.Fishbowl.Map;

using System.IO;

namespace ZenCart.Fishbowl
{
    public class ZenCartIntegration : IDisposable
    {
        public event LogMsg OnLog;
        public delegate void LogMsg(String msg);

        private Config cfg { get; set; }
        private FishbowlController fb { get; set; }
        private ZenCartController zc { get; set; }

        public ZenCartIntegration(Config cfg)
        {
            //this.raven = new RavenClient("https://5225b6cdf6f14280988ace71f4183f64:64c21d1f557a45f4a506c6c15ddbab80@app.getsentry.com/67029");
            this.cfg = cfg;
        }


        public void Run()
        {

            Log("Starting Integration");

            InitConnections();

            Log("Ready");

            DownloadOrders();
        }


        private void InitConnections()
        {
            if (fb == null)
            {
                Log("Connecting to Fishbowl");
                fb = new FishbowlController(cfg);
            }

            if (zc == null)
            {
                Log("Connecting to ZenCart");
                zc = new ZenCartController(cfg);
            }
        }

        public void DownloadOrders()
        {
            Log("Downloading Orders");
            List<ZCOrder> orders = zc.GetOrders();
            Log("Orders Downloaded: " + orders.Count);
            if (orders.Count > 0)
            {
                List<ZCFBOrder> ofOrders = DataMappers.MapNewOrders(orders);

                
                Log("Creating and Validating Customer Names.");
                ValidateCreateCustomers(ofOrders);
                Log("Validated Customers");

                /*
                Log("Validating Items in Fishbowl.");
                ValidateItems(ofOrders);
                Log("Items Validated");
                */

                Log("Creating Sales Orders Data.");
                ValidateOrder(cfg, ofOrders, "10 ");
                Log("Finished Creating Sales Order Data.");
              
                var ret = CreateSalesOrders(ofOrders);

                Log("Result: " + String.Join(Environment.NewLine, ret));

                Log("Downloading Orders Finished");
            }

        }

        private List<String> CreateSalesOrders(List<ZCFBOrder> ofOrders)
        {
            var ret = new List<String>();

            foreach (var o in ofOrders)
            {
                String soNum;

                bool soExists = fb.CheckSoExists(o.FbOrder.CustomerPO);

                if (!soExists)
                {
                    String msg = "";
                    Double ordertotal;
                    var result = fb.SaveSalesOrder(o.FbOrder, out soNum, out msg, out ordertotal);

                    zc.UpdateZC2FBDownloaded(Convert.ToInt32(o.Order.orders_id), soNum);
                }
                else
                {
                    ret.Add("SO Exists.");
                }

                Config.Save(cfg);
            }

            return ret;
        }

        private void ValidateOrder(Config cfg, List<ZCFBOrder> ofOrders, String OrderStatus)
        {
            foreach (var o in ofOrders)
            {
                o.FbOrder = DataMappers.MapSalesOrder(cfg, o, OrderStatus);
            }
        }
    
        private void ValidateItems(List<ZCFBOrder> ofOrders)
        {
            // Clean order models.
            ofOrders.ForEach(x =>
            {
                x.Order.Items.ForEach(y => y.products_id = y.products_id.Trim());
            });
            var fbProds = fb.GetAllProducts();
            foreach (var i in ofOrders)
            {
                var list = i.Order.Items.Select(x => x.products_id);
                var except = list.Except(fbProds);
                if (except.Any())
                {
                    throw new Exception($"Products Not Found on Order [{i.Order.orders_id}] Please Create Them: " + String.Join(",", except));
                }
            }
        }

        private void ValidateCreateCustomers(List<ZCFBOrder> ofOrders)
        {
            foreach (var x in ofOrders)
            {
                // Does the customer exist with the first order name?
                bool IsCustomerExists = fb.CustomerExists(x.CustomerName);
                if (!IsCustomerExists)
                {
                    // Maybe it does not, so check by email address.
                    String CustomerNameByEmail = fb.FindCustomerNameByEmail(x.Order.customers_email_address);
                    if (!String.IsNullOrWhiteSpace(CustomerNameByEmail))
                    {
                        x.CustomerName = CustomerNameByEmail;
                    }
                    // If it does not exist at all, try creating the customer
                    else
                    {
                        Log("Creating Customer Name: " + x.CustomerName);
                        CreateCustomer(x.CustomerName, x.Order);
                        Log("Customer Created!");
                    }
                }
                // Load the Customer so we have the entire object later.
                Log("Loading Customer Fishbowl");
                var fbCustomer = fb.LoadCustomer(x.CustomerName);
                if (fbCustomer == null)
                {
                    throw new Exception(
                        "Cannot continue if a Customer Name is Missing, Or Cannot Be Loaded from Fishbowl. " +
                        x.CustomerName);
                }
            }
        }
        private void CreateCustomer(string customerName, ZCOrder Order)
        {
            Log("Creating Fishbowl Customer " + customerName);
            var cas = fb.GetCountryState(Order.billing_country, Order.billing_state);
            var customer = DataMappers.MapCustomer(cfg, Order, customerName, cas);
            fb.CreateCustomer(customer);
        }



        public void Log(String msg)
        {
            if (OnLog != null)
            {
                OnLog(msg);
            }
        }

        private void LogException(Exception ex)
        {
            String msg = ex.Message;
            Log(msg);
            File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "exception.txt", ex.ToString() + Environment.NewLine);
            ///raven.CaptureException(ex);
        }

        public void Dispose()
        {
            if (fb != null)
                fb.Dispose();
        }
    }
}
