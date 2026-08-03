using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechStore.Models
{
    public class PaymentModel
    {
        public Payment Payment { get; set; }
        public int? voucherShipID { get; set; }

    }
}
