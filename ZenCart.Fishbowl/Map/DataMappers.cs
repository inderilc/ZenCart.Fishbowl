using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FishbowlSDK;
using ZenCart.Fishbowl.Configuration;
using ZenCart.Fishbowl.Models;
using ZenCart.Fishbowl.Extensions;

namespace ZenCart.Fishbowl.Map
{
    public static class DataMappers
    {
        public static FishbowlSDK.SalesOrder MapSalesOrder(Config cfg, ZCFBOrder ord, String OrderStatus)
        {
            SalesOrder salesOrder = new SalesOrder();

            var o = ord.Order;

            salesOrder.CustomerName = ord.CustomerName; //done
            salesOrder.CustomerPO = o.orders_id.ToString(); //done
        
            salesOrder.TotalIncludesTax = true;
            salesOrder.TotalIncludesTaxSpecified = false;

            salesOrder.Items = MapItems(ord.Order.Items).ToList();

            /*
         
            public String customers_email_address { get; set; }
            public String customers_address_format_id { get; set; }
            
            public String payment_method { get; set; }
            public String payment_module_code { get; set; }

            public String shipping_method { get; set; } --done
            public String shipping_module_code { get; set; }
            
            public String coupon_code { get; set; }
            public String cc_type { get; set; }
            public String cc_owner { get; set; }
            public String cc_number { get; set; }
            public String cc_expires { get; set; }
            public String cc_cvv { get; set; }
            public String last_modified { get; set; }
            public String date_purchased { get; set; }

            public String orders_status { get; set; }  --done
            public String orders_date_finished { get; set; }
            public String currency { get; set; }
            public String currency_value { get; set; }

            public String order_total { get; set; }
            public String order_tax { get; set; }

            public String paypal_ipn_id { get; set; }
            public String ip_address { get; set; }

            public String delivery_name { get; set; }
            public String delivery_company { get; set; }
            public String delivery_street_address { get; set; }
            public String delivery_suburb { get; set; }
            public String delivery_city { get; set; }
            public String delivery_postcode { get; set; }
            public String delivery_state { get; set; }
            public String delivery_country { get; set; }
            public String delivery_address_format_id { get; set; } 
           */

            salesOrder.Status = o.orders_status;

            salesOrder.CustomerContact = o.customers_company;
            salesOrder.Carrier = MapCarrier(cfg, o.shipping_method);

            salesOrder.CustomFields = MapCustomFields(ord);

            salesOrder.Ship = new ShipType
            {
                AddressField = o.delivery_street_address,
                City = o.delivery_city,
                Country = o.delivery_country,
                State = o.delivery_state,
                Zip = o.delivery_postcode,
                Name = o.delivery_name,

            };

            salesOrder.BillTo = new BillType
            {
                AddressField = o.billing_street_address,
                City = o.billing_city,
                Country = o.billing_country,
                State = o.billing_state,
                Zip = o.billing_postcode,
                Name = o.billing_name
            };

            return salesOrder;
        }

        private static List<CustomField> MapCustomFields(ZCFBOrder ord)
        {
            List<CustomField> ret = new List<CustomField>();
            ret.Add(new CustomField()
            {
                Name = "Zen Cart Order ID",
                Type = "CFT_LONG_TEXT",
                Info = ord.Order.orders_id
            });

            return ret;
        }

        private static IEnumerable<SalesOrderItem> MapItems(List<ZCProduct> items)
        {
            return items.Select(i => MapSOItem(i));
        }


        private static SalesOrderItem MapSOItem(ZCProduct p)
        {

            return new SalesOrderItem
            {
                Quantity = (double)(Convert.ToDouble(p.products_quantity)),
                ProductNumber = p.products_model.Trim(),
                ProductPrice = Convert.ToDouble(p.final_price),
                TotalPrice = Convert.ToDouble(p.final_price) * Convert.ToDouble(p.products_quantity),
                SOID = "-1",
                ID = "-1",
                ItemType = "10",
                Status = "10",
                ProductPriceSpecified = true,
                Taxable = false,
                TaxRateSpecified = false,
                UOMCode = "ea"
            };
        }


        private static string MapCustomerName(ZCOrder o)
        {
            return StringExtensions.Coalesce(
                o?.customers_name
                ).Trim();
        }

        public static List<ZCFBOrder> MapNewOrders(List<ZCOrder> orders)
        {
            var ret = new List<ZCFBOrder>();

            foreach (var o in orders)
            {
                var x = new ZCFBOrder();
                x.Order = o;
                x.CustomerName = MapCustomerName(o);
                ret.Add(x);
            }
            return ret;
        }
        private static string MapCarrier(Config cfg, string shipping)
        {
            var dict = cfg.Store.OrderSettings.CarrierSearchNames;
            foreach (var i in dict)
            {
                bool found = shipping.ToUpper().Contains(i.Key.ToUpper()) && shipping.ToUpper().Equals(i.Value.ToUpper());
                if (found)
                {
                    return i.Value;
                }
            }
            return cfg.Store.OrderSettings.DefaultCarrier;
        }
        public static Customer MapCustomer(Config cfg, ZCOrder o, String customerName, CountryAndState csa)
        {

            Customer customer = new Customer();
            customer.CustomerID = "-1";
            customer.Status = "Normal";

            customer.TaxRate = null;
            customer.Name = customerName;
          
            customer.CreditLimit = "0";
            customer.TaxExempt = false;
            customer.TaxExemptNumber = null;
            customer.TaxExemptSpecified = true;
            customer.ActiveFlag = true;
            customer.ActiveFlagSpecified = true;
            customer.AccountingID = null;

            customer.JobDepth = "1";
            Address address = new Address();
            address.Street = o.customers_street_address;

            address.Name = o.customers_name;

            address.Attn = address.Name;

            address.Residential = false;
            address.ResidentialSpecified = false;

            address.State.Code = csa.State.CODE;
            address.State.Name = csa.State.NAME;

            address.Country.Name = csa.Country.NAME;
            address.Country.Code = csa.Country.ABBREVIATION;
            address.Country.ID = csa.Country.ID.ToString();

            address.Zip = o.customers_postcode;
            address.Type = "Main Office";
            address.TempAccount = null;
            address.Default = true;
            address.DefaultSpecified = true;

            address.AddressInformationList = new List<AddressInformation>()
        {
            new AddressInformation()
            {
                Name = "Email",
                Type = "Email",
                Default = true,
                DefaultSpecified = true,
                Data = o.customers_email_address.ToString()
            }
        };

            customer.Addresses.Add(address);
            return customer;
        }
    }
}