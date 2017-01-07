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
using System.Windows.Shapes;

namespace MashAttack
{
    /// <summary>
    /// Interaction logic for PlayerForm.xaml
    /// </summary>
    public partial class PlayerForm : Window
    {
        AttackWindow mainForm;

        public PlayerForm()
        {
            InitializeComponent();
        }

        public PlayerForm(AttackWindow window)
        {
            this.mainForm = window;
            InitializeComponent();
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            if(nameBox.Text != string.Empty)
            {
                mainForm.AddPlayer(nameBox.Text);
                nameBox.Text = "";
            }
        }
    }
}
