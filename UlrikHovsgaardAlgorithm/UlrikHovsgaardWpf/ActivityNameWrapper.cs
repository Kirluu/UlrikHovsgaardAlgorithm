using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace UlrikHovsgaardWpf
{
    public class ActivityNameWrapper
    {
        //public ICommand Command { get; set; }
        public string DisplayName { get; set; }

        public ActivityNameWrapper(string name)
        {
            //Command = cmd;
            DisplayName = name;
        }
    }
}
