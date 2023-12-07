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
using System.Windows.Threading;

namespace CheckersMultiplayer
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();

            System.Windows.Threading.DispatcherTimer textBoxColorTimer = new System.Windows.Threading.DispatcherTimer();

            for (int i = 10; i < 100; i++)
                ageComboBox.Items.Add(i);

            textBoxColorTimer.Tick += textBoxColorTimer_Tick;
            textBoxColorTimer.Interval = new TimeSpan(0, 0, 1);
            textBoxColorTimer.Start();
        }

        private void textBoxColorTimer_Tick(object sender, EventArgs e)
        {
            if(loginTextBox.Text == "This login is already taken!")
                loginTextBox.Foreground = Brushes.Red;
            else
                loginTextBox.Foreground = Brushes.Black;
        }

        private void signInButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void signUpButton_Click(object sender, RoutedEventArgs e)
        {
            bool alreadyRegistered = false;
            CRUD_Service crud = new CRUD_Service();

            foreach (var item in crud.LoadData())
            {
                if (item.Value.login == loginTextBox.Text)
                    alreadyRegistered = true;
            }

            if (alreadyRegistered == false)
                crud.SetData(nameTextBox.Text, loginTextBox.Text, passwordTextBox.Password, Int32.Parse(ageComboBox.Text), false, false);
            else
                loginTextBox.Text = "This login is already taken!";
        }
    }
}
