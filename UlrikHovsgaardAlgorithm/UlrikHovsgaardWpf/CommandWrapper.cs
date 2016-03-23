using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace UlrikHovsgaardWpf
{
    public class CommandWrapper
    {
        public ICommand Command { get; set; }
        public string DisplayName { get; set; }

        public CommandWrapper(ICommand cmd, string name)
        {
            Command = cmd;
            DisplayName = name;
        }
    }
}
