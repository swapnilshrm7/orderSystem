using OrderSystem.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderSystem.Models
{
    public class OrderInput
    {
        public IOrderService OrderService { get; set; }
        public decimal PriceThreshold { get; set; }
    }
}
