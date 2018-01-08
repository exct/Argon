using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ArgonSvc
{
    public partial class ArgonSvc : ServiceBase
    {
        public ArgonSvc()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //Controller.Initialize();
        }

        protected override void OnStop()
        {
        }
    }
}
