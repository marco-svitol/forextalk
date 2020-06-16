using System;
using System.Collections.Generic;

namespace ForexPOSService
{
    public enum TransactionType
    {
        Insert,
        Delete,
        Undelete
    }

    public class ApiAuth
    {
        public string POSCode { get; set; }
        public string APIKey { get; set; }
    }

    public class TransactionsAdd
    {
        public ApiAuth apiAuth { get; set; }
        public IList<QueueModel> transactions { get; set; }
    }

    public class TransactionsAddReply 
    {
        public int QId { get; set; }
        public bool added { get; set; }
    }

    public class ActionsGetReply
    {    
        public int POSActionQueueId {get; set;}
        public string action {get; set;}
        public string POSActionParams {get; set;}
    }

    public class ActionSendToPosParams
    {
        public int currency {get; set;}
        public decimal amount {get;set ;}
        public double exchangerate {get;set;}
    }
        
    public class ActionAck
    {    
        public int POSActionQueueId {get; set;}
    }
}

