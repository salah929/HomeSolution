using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Home.Shared.Enums
{
    public enum SupplierOrderStatus
    {
        Pending = 0,
        Confirmed = 1,
        Received = 2,
        Cancelled = -1
    }
}
