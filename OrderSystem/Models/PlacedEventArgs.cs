using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderSystem.Models
{
    public class PlacedEventArgs
    {
        public PlacedEventArgs(string code, decimal price)
        {
            Code = code;
            Price = price;
        }

        public string Code { get; set; }
        public decimal Price { get; set; }
    }
}
