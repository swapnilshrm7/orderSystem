using OrderSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderSystem.Interfaces
{
    public interface IPlaced
    {
        event PlacedEventHandler Placed;
    }

    public delegate void PlacedEventHandler(PlacedEventArgs e);
}
