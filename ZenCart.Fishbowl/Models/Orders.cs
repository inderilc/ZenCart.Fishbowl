using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenCart.Fishbowl.Models
{
    public class ZCProduct
    {
        public String orders_products_id { get; set; }
        public String orders_id { get; set; }
        public String products_id { get; set; }
        public String products_model { get; set; }
        public String products_name { get; set; }
        public String products_price { get; set; }
        public String final_price { get; set; }
        public String products_tax { get; set; }
        public String products_quantity { get; set; }
        public String onetime_charges { get; set; }
        public String products_priced_by_attribute { get; set; }
        public String product_is_free { get; set; }
        public String products_discount_type { get; set; }
        public String products_discount_type_from { get; set; }
        public String products_prid { get; set; }
    }

    public class ZCOrder
    {
        public String orders_id { get; set; }
        public String customers_id { get; set; }
        public String customers_name { get; set; }
        public String customers_company { get; set; }
        public String customers_street_address { get; set; }
        public String customers_suburb { get; set; }
        public String customers_city { get; set; }
        public String customers_postcode { get; set; }
        public String customers_state { get; set; }
        public String customers_country { get; set; }
        public String customers_telephone { get; set; }
        public String customers_email_address { get; set; }
        public String customers_address_format_id { get; set; }
        public String delivery_name { get; set; }
        public String delivery_company { get; set; }
        public String delivery_street_address { get; set; }
        public String delivery_suburb { get; set; }
        public String delivery_city { get; set; }
        public String delivery_postcode { get; set; }
        public String delivery_state { get; set; }
        public String delivery_country { get; set; }
        public String delivery_address_format_id { get; set; }
        public String billing_name { get; set; }
        public String billing_company { get; set; }
        public String billing_street_address { get; set; }
        public String billing_suburb { get; set; }
        public String billing_city { get; set; }
        public String billing_postcode { get; set; }
        public String billing_state { get; set; }
        public String billing_country { get; set; }
        public String billing_address_format_id { get; set; }
        public String payment_method { get; set; }
        public String payment_module_code { get; set; }
        public String shipping_method { get; set; }
        public String shipping_module_code { get; set; }
        public String coupon_code { get; set; }
        public String cc_type { get; set; }
        public String cc_owner { get; set; }
        public String cc_number { get; set; }
        public String cc_expires { get; set; }
        public String cc_cvv { get; set; }
        public String last_modified { get; set; }
        public String date_purchased { get; set; }
        public String orders_status { get; set; }
        public String orders_date_finished { get; set; }
        public String currency { get; set; }
        public String currency_value { get; set; }
        public String order_total { get; set; }
        public String order_tax { get; set; }
        public String paypal_ipn_id { get; set; }
        public String ip_address { get; set; }
        public List<ZCProduct> Items { get; set; }
    }
    public class ZCFBOrder
    {
        public String CustomerName { get; set; }
        public String CustomerID { get; set; }
        public ZCOrder Order { get; set; }
        public FishbowlSDK.SalesOrder FbOrder { get; set; }
    }
    public class Stateconst
    {
        public System.Int32 ID { get; set; }
        public System.Int32 COUNTRYCONSTID { get; set; }
        public System.String NAME { get; set; }
        public System.String CODE { get; set; }
    }
    public class Countryconst
    {
        public System.Int32 ID { get; set; }
        public System.String ABBREVIATION { get; set; }
        public System.String NAME { get; set; }
        public System.Int16 UPS { get; set; }
    }
    public class CountryAndState
    {
        public Countryconst Country { get; set; }
        public Stateconst State { get; set; }
    }

}
