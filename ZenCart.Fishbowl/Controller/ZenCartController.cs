using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudDataClient;
using ZenCart.Fishbowl.Configuration;
using ZenCart.Fishbowl.Models;
using RestSharp;
using System.Net;
using System.IO;

namespace ZenCart.Fishbowl.Controller
{
    public class ZenCartController
    {
        private Config cfg;
        private CloudData cd { get; set; }
        public ZenCartController(Config cfg)
        {
            this.cfg = cfg;
            cd = new CloudData(cfg.Store.StoreUrl, cfg.Store.ApiKey);
        }

        public List<ZCOrder> GetOrders()
        {
            var rs = cd.Query<List<ZCOrder>>(String.Format(SQL.ZenCart.ZenCart_GetOrders));
            //var rs = cd.Query<List<ZCOrder>>(String.Format("select orders.* from orders left join fishbowl_imported on orders.orders_id = fishbowl_imported.orderid"));

            if (rs.HTTPCode == 200)
            {
                if (rs.Data != null)
                {
                    var data = rs.Data;
                    foreach (var o in data)
                    {
                        o.Items = GetOrderItems(o.orders_id);
                    }
                    return data.OrderBy(k => k.orders_id).ToList();
                }
                else
                {
                    return new List<ZCOrder>();
                }
            }
            else
            {
                return new List<ZCOrder>();
            }
        }
        
        private List<ZCProduct> GetOrderItems(string orderid)
        {
            var rs = cd.Query<List<ZCProduct>>(String.Format(SQL.ZenCart.ZenCart_GetOrder_Products, orderid.ToString()));
            return rs.Data;
        }
        public bool ProductExists(string id)
        {
            var rs = cd.Query<List<ProductDataClass>>(String.Format(SQL.ZenCart.ZenCart_ProductExist, id.ToString()));
            return rs.Data.Count>0?true:false;
        }
        public List<ProductDataClass> GetAllZenProductsInfo()
        {
            return cd.Query<List<ProductDataClass>>("select products_model from products").Data;
        }

        public bool CreateProducts(String ProductsCSV)
        {
            String uploadFileName = AppDomain.CurrentDomain.BaseDirectory + "uploadFile.csv";
            File.WriteAllText(uploadFileName, ProductsCSV);
            string urlAddress = "http://orders.massbevalliance.com/fishbowl-product-import.php";
            var client = new RestClient(urlAddress);

            var request = new RestRequest(Method.POST);
            request.AddFile("file", uploadFileName);
            try
            {
                var response = client.Execute(request);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }


        }
        public bool UpdateZC2FBDownloaded(Int32 orderid, String soNum)
        {
            var ret = cd.Execute(String.Format(SQL.ZenCart.ZenCart_ZC2FBDownloaded, orderid, soNum.ToString()));
            return ret.Executed;
        }
    }
}

