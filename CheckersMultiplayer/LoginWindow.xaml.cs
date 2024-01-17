using System;
using System.Windows;
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

            if (!loggedIn) return;
            
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
