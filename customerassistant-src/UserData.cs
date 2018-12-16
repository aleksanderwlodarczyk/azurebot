using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EchoBotWithCounter
{
    public class UserData
    {
        public string emailnormalized { get; set; }
        public int itemsbought { get; set; }
        public float aggression { get; set; }
        public string name { get; set; }
        public float designtype { get; set; }
        public float techtype { get; set; }

        public UserData()
        {

        }

        public UserData(string email)
        {
          emailnormalized = email.ToUpper();
        }

    }
}
