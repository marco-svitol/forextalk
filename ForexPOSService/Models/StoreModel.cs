using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForexPOSService
{
    public class QueueModel
    {
        public int QId { get; set; }
        public string IDU { get; set; }
        public ForexTransactionModel Transaction { get; set; }
    }

    public class ConfigModel
    {
        public string param { get; set; }
        public string value { get; set; }
    }

    public class ForexTransactionModel
    {
        public int oid { get; set; }
        public int account_year { get; set; }
        public int forex_type { get; set; }
        public int forex_oid { get; set; }
        public decimal foreign_amount { get; set; }
        public double exchange_rate { get; set; }
        public decimal national_amount { get; set; }
        public DateTime journal_date_time { get; set; }
        public string userID { get; set; }
        public int isPOStransaction { get; set; }
    }

}
