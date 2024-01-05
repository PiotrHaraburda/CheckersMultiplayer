﻿using FireSharp.Response;
using System;
using System.Collections.Generic;
using System.Data.Common;
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
        CRUD_Connection conn = new CRUD_Connection();
        string accountName;
        string accountLogin;
        int accountAge;
        string accountEmail;
        bool accountInGame;
        bool accountOnline;

        EventStreamResponse response;

        CRUDgame_rooms currentGame=new CRUDgame_rooms();

        Image selectedPawn;

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
            gameRoomsListBox.Items.Clear();
            gameRoomsGrid.Visibility = Visibility.Visible;
            multiplayerPanel.Visibility= Visibility.Collapsed;

            var gameRooms = crud.LoadGameRooms();
            if (gameRooms == null)
                return;

            foreach (var item in gameRooms)
            {
                //Console.WriteLine($"Room: {item.Value.roomName}");
                gameRoomsListBox.Items.Add(item.Value.roomName);

                foreach (var boardRow in item.Value.board)
                {
                    //Console.Write("Board Values: ");

                    foreach (var value in boardRow)
                    {
                        //Console.Write($"{value} ");
                    }

                    //Console.WriteLine();
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
            currentGame.host = accountLogin;

            OpponentListenerAsync(accountLogin);
        }

        private void quitLobbyCreation_Click(object sender, RoutedEventArgs e)
        {
            multiplayerPanel.Visibility = Visibility.Visible;
            createLobbyGrid.Visibility = Visibility.Collapsed;
            crud.DeleteGameRoom(accountLogin);

            response.Dispose();
        }

        private void saveRoomInfoButton_Click(object sender, RoutedEventArgs e)
        {
            crud.UpdateGameRoom(accountLogin, roomPasswordTextBox.Text, roomNameTextBox.Text, opponentNameLabel.Content.ToString(), false);
        }

        private void quitGameRoomsButton_Click(object sender, RoutedEventArgs e)
        {
            gameRoomsGrid.Visibility = Visibility.Collapsed;
            multiplayerPanel.Visibility = Visibility.Visible;
        }

        private void joinGameRoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (gameRoomsListBox.SelectedItem != null)
            {
                roomPasswordGrid.Visibility = Visibility.Visible;
                gameRoomsGrid.Visibility = Visibility.Collapsed;
            }
        }

        private void quitRoomPasswordGridButton_Click(object sender, RoutedEventArgs e)
        {
            roomPasswordGrid.Visibility = Visibility.Collapsed;
            gameRoomsGrid.Visibility=Visibility.Visible;
        }

        private void checkGameRoomPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            var gameRooms = crud.LoadGameRooms();
            if (gameRooms == null)
                return;

            foreach (var item in gameRooms)
            {
                if (item.Value.roomName.Equals(gameRoomsListBox.SelectedItem.ToString()))
                {
                    if (item.Value.password.Equals(enterRoomPasswordTextBox.Text))
                    {
                        if(item.Value.whitePawns.Equals(""))
                        {
                            Console.WriteLine("Valid password");
                            crud.UpdateGameRoomOpponent(item.Value.blackPawns, item.Value.password, item.Value.roomName, accountLogin);
                            roomPasswordGrid.Visibility = Visibility.Collapsed;
                            createLobbyGrid.Visibility = Visibility.Visible;
                            roomNameTextBox.Text = item.Value.roomName;
                            roomPasswordTextBox.Text = item.Value.password;
                            roomNameTextBox.IsEnabled = false;
                            roomPasswordTextBox.IsEnabled = false;
                            currentGame.host=item.Value.host;


                            opponentNameLabel.Content=item.Value.blackPawns;
                            waitingForOpponentsLabel.Content = "Waiting for room host to start the game....";
                            saveRoomInfoButton.IsEnabled = false;
                            startGameButton.IsEnabled = false;

                            GameStartedListenerAsync(item.Value.host);
                        }
                        else
                        {
                            Console.WriteLine("Room full");
                        }
                    }
                    else
                        Console.WriteLine("Invalid password");
                    return;
                }
            }

            Console.WriteLine("Game room not existing");
            return;
        }

        public async Task OpponentListenerAsync(string accountLogin)
        {
            response = await conn.client.OnAsync("gameRooms/" + accountLogin, (sender, args, context) =>
            {
                if (args.Path.Equals("/whitePawns"))
                {
                    Dispatcher.Invoke(new Action(() => { 
                        opponentNameLabel.Content = args.Data.ToString();
                        waitingForOpponentsLabel.Content = "Waiting for you to start the game...."; ; }));

                    Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(_ => OpponentListenerAsync(accountLogin));
                }
            });
        }

        public async Task GameStartedListenerAsync(string host)
        {
            response = await conn.client.OnAsync("gameRooms/" + host, (sender, args, context) =>
            {
                if (args.Path.Equals("/inProgress")&&args.Data.Equals("True"))
                {
                    Dispatcher.Invoke(new Action(() => {
                        createLobbyGrid.Visibility=Visibility.Collapsed;
                        gameGrid.Visibility = Visibility.Visible;
                        checkersGame();
                    }));
                }
            });
        }

        private void startGameButton_Click(object sender, RoutedEventArgs e)
        {
            crud.UpdateGameRoom(accountLogin, roomPasswordTextBox.Text, roomNameTextBox.Text, opponentNameLabel.Content.ToString(), true);
            createLobbyGrid.Visibility = Visibility.Collapsed;
            gameGrid.Visibility = Visibility.Visible;
            checkersGame();
        }

        void checkersGame()
        {
            var gameRooms = crud.LoadGameRooms();
            if (gameRooms == null)
                return;

            foreach (var item in gameRooms)
            {
                if(item.Key.Equals(currentGame.host))
                {
                    currentGame.roomName = item.Value.roomName;
                    currentGame.whitePawns = item.Value.whitePawns;
                    currentGame.blackPawns = item.Value.blackPawns;
                    currentGame.board=item.Value.board;
                }
            }

            int i = 0;
            int b = 12;
            int w = 1;

            foreach (var boardRow in currentGame.board)
            {
                Console.Write("Board Values: ");
                int j = 0;

                foreach (var value in boardRow)
                {
                    Console.Write($"{value} ");
                    if (!value.Equals("0"))
                    {
                        Image image = new Image();

                        if (value.Equals("B"))
                        {
                            image.Source = new BitmapImage(new Uri(@"/images/blackPawn.png", UriKind.Relative));
                            string imageName = $"pawnImageB_{b}";
                            image.Name = imageName;

                            if(currentGame.blackPawns.Equals(accountLogin))
                            {
                                image.MouseLeftButtonDown += ShowPawnPaths;
                                image.Cursor = Cursors.Hand;
                            }
                            b--;
                        }
                        else if (value.Equals("W"))
                        {
                            image.Source = new BitmapImage(new Uri(@"/images/whitePawn.png", UriKind.Relative));
                            string imageName = $"pawnImageW_{w}";
                            image.Name = imageName;
                            if (currentGame.whitePawns.Equals(accountLogin))
                            {
                                image.MouseLeftButtonDown += ShowPawnPaths;
                                image.Cursor = Cursors.Hand;
                            }
                            w++;
                        }

                        // Calculate row and column indices based on i and j
                        int rowIndex = i+1;
                        int columnIndex = j+1;

                        // Set the row and column indices
                        Grid.SetRow(image, rowIndex);
                        Grid.SetColumn(image, columnIndex);


                        image.Margin = new Thickness(63 + j * 46.5, 63 + i * 46.5, 0, 0);
                        image.Width = 36;
                        image.Height = 36;
                        image.HorizontalAlignment = HorizontalAlignment.Left;
                        image.VerticalAlignment = VerticalAlignment.Top;

                        gameGrid.Children.Add(image);
                    }
                    j++;
                }
                i++;
                Console.WriteLine();
            }
        }

        private void ShowPawnPaths(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image clickedImage)
            {
                Image imageToRemove = gameGrid.Children.OfType<Image>().FirstOrDefault(img => img.Name == "pawnPath_1");
                Image imageToRemove2 = gameGrid.Children.OfType<Image>().FirstOrDefault(img => img.Name == "pawnPath_2");

                if (imageToRemove != null)
                {
                    gameGrid.Children.Remove(imageToRemove);
                }

                if (imageToRemove2 != null)
                {
                    gameGrid.Children.Remove(imageToRemove2);
                }


                string imageName = clickedImage.Name;

                int targetRow = Grid.GetRow(clickedImage);
                int targetColumn = Grid.GetColumn(clickedImage);


                System.Console.WriteLine(targetRow + " " + targetColumn);

                selectedPawn = clickedImage;


                bool isPawnPresent = false;
                bool isPawnPresent2 = false;

                foreach (var child in gameGrid.Children)
                {
                    if (child is Image)
                    {
                        Image pawnImage = (Image)child;

                        // Pobierz informacje o pozycji obrazu na Grid
                        int row = Grid.GetRow(pawnImage);
                        int column = Grid.GetColumn(pawnImage);



                        if (currentGame.whitePawns.Equals(accountLogin))
                        {
                            if ((row == targetRow - 1 && column == targetColumn + 1) || (targetRow - 1 < 1) || (targetColumn + 1 > 8))
                            {
                                isPawnPresent = true;
                            }

                            if ((row == targetRow - 1 && column == targetColumn - 1) || (targetRow - 1 < 1) || (targetColumn - 1 < 1))
                            {
                                isPawnPresent2 = true;
                            }
                        }
                        else if (currentGame.blackPawns.Equals(accountLogin))
                        {
                            if ((row == targetRow + 1 && column == targetColumn + 1) || (targetRow + 1 > 8) || (targetColumn + 1 > 8))
                            {
                                isPawnPresent = true;
                            }

                            if ((row == targetRow + 1 && column == targetColumn - 1) || (targetRow + 1 > 8) || (targetColumn - 1 < 1))
                            {
                                isPawnPresent2 = true;
                            }
                        }
                    }
                }

                if (!isPawnPresent)
                {
                    Image image = new Image();
                    image.Source = new BitmapImage(new Uri(@"/images/greyPawn.png", UriKind.Relative));
                    string pathImageName = "pawnPath_1";
                    image.Name = pathImageName;

                    Grid.SetRow(image, targetRow);
                    Grid.SetColumn(image, targetColumn);

                    if (currentGame.whitePawns.Equals(accountLogin))
                    {
                        image.Margin = new Thickness(63 + (targetColumn - 1 + 1) * 46.5, 63 + (targetRow - 1 - 1) * 46.5, 0, 0);

                        Grid.SetRow(image, targetRow-1);
                        Grid.SetColumn(image, targetColumn + 1);
                    }
                    else if (currentGame.blackPawns.Equals(accountLogin))
                    {
                        image.Margin = new Thickness(63 + (targetColumn - 1 + 1) * 46.5, 63 + (targetRow - 1 + 1) * 46.5, 0, 0);

                        Grid.SetRow(image, targetRow+1);
                        Grid.SetColumn(image, targetColumn+1);
                    }

                    image.Width = 36;
                    image.Height = 36;
                    image.HorizontalAlignment = HorizontalAlignment.Left;
                    image.VerticalAlignment = VerticalAlignment.Top;
                    image.Cursor = Cursors.Hand;
                    image.MouseLeftButtonDown += MovePawn;

                    gameGrid.Children.Add(image);
                }
                
                if(!isPawnPresent2)
                {
                    Image image = new Image();
                    image.Source = new BitmapImage(new Uri(@"/images/greyPawn.png", UriKind.Relative));
                    string pathImageName = "pawnPath_2";
                    image.Name = pathImageName;


                    if (currentGame.whitePawns.Equals(accountLogin))
                    {
                        image.Margin = new Thickness(63 + (targetColumn - 1 - 1) * 46.5, 63 + (targetRow - 1 - 1) * 46.5, 0, 0);

                        Grid.SetRow(image, targetRow-1);
                        Grid.SetColumn(image, targetColumn-1);
                    }
                    else if (currentGame.blackPawns.Equals(accountLogin))
                    {
                        image.Margin = new Thickness(63 + (targetColumn - 1 - 1) * 46.5, 63 + (targetRow - 1 + 1) * 46.5, 0, 0);

                        Grid.SetRow(image, targetRow + 1);
                        Grid.SetColumn(image, targetColumn-1);
                    }

                    image.Width = 36;
                    image.Height = 36;
                    image.HorizontalAlignment = HorizontalAlignment.Left;
                    image.VerticalAlignment = VerticalAlignment.Top;
                    image.Cursor = Cursors.Hand;
                    image.MouseLeftButtonDown += MovePawn;

                    gameGrid.Children.Add(image);
                }
            }
        }

        private void MovePawn(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image clickedImage)
            {
                string imageName = clickedImage.Name;

                int targetRow = Grid.GetRow(clickedImage);
                int targetColumn = Grid.GetColumn(clickedImage);

                selectedPawn.Margin = new Thickness(63 + (targetColumn-1) * 46.5, 63 + (targetRow-1) * 46.5, 0, 0);


                Grid.SetRow(selectedPawn, targetRow);
                Grid.SetColumn(selectedPawn, targetColumn);


                Image imageToRemove = gameGrid.Children.OfType<Image>().FirstOrDefault(img => img.Name == "pawnPath_1");
                Image imageToRemove2 = gameGrid.Children.OfType<Image>().FirstOrDefault(img => img.Name == "pawnPath_2");

                if (imageToRemove != null)
                {
                    gameGrid.Children.Remove(imageToRemove);
                }

                if (imageToRemove2 != null)
                {
                    gameGrid.Children.Remove(imageToRemove2);
                }
            }
        }

    }
}
