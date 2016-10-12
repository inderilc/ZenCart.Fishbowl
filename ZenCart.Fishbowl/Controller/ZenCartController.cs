using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudDataClient;
using ZenCart.Fishbowl.Configuration;
using ZenCart.Fishbowl.Models;

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
        public bool CreateProduct(ProductDataFB product)
        {
            //string query = String.Format("INSERT INTO `massbeva_orders`.`products` (`products_id`,`products_type`,`products_quantity`,`products_model`,`products_price`,`products_virtual`,`products_date_added`,`products_weight`, `products_status`, `products_tax_class_id`, `products_ordered`, `products_quantity_order_min`, `products_quantity_order_units, `products_priced_by_attribute`, `product_is_free`, `product_is_call`, `products_quantity_mixed`, `product_is_always_free_shipping`, `products_qty_box_status`, `products_quantity_order_max`, `products_sort_order`, `products_discount_type`, `products_discount_type_from`, `products_price_sorter`, `master_categories_id`, `products_mixed_discount_quantity`, `metatags_title_status`, `metatags_products_name_status`, `metatags_model_status`, `metatags_price_status`,`metatags_title_tagline_status`) VALUES (NULL, 1, 0, '{0}',{1},0,NULL,{3},{4},{5},0,1,1,0,0,0,0,0,1,0,0,0,0,{6},0,1,0,0,0,0,0)", product.NUM, product.PRICE, product.DATECREATED, product.WEIGHT, product.ACTIVEFLAG, product.QBCLASSID, product.PRICE);
            string query = String.Format("INSERT INTO `massbeva_orders`.`products` (`products_id`, `products_model`,`products_price`,`products_weight`, `products_price_sorter`, `products_image`,`products_date_added`,`products_last_modified`) VALUES (NULL, '{0}', {1}, {2}, {3}, 'no_picture.gif',{4},{4})", product.NUM,product.PRICE, product.WEIGHT, product.PRICE, DateTime.Now.ToString("yyyy-mm-dd HH:mm:ss"));
            var rs = cd.Execute(query);            
            return String.IsNullOrEmpty(rs.RawContent.ToString()) ? true:false;
        }
        public bool UpdateZC2FBDownloaded(Int32 orderid, String soNum)
        {
            var ret = cd.Execute(String.Format(SQL.ZenCart.ZenCart_ZC2FBDownloaded, orderid, soNum.ToString()));
            return ret.Executed;
        }
    }
}

