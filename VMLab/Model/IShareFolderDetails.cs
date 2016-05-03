using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMLab.Model
{
    public interface IShareFolderDetails
    {
        string Name { get; }
        string GuestPath { get; }
        string HostPath { get; }
    }

    public class ShareFolderDetails : IShareFolderDetails
    {
        public ShareFolderDetails(string name, string guestpath, string hostpath)
        {
            Name = name;
            GuestPath = guestpath;
            HostPath = hostpath;
        }

        public string Name { get; }
        public string GuestPath { get; }
        public string HostPath { get; }
    }
}
