using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Secrecy
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ListChooser : Window
    {

        public ListChooser(List<string> list)
        {
            InitializeComponent();
            this.ListElement.ItemsSource = list;
            this.ListElement.Focus();
        }

        private void onKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    this.Close();
                    return;
                case Key.Escape:
                    ListElement.SelectedItem = null;
                    this.Close();
                    return;
            }
        }
    }
}
