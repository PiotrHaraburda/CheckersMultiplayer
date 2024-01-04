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

namespace CheckersMultiplayer
{
    /// <summary>
    /// Logika interakcji dla klasy MainApp.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CRUD_Service crud = new CRUD_Service();
        string accountName;
        string accountLogin;
        int accountAge;
        string accountEmail;
        bool accountInGame;
        bool accountOnline;
        
        string gameRoomName;

        public MainWindow(string accountName, string accountLogin, int accountAge, string accountEmail, bool accountInGame, bool accountOnline)
        {
            InitializeComponent();
            this.accountName = accountName;
            this.accountLogin = accountLogin;
            this.accountAge = accountAge;
            this.accountEmail = accountEmail;
            this.accountInGame = accountInGame;
            this.accountOnline = accountOnline;
        }

        private void multiplayerButton_Click(object sender, RoutedEventArgs e)
        {
            mainMenuPanel.Visibility = Visibility.Collapsed;
            multiplayerPanel.Visibility = Visibility.Visible;
        }

        private void quitMultiplayerButton_Click(object sender, RoutedEventArgs e)
        {
            multiplayerPanel.Visibility = Visibility.Collapsed;
            mainMenuPanel.Visibility = Visibility.Visible;
        }

        private void logoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (crud.LogoutUser() == null)
            {
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        private void joinLobbyButton_Click(object sender, RoutedEventArgs e)
        {
            gameRoomsGrid.Visibility = Visibility.Visible;
            multiplayerPanel.Visibility= Visibility.Collapsed;

            var gameRooms = crud.LoadGameRooms();
            if (gameRooms == null)
                return;

            foreach (var item in gameRooms)
            {
                Console.WriteLine($"Room: {item.Key}");
                gameRoomsListBox.Items.Add(item.Value.roomName);

                foreach (var boardRow in item.Value.board)
                {
                    Console.Write("Board Values: ");

                    foreach (var value in boardRow)
                    {
                        Console.Write($"{value} ");
                    }

                    Console.WriteLine();
                }
            }
        }

        private void createLobbyButton_Click(object sender, RoutedEventArgs e)
        {
            multiplayerPanel.Visibility = Visibility.Collapsed;
            createLobbyGrid.Visibility = Visibility.Visible;
            CRUDgame_rooms game_room=crud.CreateGameRoom(accountLogin);
            roomNameTextBox.Text = game_room.roomName;
            roomPasswordTextBox.Text = game_room.password;
        }

        private void quitLobbyCreation_Click(object sender, RoutedEventArgs e)
        {
            multiplayerPanel.Visibility = Visibility.Visible;
            createLobbyGrid.Visibility = Visibility.Collapsed;
            crud.DeleteGameRoom(gameRoomName);
        }

        private void saveRoomInfoButton_Click(object sender, RoutedEventArgs e)
        {
            crud.UpdateGameRoom(accountLogin, roomNameTextBox.Text, roomPasswordTextBox.Text);
        }

        private void quitGameRoomsButton_Click(object sender, RoutedEventArgs e)
        {
            gameRoomsGrid.Visibility = Visibility.Collapsed;
            multiplayerPanel.Visibility = Visibility.Visible;
        }

        private void joinGameRoomButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine(gameRoomsListBox.SelectedItem);
        }
    }
}
