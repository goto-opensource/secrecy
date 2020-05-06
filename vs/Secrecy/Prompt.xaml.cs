using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Secrecy
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class Prompt : Window
    {
        public Prompt()
        {
            InitializeComponent();
            this.ResponseTextBox.Focus();
        }

        public string ResponseText
        {
            get { return ResponseTextBox.Text; }
            set { ResponseTextBox.Text = value; }
        }

        private void onKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    this.Close();
                    return;
                case Key.Escape:
                    ResponseText = null;
                    this.Close();
                    return;
            }
        }
    }
}
