using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AVL_Prototype_1
{
    /// <summary>
    /// Interaction logic for ElementTemplate.xaml
    /// </summary>
    public partial class ElementTemplate : UserControl
    {
        public ElementTemplate()
        {
            InitializeComponent();
        }

        private void ElementTemplate_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //Image image = (Image)((Border)((DockPanel)sender).Children[0]).Child;   //ez
            //DataObject data = new DataObject(typeof(ImageSource), image.Source);
            ElementTemplate et = (ElementTemplate)((DockPanel)sender).Parent;
            String data = et.Name;
            DragDrop.DoDragDrop(et, data, DragDropEffects.Copy);
        }

        private void ElementTemplate_MouseEnter(object sender, MouseEventArgs e)
        {
            Border border = (Border)((DockPanel)sender).Children[0];
            border.BorderBrush = new SolidColorBrush(Colors.Black);
        }

        private void ElementTemplate_MouseLeave(object sender, MouseEventArgs e)
        {
            Border border = (Border)((DockPanel)sender).Children[0];
            border.BorderBrush = new SolidColorBrush(Colors.Transparent);
        }
    }
}
