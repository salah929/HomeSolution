using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Home.Shared.Enums
{
    public enum CustomerOrderStatus
    {
        Pending = 0,
        Confirmed = 1,
        Shipped = 2,
        Cancelled = -1
    }
}
