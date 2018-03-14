using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace WinTest
{


    public partial class Company
    {
        public int Account_Code { get; set; }
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }
    }


    public partial class Remise
    {
        public int Code_Article { get; set; }
        public string Code_CSP { get; set; }
        public float Pourcent { get; set; }
        public DateTime Date_Application { get; set; }
        public DateTime Date_Fin { get; set; }
    }
    
}
