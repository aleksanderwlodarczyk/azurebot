using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EchoBotWithCounter
{
    public static class State
    {
        public static bool UserPromptedForEmail { get; set; }
        public static bool GotEmail { get; set; }
        public static UserData User { get; set; }
        public static bool Registered { get; set; }
        public static bool UserPromptedForName { get; set; }
        public static bool  UserReadyToSave { get; set; }
    }
}
