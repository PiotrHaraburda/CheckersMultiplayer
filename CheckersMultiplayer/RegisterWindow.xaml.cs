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
using Firebase.Auth;
using Firebase.Auth.Providers;

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

            if (loginTextBox.Text!=""&&passwordTextBox.Password!=""&&emailTextBox.Text!=""&&nameTextBox.Text!="")
            {
                bool alreadyRegistered = false;
                CRUD_Service crud = new CRUD_Service();

                foreach (var item in crud.LoadPlayers())
                {
                    if (item.Value.login == loginTextBox.Text)
                        alreadyRegistered = true;
                }

                if (alreadyRegistered == false)
                {
                    bool isRegistered=crud.RegisterUser(emailTextBox.Text, passwordTextBox.Password);

                    if(isRegistered == true)
                    {
                        crud.SetData(nameTextBox.Text, loginTextBox.Text, emailTextBox.Text, Int32.Parse(ageComboBox.Text), false, false);
                        accountCreatedLabel.Visibility = Visibility.Visible;
                        nameTextBox.Visibility = Visibility.Hidden;
                        loginTextBox.Visibility = Visibility.Hidden;
                        passwordTextBox.Visibility = Visibility.Hidden;
                        emailTextBox.Visibility = Visibility.Hidden;
                        signUpButton.Visibility = Visibility.Hidden;
                        ageComboBox.Visibility = Visibility.Hidden;
                        nameLabel.Visibility = Visibility.Hidden;
                        loginLabel.Visibility = Visibility.Hidden;
                        passwordLabel.Visibility = Visibility.Hidden;
                        emailLabel.Visibility = Visibility.Hidden;
                        ageLabel.Visibility = Visibility.Hidden;
                        alreadyRegisteredLabel.Visibility = Visibility.Hidden;
                        welcomeLabel.Visibility = Visibility.Hidden;

                        signInButton.Width = 344;
                    }
                }
                else
                    loginTextBox.Text = "This login is already taken!";
            }
        }
    }
}
