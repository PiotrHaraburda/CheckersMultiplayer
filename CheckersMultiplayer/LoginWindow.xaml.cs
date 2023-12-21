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
using System.Windows.Threading;

namespace CheckersMultiplayer
{
    /// <summary>
    /// Logika interakcji dla klasy LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            Console.WriteLine("Login window opened");
        }

        private void signUpButton_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow registerWindow = new RegisterWindow();
            registerWindow.Show();
            this.Close();
        }

        private void signInButton_Click(object sender, RoutedEventArgs e)
        {
            CRUD_Service crud = new CRUD_Service();
            bool loggedIn=crud.LoginUser(emailTextBox.Text, passwordTextBox.Password);

            if(loggedIn)
            {
                foreach (var item in crud.LoadData())
                {
                    if (item.Value.email == emailTextBox.Text)
                    {
                        crud.UpdateData(item.Value.name, item.Value.login, item.Value.email, item.Value.age, true, item.Value.inGame);
                        break;
                    }
                }

                emailLabel.Visibility = Visibility.Hidden;
                passwordLabel.Visibility = Visibility.Hidden;
                emailTextBox.Visibility = Visibility.Hidden;
                passwordTextBox.Visibility = Visibility.Hidden;
                signInButton.Visibility = Visibility.Hidden;
                signedInLabel.Visibility = Visibility.Visible;

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                timer.Start();
                timer.Tick += (sender2, args) =>
                {
                    timer.Stop();
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                };

            }

        }
    }
}
