using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics;

namespace Argon
{
    public sealed class EventReader
    {
        //System.Diagnostics.Eventing.Reader.EventLogReader;
        public static void Initialize()
        {
            EventLog log = new EventLog("Security");
            //Enable auditing for allowed and blocked connections
            //ExecuteCmd("auditpol.exe", "/set /subcategory:{0CCE9226-69AE-11D9-BED3-505054503030} /failure:enable /success:enable");

        }

    }
}