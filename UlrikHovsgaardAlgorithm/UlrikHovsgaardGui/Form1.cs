using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardGui
{
    public partial class Form1 : Form
    {
        public ObservableCollection<Activity> Activities { get; private set; }


        public Form1()
        {
            InitializeComponent();
            dataAlphabet.RowHeadersWidth = 24;

            Activities = new ObservableCollection<Activity> { new Activity("A", "somenameA") };

            dataAlphabet.DataSource = Activities;

            dataAlphabet.Refresh();
        }
    }
}
