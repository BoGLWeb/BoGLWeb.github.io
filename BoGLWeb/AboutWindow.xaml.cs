using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AVL_Prototype_1
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            Version? v = Assembly.GetExecutingAssembly().GetName().Version;
            if (v != null)
            {
                versionString.Text = string.Format("{0}.{1}.{2}", v.Major, v.Minor, v.Build);
                if (v.Revision != 0)
                    revisionString.Text = string.Format("-{0}", v.Revision);
                else
                    revisionString.Text = "";
            }
        }
    }
}
