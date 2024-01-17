using System;
using System.Windows;
using System.Windows.Media;
using CheckersMultiplayer.scripts;

namespace CheckersMultiplayer
{
    public partial class RegisterWindow
    {
        public RegisterWindow()
        {
            InitializeComponent();

            var textBoxColorTimer = new System.Windows.Threading.DispatcherTimer();

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
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void signUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (loginTextBox.Text == "" || passwordTextBox.Password == "" || emailTextBox.Text == "" ||
                nameTextBox.Text == "") return;
            
            bool alreadyRegistered = false;
            var firebaseCrud = new FirebaseCrud();

            if(firebaseCrud.LoadPlayers()!=null)
                foreach (var item in firebaseCrud.LoadPlayers())
                {
                    if (item.Value.login == loginTextBox.Text)
                        alreadyRegistered = true;
                }

            if (alreadyRegistered == false)
            {
                bool isRegistered=firebaseCrud.RegisterUser(emailTextBox.Text, passwordTextBox.Password);

                if (isRegistered != true) return;
                
                firebaseCrud.SetData(nameTextBox.Text, loginTextBox.Text, emailTextBox.Text, int.Parse(ageComboBox.Text), false, false, 1000);
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
            else
                loginTextBox.Text = "This login is already taken!";
        }
    }
}
