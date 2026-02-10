using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderSystem.Models
{
    public class ErroredEventArgs : ErrorEventArgs
    {
        public ErroredEventArgs(string code, decimal price, Exception ex) : base(ex)
        {
            Code = code;
            Price = price;
        }

        public string Code { get; set; }
        public decimal Price { get; set; }
    }
}
