using OrderSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderSystem.Interfaces
{
    public interface IErrored
    {
        event ErroredEventHandler Errored;
    }

    public delegate void ErroredEventHandler(ErroredEventArgs e);
}
