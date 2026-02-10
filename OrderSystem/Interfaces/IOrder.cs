using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderSystem.Interfaces
{
    public interface IOrder : IPlaced, IErrored
    {
        void RespondToTick(string code, decimal price);
    }
}
