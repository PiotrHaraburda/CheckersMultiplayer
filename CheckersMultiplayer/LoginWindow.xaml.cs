using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CheckersMultiplayer.scripts;

namespace CheckersMultiplayer
{
    public partial class LoginWindow
    {
        public LoginWindow()
        {
            InitializeComponent();
            Console.WriteLine(@"Login window opened");
        }

        private void signUpButton_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            registerWindow.Show();
            Close();
        }


        private void signInButton_Click(object sender, RoutedEventArgs e)
        {
            var firebaseCrud = new FirebaseCrud();
            bool loggedIn=firebaseCrud.LoginUser(emailTextBox.Text, passwordTextBox.Password);

            if (!loggedIn)
            {
                LinearGradientBrush myBrush = new LinearGradientBrush();
                myBrush.GradientStops.Add(new GradientStop(Colors.Yellow, 0.0));
                myBrush.GradientStops.Add(new GradientStop(Colors.Orange, 0.5));
                myBrush.GradientStops.Add(new GradientStop(Colors.Red, 1.0));
                emailTextBox.Style = null;
                passwordTextBox.Style = null;

                emailTextBox.BorderThickness = new Thickness(0, 0, 0, 1);
                emailTextBox.BorderBrush = myBrush;
                passwordTextBox.BorderThickness = new Thickness(0, 0, 0, 1);
                passwordTextBox.BorderBrush = myBrush;

                welcomeLabel.Content = "Invalid account data!";
                welcomeLabel.Foreground = System.Windows.Media.Brushes.Red;

                return;
            }
            
            string accountName;
            string accountLogin;
            int accountAge;
            string accountEmail;
            bool accountInGame;
            bool accountOnline;
            int accountVR;

            foreach (var item in firebaseCrud.LoadPlayers())
            {
                if (item.Value.email != emailTextBox.Text || item.Value.online) continue;
                
                firebaseCrud.UpdateData(item.Value.name, item.Value.login, item.Value.email, item.Value.age, true, item.Value.inGame, item.Value.vr);
                accountName = item.Value.name;
                accountLogin = item.Value.login;
                accountAge= item.Value.age;
                accountEmail = item.Value.email;
                accountInGame = item.Value.inGame;
                accountOnline = item.Value.online;
                accountVR = item.Value.vr;

                emailLabel.Visibility = Visibility.Hidden;
                passwordLabel.Visibility = Visibility.Hidden;
                emailTextBox.Visibility = Visibility.Hidden;
                passwordTextBox.Visibility = Visibility.Hidden;
                signInButton.Visibility = Visibility.Hidden;
                signUpButton.Visibility = Visibility.Hidden;
                welcomeLabel.Visibility = Visibility.Hidden;
                notRegisteredLabel.Visibility = Visibility.Hidden;

                signedInLabel.Visibility = Visibility.Visible;

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                timer.Start();
                timer.Tick += (sender2, args) =>
                {
                    timer.Stop();
                    var mainWindow = new MainWindow(accountName, accountLogin, accountAge, accountEmail, accountInGame, accountOnline, accountVR);
                    mainWindow.Show();
                    Close();
                };

                break;
            }
        }
    }
}
