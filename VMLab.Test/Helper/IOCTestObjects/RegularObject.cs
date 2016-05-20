using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMLab.Test.Helper.IOCTestObjects
{
    public interface IRegularObject
    {
        string Value1 { get; set; }
        int Value2 { get; set; }
    }

    public class RegularObject : IRegularObject
    {
        public string Value1 { get; set; }
        public int Value2 { get; set; }
    }

    public class RegularObject2 : IRegularObject
    {
        public string Value1 { get; set; }
        public int Value2 { get; set; }
    }

}
