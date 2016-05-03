﻿using System.Management.Automation;
using VMLab.Drivers;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Cmdlet
{
    [Cmdlet(VerbsCommon.Get, "LabSharedFolder")]
    public class GetLabSharedFolder : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string VMName { get; set; }

        protected override void ProcessRecord()
        {
            var svc = ServiceDiscovery.GetInstance();
            var driver = svc.GetObject<IDriver>();
            var env = svc.GetObject<IEnvironmentDetails>();

            env.UpdateEnvironment(this);

            foreach (var f in driver.GetSharedFolders(VMName))
            {
                WriteObject(f);
            }

            base.ProcessRecord();
        }
    }
}
