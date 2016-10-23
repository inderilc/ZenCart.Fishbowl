using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenCart.Fishbowl.Configuration;
using ZenCart.Fishbowl.Controller;
using ZenCart.Fishbowl.Models;
using ZenCart.Fishbowl.Map;
using System.Net.Mail;
using System.Net;
using CsvHelper.Configuration;
using CsvHelper;
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
            this.cfg = cfg;
        }


        public void Run()
        {
            char userInput;
            char upper;

            Log("Starting Integration");

            InitConnections();

            Log("Ready");

            do
            {
                Console.Clear();
                System.Console.WriteLine("---------------------------------------------------------");
                System.Console.WriteLine("1. ZenCart to Fishbowl Download");
                System.Console.WriteLine("2. Create Products in ZenCart");
                System.Console.WriteLine("0. Exit");
                System.Console.WriteLine("---------------------------------------------------------");
                System.Console.WriteLine("Enter an option --->");
                userInput = Convert.ToChar(Console.ReadLine());
                upper = char.ToUpper(userInput);

                if (upper == '1')
                {
                    DownloadOrders();
                }
                else if (upper == '2')
                {
                    CreateOrderZenCart();
                }
                else if (upper == '0')
                {
                    Console.WriteLine("Exiting service");
                }
                else
                    Console.WriteLine("You entered an incorrect option, please select new option");
            }
            while (upper != '0');
            
        }
        public void EmailLog(String file)
        {
            MailMessage m = new MailMessage();
            String addresses = (cfg.Email.LogEmail).Replace(",", ";");
            m.Subject = "ZenCart to Fishbowl Download Log attached.";
            m.Body = "ZenCart to Fishbowl download log files are attached. The log file "+ Path.GetFileName(file)+" is attached";

            m.Attachments.Add(new Attachment(file));

            m.To.Add(addresses);
            SendEmail(cfg.Email, m);

        }

        private static Boolean SendEmail(EmailConfig cfg, MailMessage m)
        {
            try
            {
                SmtpClient smtp = GenSMTP(cfg);
                m.From = new MailAddress(cfg.MailFrom, cfg.MailFromName);
                smtp.Send(m);
                return true;
            }
            catch (Exception ex)
            {
                File.AppendAllText("smtplog.txt", ex.ToString());
                return false;
            }
        }

        private static SmtpClient GenSMTP(EmailConfig cfg)
        {
            SmtpClient smtp = new SmtpClient(cfg.Host, cfg.Port);
            smtp.Credentials = new NetworkCredential(cfg.User, cfg.Pass);
            smtp.EnableSsl = cfg.SSL;

            return smtp;
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
            CreateOrderZenCart();
            Log("Orders Downloaded: " + orders.Count);
            if (orders.Count > 0)
            {
                List<ZCFBOrder> ofOrders = DataMappers.MapNewOrders(orders);
                ofOrders = fb.MapCustomerID(ofOrders);
                
                Log("Creating and Validating Customer Names.");
                ValidateCreateCustomers(ofOrders);
                Log("Validated Customers");

                /* Will need to turn-on this code, once the Create Product functionality is in place....
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
        public void CreateOrderZenCart()
        {
            Log("Downloading Products information from Fishbowl.");
            List<ProductDataFB> fbProducts = fb.GetProductsInfo();
            Log("Products information downloaded from Fishbowl. Total Fishbowl Products:" + fbProducts.Count());
            CheckNCreateZenProducts(fbProducts);
        }
        public void CheckNCreateZenProducts(List<ProductDataFB> fbProducts)
        {
            int i = 0;
            Log("Downloading Products information from ZenCart.");
            List<ProductDataClass> zcProducts = zc.GetAllZenProductsInfo();
            Log("Products information downloaded from ZenCart. Total ZenCart Products:" + zcProducts.Count());
            Log("Checking to see if any products need to be created");
            foreach (var fbProduct in fbProducts)
            {
                foreach (var zcProduct in zcProducts)
                {
                    if (fbProduct.NUM == zcProduct.products_model)
                    {
                        fbProduct.isNotCreating = true;
                        i++;
                    } 
                }
            }
            List<ProductDataFB> UploadList = new List<ProductDataFB>();
            foreach (var fbProduct in fbProducts)
            {
                if (!fbProduct.isNotCreating)
                {
                    UploadList.Add(fbProduct);
                }
            }
            Log("Total Products need to be created " + (UploadList.Count) + ".");

            String CSV = GenerateProductsCSV(UploadList);

            bool created = zc.CreateProducts(CSV);
            if (created)
            {
                Log("Products created succesfully");
            }
            else
            {
                Log("Products creation failed");
            }
        }

        private String GenerateProductsCSV(List<ProductDataFB> products)
        {
            StringWriter sw = new StringWriter();
            var csv = new CsvWriter(sw);
            csv.Configuration.RegisterClassMap<ProductDataFBMap>();
            csv.Configuration.HasHeaderRecord = true;
            csv.Configuration.QuoteAllFields = false;
            csv.WriteHeader(typeof(ProductDataFB));
            csv.WriteRecords(products);
            return sw.ToString();
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
