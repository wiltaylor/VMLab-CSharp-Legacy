
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMLab.Model
{
    public interface IVMCredential
    {
        string Username { get; }
        string Password { get; }
    }

    public class VMCredential : IVMCredential
    {
        public string Username { get; }
        public string Password { get; }

        public VMCredential(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}
