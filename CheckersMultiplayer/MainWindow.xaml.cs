using FireSharp.Response;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

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

        EventStreamResponse response1;
        EventStreamResponse response2;
        EventStreamResponse response3;

        CRUDgame_rooms currentGame = new CRUDgame_rooms();

        Image selectedPawn;
        string selectedPawnName;

        private DispatcherTimer timer;

        bool boardUpdated = false;
        string opponent;

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
            multiplayerPanel.Visibility = Visibility.Collapsed;

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
            CRUDgame_rooms game_room = crud.CreateGameRoom(accountLogin);
            roomNameTextBox.Text = game_room.roomName;
            roomPasswordTextBox.Text = game_room.password;
            currentGame.host = accountLogin;

            OpponentListenerAsync(accountLogin);
        }

        private void quitLobbyCreation_Click(object sender, RoutedEventArgs e)
        {
            multiplayerPanel.Visibility = Visibility.Visible;
            createLobbyGrid.Visibility = Visibility.Collapsed;
            if (accountLogin.Equals(currentGame.host))
            {
                crud.DeleteGameRoom(accountLogin);
                response1.Dispose();
            }
            else
                crud.ClearGameRoomWhitePawns(currentGame.host, currentGame.blackPawns, currentGame.password, currentGame.roomName, currentGame.board, currentGame.turn);

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
            gameRoomsGrid.Visibility = Visibility.Visible;
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
                        if (item.Value.whitePawns.Equals(""))
                        {
                            Console.WriteLine("Valid password");
                            crud.UpdateGameRoomOpponent(item.Value.blackPawns, item.Value.password, item.Value.roomName, accountLogin);
                            roomPasswordGrid.Visibility = Visibility.Collapsed;
                            createLobbyGrid.Visibility = Visibility.Visible;
                            roomNameTextBox.Text = item.Value.roomName;
                            roomPasswordTextBox.Text = item.Value.password;
                            roomNameTextBox.IsEnabled = false;
                            roomPasswordTextBox.IsEnabled = false;
                            currentGame.host = item.Value.host;
                            currentGame.password = item.Value.password;
                            currentGame.roomName = item.Value.roomName;
                            currentGame.whitePawns = item.Value.whitePawns;
                            currentGame.blackPawns = item.Value.blackPawns;
                            currentGame.board = item.Value.board;
                            currentGame.turn = item.Value.turn;


                            opponentNameLabel.Content = item.Value.blackPawns;
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

            //pobranie danych o pokoju gier

            foreach (var item in gameRooms)
            {
                if (item.Key.Equals(currentGame.host))
                {
                    currentGame.host = item.Value.host;
                    currentGame.password = item.Value.password;
                    currentGame.roomName = item.Value.roomName;
                    currentGame.whitePawns = item.Value.whitePawns;
                    currentGame.blackPawns = item.Value.blackPawns;
                    currentGame.board = item.Value.board;
                    currentGame.turn = item.Value.turn;
                }
            }


            if (currentGame.whitePawns.Equals(accountLogin))
                opponentNameLabel2.Content = currentGame.blackPawns;
            else
                opponentNameLabel2.Content = currentGame.whitePawns;


            //uruchomienie zegara sprawdzającego czy polozenie pionkow w bazie zostalo zmienione

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            timer.Start();

            //narysowanie pionkow na podstawie aktualnych danych z bazy

            drawPawns();
            gameLabelsUpdate();

        }

        private void ShowPawnPaths(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image clickedImage && currentGame.turn.Equals(accountLogin))
            {
                //usuniecie wszystkich widocznych sciezek

                var imagesToRemove = gameGrid.Children.OfType<Image>().Where(img => img.Name != null && img.Name.StartsWith("pawnPath_"));

                foreach (var imageToRemove in imagesToRemove.ToList())
                {
                    gameGrid.Children.Remove(imageToRemove);
                }


                //pobranie polozenia pionka ktory zostal klikniety

                string imageName = clickedImage.Name;

                int targetRow = Grid.GetRow(clickedImage);
                int targetColumn = Grid.GetColumn(clickedImage);


                //System.Console.WriteLine(targetRow + " " + targetColumn);

                selectedPawn = clickedImage;
                selectedPawnName = selectedPawn.Name;


                bool leftPathAvailable = true;
                bool rightPathAvailable = true;

                bool leftCaptureAvailable = true;
                bool rightCaptureAvailable = true;

                bool leftPathBlocked = false;
                bool rightPathBlocked = false;

                bool leftCaptureBlocked = false;
                bool rightCaptureBlocked = false;

                bool[] kingLeftUpPathAvailable = new bool[6] { true, true, true, true, true, true };
                bool[] kingRightUpPathAvailable = new bool[6] { true, true, true, true, true, true };

                bool[] kingLeftDownPathAvailable = new bool[6] { true, true, true, true, true, true };
                bool[] kingRightDownPathAvailable = new bool[6] { true, true, true, true, true, true };

                int kingLeftUpCapture = 0;
                int kingRightUpCapture = 0;
                int kingLeftDownCapture = 0;
                int kingRightDownCapture = 0;

                int kingLeftUpAlly = 0;
                int kingRightUpAlly = 0;
                int kingLeftDownAlly = 0;
                int kingRightDownAlly = 0;

                if (currentGame.whitePawns.Equals(accountLogin))
                {
                    if ((targetRow - 1 < 1) || (targetColumn + 1 > 8))
                    {
                        leftPathBlocked = true;
                    }
                    if ((targetRow - 1 < 1) || (targetColumn - 1 < 1))
                    {
                        rightPathBlocked = true;
                    }

                    if ((targetRow - 2 < 1) || (targetColumn + 2 > 8))
                    {
                        leftCaptureBlocked = true;
                    }
                    if ((targetRow - 2 < 1) || (targetColumn - 2 < 1))
                    {
                        rightCaptureBlocked = true;
                    }

                }
                else if (currentGame.blackPawns.Equals(accountLogin))
                {

                    if ((targetRow + 1 > 8) || (targetColumn + 1 > 8))
                    {
                        leftPathBlocked = true;

                    }
                    if ((targetRow + 1 > 8) || (targetColumn - 1 < 1))
                    {
                        rightPathBlocked = true;
                    }

                    if ((targetRow + 2 > 8) || (targetColumn + 2 > 8))
                    {
                        leftCaptureBlocked = true;

                    }
                    if ((targetRow + 2 > 8) || (targetColumn - 2 < 1))
                    {
                        rightCaptureBlocked = true;
                    }

                }


                if (clickedImage.Name.StartsWith("pawnImageK"))
                {
                    for (int i = 1; i <= 6; i++)
                    {
                        if ((targetRow - i < 1) || (targetColumn - i < 1))
                            kingLeftUpPathAvailable[i - 1] = false;
                        if ((targetRow - i < 1) || (targetColumn + i > 8))
                            kingRightUpPathAvailable[i - 1] = false;
                        if ((targetRow + i > 8) || (targetColumn - i < 1))
                            kingLeftDownPathAvailable[i - 1] = false;
                        if ((targetRow + i > 8) || (targetColumn + i > 8))
                            kingRightDownPathAvailable[i - 1] = false;
                    }
                }

                //dla kazdego pionka narysowanego na planszy

                foreach (var child in gameGrid.Children)
                {
                    if (child is Image)
                    {
                        Image pawnImage = (Image)child;
                        string pawnImageName = pawnImage.Name;

                        //pobranie polozenia pionka
                        int row = Grid.GetRow(pawnImage);
                        int column = Grid.GetColumn(pawnImage);

                        // jezeli sterujemy pionkami bialymi sprawdzamy czy jakis pionek znajduje sie nad kliknietym pionkiem po lewej i prawej
                        // jesli natomiast sterujemy czarnymi to patrzymy pod spodem

                        if (currentGame.whitePawns.Equals(accountLogin))
                        {
                            if ((row == targetRow - 1 && column == targetColumn + 1))
                            {
                                leftPathAvailable = false;
                            }
                            if ((row == targetRow - 1 && column == targetColumn - 1))
                            {
                                rightPathAvailable = false;
                            }

                            if ((row == targetRow - 2 && column == targetColumn + 2))
                            {
                                leftCaptureAvailable = false;
                            }
                            if ((row == targetRow - 2 && column == targetColumn - 2))
                            {
                                rightCaptureAvailable = false;
                            }
                        }
                        else if (currentGame.blackPawns.Equals(accountLogin))
                        {
                            if ((row == targetRow + 1 && column == targetColumn + 1))
                            {
                                leftPathAvailable = false;
                            }
                            if ((row == targetRow + 1 && column == targetColumn - 1))
                            {
                                rightPathAvailable = false;
                            }

                            if ((row == targetRow + 2 && column == targetColumn + 2))
                            {
                                leftCaptureAvailable = false;
                            }
                            if ((row == targetRow + 2 && column == targetColumn - 2))
                            {
                                rightCaptureAvailable = false;
                            }

                        }

                        if (clickedImage.Name.StartsWith("pawnImageKW"))
                        {
                            for (int i = 1; i <= 6; i++)
                            {
                                if (row == targetRow - i && column == targetColumn - i)
                                {
                                    kingLeftUpPathAvailable[i - 1] = false;
                                    if ((i < kingLeftUpCapture || kingLeftUpCapture == 0) && pawnImageName.StartsWith("pawnImageB"))
                                        kingLeftUpCapture = i;
                                    else if ((i < kingLeftUpAlly || kingLeftUpAlly == 0) && pawnImageName.StartsWith("pawnImageW"))
                                        kingLeftUpAlly = i;
                                }

                                if (row == targetRow - i && column == targetColumn + i)
                                {
                                    kingRightUpPathAvailable[i - 1] = false;
                                    if ((i < kingRightUpCapture || kingRightUpCapture == 0) && pawnImageName.StartsWith("pawnImageB"))
                                        kingRightUpCapture = i;
                                    else if ((i < kingRightUpAlly || kingRightUpAlly == 0) && pawnImageName.StartsWith("pawnImageW"))
                                        kingRightUpAlly = i;
                                }

                                if (row == targetRow + i && column == targetColumn - i)
                                {
                                    kingLeftDownPathAvailable[i - 1] = false;
                                    if ((i < kingLeftDownCapture || kingLeftDownCapture == 0) && pawnImageName.StartsWith("pawnImageB"))
                                        kingLeftDownCapture = i;
                                    else if ((i < kingLeftDownAlly || kingLeftDownAlly == 0) && pawnImageName.StartsWith("pawnImageW"))
                                        kingLeftDownAlly = i;
                                }

                                if (row == targetRow + i && column == targetColumn + i)
                                {
                                    kingRightDownPathAvailable[i - 1] = false;
                                    if ((i < kingRightDownCapture || kingRightDownCapture == 0) && pawnImageName.StartsWith("pawnImageB"))
                                        kingRightDownCapture = i;
                                    else if ((i < kingRightDownAlly || kingRightDownAlly == 0) && pawnImageName.StartsWith("pawnImageW"))
                                        kingRightDownAlly = i;
                                }
                            }
                        }
                        else if (clickedImage.Name.StartsWith("pawnImageKB"))
                        {
                            for (int i = 1; i <= 6; i++)
                            {
                                if (row == targetRow - i && column == targetColumn - i)
                                {
                                    kingLeftUpPathAvailable[i - 1] = false;
                                    if ((i < kingLeftUpCapture || kingLeftUpCapture == 0) && pawnImageName.StartsWith("pawnImageW"))
                                        kingLeftUpCapture = i;
                                    else if ((i < kingLeftUpAlly || kingLeftUpAlly == 0) && pawnImageName.StartsWith("pawnImageB"))
                                        kingLeftUpAlly = i;
                                }

                                if (row == targetRow - i && column == targetColumn + i)
                                {
                                    kingRightUpPathAvailable[i - 1] = false;
                                    if ((i < kingRightUpCapture || kingRightUpCapture == 0) && pawnImageName.StartsWith("pawnImageW"))
                                        kingRightUpCapture = i;
                                    else if ((i < kingRightUpAlly || kingRightUpAlly == 0) && pawnImageName.StartsWith("pawnImageB"))
                                        kingRightUpAlly = i;
                                }

                                if (row == targetRow + i && column == targetColumn - i)
                                {
                                    kingLeftDownPathAvailable[i - 1] = false;
                                    if ((i < kingLeftDownCapture || kingLeftDownCapture == 0) && pawnImageName.StartsWith("pawnImageW"))
                                        kingLeftDownCapture = i;
                                    else if ((i < kingLeftDownAlly || kingLeftDownAlly == 0) && pawnImageName.StartsWith("pawnImageB"))
                                        kingLeftDownAlly = i;
                                }

                                if (row == targetRow + i && column == targetColumn + i)
                                {
                                    kingRightDownPathAvailable[i - 1] = false;
                                    if ((i < kingRightDownCapture || kingRightDownCapture == 0) && pawnImageName.StartsWith("pawnImageW"))
                                        kingRightDownCapture = i;
                                    else if ((i < kingRightDownAlly || kingRightDownAlly == 0) && pawnImageName.StartsWith("pawnImageB"))
                                        kingRightDownAlly = i;
                                }
                            }
                        }

                    }
                }

                if (clickedImage.Name.StartsWith("pawnImageK"))
                {
                    for (int i = 1; i <= 6; i++)
                    {
                        if ((i > kingLeftUpCapture + 1 && kingLeftUpCapture != 0) || (i > kingLeftUpAlly && kingLeftUpAlly != 0))
                            kingLeftUpPathAvailable[i - 1] = false;
                        if ((i > kingRightUpCapture + 1 && kingRightUpCapture != 0) || (i > kingRightUpAlly && kingRightUpAlly != 0))
                            kingRightUpPathAvailable[i - 1] = false;
                        if ((i > kingLeftDownCapture + 1 && kingLeftDownCapture != 0) || (i > kingLeftDownAlly && kingLeftDownAlly != 0))
                            kingLeftDownPathAvailable[i - 1] = false;
                        if ((i > kingRightDownCapture + 1 && kingRightDownCapture != 0) || (i > kingRightDownAlly && kingRightDownAlly != 0))
                            kingRightDownPathAvailable[i - 1] = false;
                    }

                    Console.WriteLine(kingLeftUpCapture + " " + kingLeftUpAlly + " " + kingRightUpCapture + " " + kingRightUpAlly + " " + kingLeftDownCapture + " " + kingLeftDownAlly + " " + kingRightDownCapture + " " + kingRightDownAlly + " ");

                    for (int i = 1; i <= 6; i++)
                    {
                        if (kingLeftUpPathAvailable[i - 1] == true)
                        {
                            Image image = new Image();
                            image.Source = new BitmapImage(new Uri(@"/images/greyPawn.png", UriKind.Relative));
                            string pathImageName = "pawnPath_K";
                            image.Name = pathImageName;

                            image.Margin = new Thickness(63 + (targetColumn - 1 - i) * 46.5, 63 + (targetRow - 1 - i) * 46.5, 0, 0);
                            Grid.SetRow(image, targetRow - i);
                            Grid.SetColumn(image, targetColumn - i);
                            image.Width = 36;
                            image.Height = 36;
                            image.HorizontalAlignment = HorizontalAlignment.Left;
                            image.VerticalAlignment = VerticalAlignment.Top;
                            image.Cursor = Cursors.Hand;
                            image.MouseLeftButtonDown += MovePawn;

                            gameGrid.Children.Add(image);
                        }
                        if (kingRightUpPathAvailable[i - 1] == true)
                        {
                            Image image = new Image();
                            image.Source = new BitmapImage(new Uri(@"/images/greyPawn.png", UriKind.Relative));
                            string pathImageName = "pawnPath_K";
                            image.Name = pathImageName;

                            image.Margin = new Thickness(63 + (targetColumn - 1 + i) * 46.5, 63 + (targetRow - 1 - i) * 46.5, 0, 0);
                            Grid.SetRow(image, targetRow - i);
                            Grid.SetColumn(image, targetColumn + i);
                            image.Width = 36;
                            image.Height = 36;
                            image.HorizontalAlignment = HorizontalAlignment.Left;
                            image.VerticalAlignment = VerticalAlignment.Top;
                            image.Cursor = Cursors.Hand;
                            image.MouseLeftButtonDown += MovePawn;

                            gameGrid.Children.Add(image);
                        }
                        if (kingLeftDownPathAvailable[i - 1] == true)
                        {
                            Image image = new Image();
                            image.Source = new BitmapImage(new Uri(@"/images/greyPawn.png", UriKind.Relative));
                            string pathImageName = "pawnPath_K";
                            image.Name = pathImageName;

                            image.Margin = new Thickness(63 + (targetColumn - 1 - i) * 46.5, 63 + (targetRow - 1 + i) * 46.5, 0, 0);
                            Grid.SetRow(image, targetRow + i);
                            Grid.SetColumn(image, targetColumn - i);
                            image.Width = 36;
                            image.Height = 36;
                            image.HorizontalAlignment = HorizontalAlignment.Left;
                            image.VerticalAlignment = VerticalAlignment.Top;
                            image.Cursor = Cursors.Hand;
                            image.MouseLeftButtonDown += MovePawn;

                            gameGrid.Children.Add(image);
                        }
                        if (kingRightDownPathAvailable[i - 1] == true)
                        {
                            Image image = new Image();
                            image.Source = new BitmapImage(new Uri(@"/images/greyPawn.png", UriKind.Relative));
                            string pathImageName = "pawnPath_K";
                            image.Name = pathImageName;

                            image.Margin = new Thickness(63 + (targetColumn - 1 + i) * 46.5, 63 + (targetRow - 1 + i) * 46.5, 0, 0);
                            Grid.SetRow(image, targetRow + i);
                            Grid.SetColumn(image, targetColumn + i);
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

                //jesli miejsce po prawej jest wolne

                if ((leftPathAvailable && !leftPathBlocked) || (leftCaptureAvailable && !leftCaptureBlocked))
                {
                    //dodaje obrazek imitujacy sciezke w odpowiednim miejscu

                    Image image = new Image();
                    image.Source = new BitmapImage(new Uri(@"/images/greyPawn.png", UriKind.Relative));
                    string pathImageName = "pawnPath_1";
                    image.Name = pathImageName;

                    Grid.SetRow(image, targetRow);
                    Grid.SetColumn(image, targetColumn);

                    if (currentGame.whitePawns.Equals(accountLogin))
                    {
                        if (leftPathAvailable)
                        {
                            image.Margin = new Thickness(63 + (targetColumn - 1 + 1) * 46.5, 63 + (targetRow - 1 - 1) * 46.5, 0, 0);
                            Grid.SetRow(image, targetRow - 1);
                            Grid.SetColumn(image, targetColumn + 1);
                        }
                        else if (leftCaptureAvailable)
                        {
                            if (gameGrid.Children.OfType<Image>().Any(i => Grid.GetRow(i) == targetRow - 1 && Grid.GetColumn(i) == targetColumn + 1 && (i.Name.StartsWith("pawnImageB") || i.Name.StartsWith("pawnImageKB"))))
                            {
                                image.Margin = new Thickness(63 + (targetColumn - 1 + 2) * 46.5, 63 + (targetRow - 1 - 2) * 46.5, 0, 0);
                                Grid.SetRow(image, targetRow - 2);
                                Grid.SetColumn(image, targetColumn + 2);
                            }
                            else
                                image = null;
                        }
                    }
                    else if (currentGame.blackPawns.Equals(accountLogin))
                    {
                        if (leftPathAvailable)
                        {
                            image.Margin = new Thickness(63 + (targetColumn - 1 + 1) * 46.5, 63 + (targetRow - 1 + 1) * 46.5, 0, 0);
                            Grid.SetRow(image, targetRow + 1);
                            Grid.SetColumn(image, targetColumn + 1);
                        }
                        else if (leftCaptureAvailable)
                        {
                            if (gameGrid.Children.OfType<Image>().Any(i => Grid.GetRow(i) == targetRow + 1 && Grid.GetColumn(i) == targetColumn + 1 && (i.Name.StartsWith("pawnImageW") || i.Name.StartsWith("pawnImageKW"))))
                            {
                                image.Margin = new Thickness(63 + (targetColumn - 1 + 2) * 46.5, 63 + (targetRow - 1 + 2) * 46.5, 0, 0);
                                Grid.SetRow(image, targetRow + 2);
                                Grid.SetColumn(image, targetColumn + 2);
                            }
                            else
                                image = null;
                        }
                    }

                    if (image != null)
                    {
                        image.Width = 36;
                        image.Height = 36;
                        image.HorizontalAlignment = HorizontalAlignment.Left;
                        image.VerticalAlignment = VerticalAlignment.Top;
                        image.Cursor = Cursors.Hand;
                        image.MouseLeftButtonDown += MovePawn;

                        gameGrid.Children.Add(image);
                    }
                }

                //jesli miejsce po lewej jest wolne

                if ((rightPathAvailable && !rightPathBlocked) || (rightCaptureAvailable && !rightCaptureBlocked))
                {
                    //dodaje obrazek imitujacy sciezke w odpowiednim miejscu

                    Image image = new Image();
                    image.Source = new BitmapImage(new Uri(@"/images/greyPawn.png", UriKind.Relative));
                    string pathImageName = "pawnPath_2";
                    image.Name = pathImageName;


                    if (currentGame.whitePawns.Equals(accountLogin))
                    {
                        if (rightPathAvailable)
                        {
                            image.Margin = new Thickness(63 + (targetColumn - 1 - 1) * 46.5, 63 + (targetRow - 1 - 1) * 46.5, 0, 0);
                            Grid.SetRow(image, targetRow - 1);
                            Grid.SetColumn(image, targetColumn - 1);
                        }
                        else if (rightCaptureAvailable)
                        {
                            if (gameGrid.Children.OfType<Image>().Any(i => Grid.GetRow(i) == targetRow - 1 && Grid.GetColumn(i) == targetColumn - 1 && (i.Name.StartsWith("pawnImageB") || i.Name.StartsWith("pawnImageKB"))))
                            {
                                image.Margin = new Thickness(63 + (targetColumn - 1 - 2) * 46.5, 63 + (targetRow - 1 - 2) * 46.5, 0, 0);
                                Grid.SetRow(image, targetRow - 2);
                                Grid.SetColumn(image, targetColumn - 2);
                            }
                            else
                                image = null;
                        }
                    }
                    else if (currentGame.blackPawns.Equals(accountLogin))
                    {
                        if (rightPathAvailable)
                        {
                            image.Margin = new Thickness(63 + (targetColumn - 1 - 1) * 46.5, 63 + (targetRow - 1 + 1) * 46.5, 0, 0);
                            Grid.SetRow(image, targetRow + 1);
                            Grid.SetColumn(image, targetColumn - 1);
                        }
                        else if (rightCaptureAvailable)
                        {
                            if (gameGrid.Children.OfType<Image>().Any(i => Grid.GetRow(i) == targetRow + 1 && Grid.GetColumn(i) == targetColumn - 1 && (i.Name.StartsWith("pawnImageW") || i.Name.StartsWith("pawnImageKW"))))
                            {
                                image.Margin = new Thickness(63 + (targetColumn - 1 - 2) * 46.5, 63 + (targetRow - 1 + 2) * 46.5, 0, 0);
                                Grid.SetRow(image, targetRow + 2);
                                Grid.SetColumn(image, targetColumn - 2);
                            }
                            else
                                image = null;
                        }
                    }
                    if (image != null)
                    {
                        image.Width = 36;
                        image.Height = 36;
                        image.HorizontalAlignment = HorizontalAlignment.Left;
                        image.VerticalAlignment = VerticalAlignment.Top;
                        image.Cursor = Cursors.Hand;

                        //jesli ta sciezka zostanie kliknieta wywoluje funkcje MovePawn

                        image.MouseLeftButtonDown += MovePawn;

                        gameGrid.Children.Add(image);
                    }
                }
            }
        }

        private void MovePawn(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image clickedPawnPath)
            {
                //pobieram dane o kliknietej sciezce

                string imageName = clickedPawnPath.Name;

                int targetRow = Grid.GetRow(clickedPawnPath);
                int targetColumn = Grid.GetColumn(clickedPawnPath);

                int selectedPawnRow = Grid.GetRow(selectedPawn);
                int selectedPawnColumn = Grid.GetColumn(selectedPawn);

                //implementuje usuwanie zbitego pionka

                if (selectedPawn.Name.StartsWith("pawnImageB"))
                {
                    if (selectedPawnRow == targetRow - 2 && selectedPawnColumn == targetColumn - 2)
                    {
                        var pawnToRemove = gameGrid.Children.OfType<Image>().FirstOrDefault(i => Grid.GetRow(i) == targetRow - 1 && Grid.GetColumn(i) == targetColumn - 1);

                        if (pawnToRemove != null)
                        {
                            gameGrid.Children.Remove(pawnToRemove);
                        }
                    }
                    else if (selectedPawnRow == targetRow - 2 && selectedPawnColumn == targetColumn + 2)
                    {
                        var pawnToRemove = gameGrid.Children.OfType<Image>().FirstOrDefault(i => Grid.GetRow(i) == targetRow - 1 && Grid.GetColumn(i) == targetColumn + 1);

                        if (pawnToRemove != null)
                        {
                            gameGrid.Children.Remove(pawnToRemove);
                        }
                    }
                }
                else if (selectedPawn.Name.StartsWith("pawnImageW"))
                {
                    if (selectedPawnRow == targetRow + 2 && selectedPawnColumn == targetColumn - 2)
                    {
                        var pawnToRemove = gameGrid.Children.OfType<Image>().FirstOrDefault(i => Grid.GetRow(i) == targetRow + 1 && Grid.GetColumn(i) == targetColumn - 1);

                        if (pawnToRemove != null)
                        {
                            gameGrid.Children.Remove(pawnToRemove);
                        }
                    }
                    else if (selectedPawnRow == targetRow + 2 && selectedPawnColumn == targetColumn + 2)
                    {
                        var pawnToRemove = gameGrid.Children.OfType<Image>().FirstOrDefault(i => Grid.GetRow(i) == targetRow + 1 && Grid.GetColumn(i) == targetColumn + 1);

                        if (pawnToRemove != null)
                        {
                            gameGrid.Children.Remove(pawnToRemove);
                        }
                    }
                }
                else if (selectedPawn.Name.StartsWith("pawnImageK"))
                {
                    if (Math.Abs(selectedPawnRow - targetRow) != 1 && Math.Abs(selectedPawnColumn - targetColumn) != 1)
                        if (selectedPawnRow < targetRow && selectedPawnColumn < targetColumn)
                        {
                            var pawnToRemove = gameGrid.Children.OfType<Image>().FirstOrDefault(i => Grid.GetRow(i) == targetRow - 1 && Grid.GetColumn(i) == targetColumn - 1);

                            if (pawnToRemove != null)
                            {
                                gameGrid.Children.Remove(pawnToRemove);
                            }
                        }
                        else if (selectedPawnRow < targetRow && selectedPawnColumn > targetColumn)
                        {
                            var pawnToRemove = gameGrid.Children.OfType<Image>().FirstOrDefault(i => Grid.GetRow(i) == targetRow - 1 && Grid.GetColumn(i) == targetColumn + 1);

                            if (pawnToRemove != null)
                            {
                                gameGrid.Children.Remove(pawnToRemove);
                            }
                        }
                        else if (selectedPawnRow > targetRow && selectedPawnColumn < targetColumn)
                        {
                            var pawnToRemove = gameGrid.Children.OfType<Image>().FirstOrDefault(i => Grid.GetRow(i) == targetRow + 1 && Grid.GetColumn(i) == targetColumn - 1);

                            if (pawnToRemove != null)
                            {
                                gameGrid.Children.Remove(pawnToRemove);
                            }
                        }
                        else if (selectedPawnRow > targetRow && selectedPawnColumn > targetColumn)
                        {
                            var pawnToRemove = gameGrid.Children.OfType<Image>().FirstOrDefault(i => Grid.GetRow(i) == targetRow + 1 && Grid.GetColumn(i) == targetColumn + 1);

                            if (pawnToRemove != null)
                            {
                                gameGrid.Children.Remove(pawnToRemove);
                            }
                        }
                }


                var foundImage = gameGrid.Children.OfType<Image>().FirstOrDefault(i => i.Name == selectedPawnName);

                Console.WriteLine(foundImage.Name + " " + selectedPawnName);

                if (foundImage != null)
                {
                    foundImage.Margin = new Thickness(63 + (targetColumn - 1) * 46.5, 63 + (targetRow - 1) * 46.5, 0, 0);


                    Grid.SetRow(foundImage, targetRow);
                    Grid.SetColumn(foundImage, targetColumn);
                }


                //usuwam wszystkie narysowane sciezki

                var imagesToRemove = gameGrid.Children.OfType<Image>().Where(img => img.Name != null && img.Name.StartsWith("pawnPath_"));

                foreach (var imageToRemove in imagesToRemove.ToList())
                {
                    gameGrid.Children.Remove(imageToRemove);
                }

                //czyszcze cala tablice odwzorowujaca plansze

                currentGame.board.Clear();
                currentGame.board = new List<List<string>>();

                for (int i = 0; i < 8; i++)
                {
                    List<string> row = new List<string>(Enumerable.Repeat("0", 8));
                    currentGame.board.Add(row);
                }

                //uzupelniam cala tablice odwzorowujaca plansze na podstawie pionkow umieszczonych na planszy

                foreach (var child in gameGrid.Children)
                {
                    if (child is Image image)
                    {
                        int row = Grid.GetRow(image);
                        int column = Grid.GetColumn(image);

                        if (image.Name.StartsWith("pawnImageW_"))
                        {
                            if (row == 1)
                                currentGame.board[row - 1][column - 1] = "KW";
                            else
                                currentGame.board[row - 1][column - 1] = "W";
                        }
                        else if (image.Name.StartsWith("pawnImageB_"))
                        {
                            if (row == 8)
                                currentGame.board[row - 1][column - 1] = "KB";
                            else
                                currentGame.board[row - 1][column - 1] = "B";

                        }
                        else if (image.Name.StartsWith("pawnImageKW_"))
                        {
                            currentGame.board[row - 1][column - 1] = "KW";
                        }
                        else if (image.Name.StartsWith("pawnImageKB_"))
                        {
                            currentGame.board[row - 1][column - 1] = "KB";
                        }

                    }
                }

                //aktualizuje dane o planszy przechowywane w bazie danych

                crud.UpdateGameRoomBoard(currentGame.host, currentGame.board);
                if (currentGame.whitePawns.Equals(accountLogin))
                {
                    crud.UpdateGameRoomTurn(currentGame.host, currentGame.blackPawns);
                    currentGame.turn = currentGame.blackPawns;
                }
                else
                {
                    crud.UpdateGameRoomTurn(currentGame.host, currentGame.whitePawns);
                    currentGame.turn = currentGame.whitePawns;
                }

                gameLabelsUpdate();

            }
        }

        private async Task OpponentListenerAsync(string host)
        {
            response1 = await conn.client.OnAsync("gameRooms/" + host, (sender, args, context) =>
            {
                if (args.Path.Equals("/whitePawns"))
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        opponent = args.Data.ToString();
                        opponentNameLabel.Content = args.Data.ToString();
                        waitingForOpponentsLabel.Content = "Waiting for you to start the game...."; ;
                    }));

                }
            });
        }

        private async Task GameStartedListenerAsync(string host)
        {
            response2 = await conn.client.OnAsync("gameRooms/" + host, (sender, args, context) =>
            {
                if (args.Path.Equals("/inProgress") && args.Data.Equals("True"))
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        createLobbyGrid.Visibility = Visibility.Collapsed;
                        gameGrid.Visibility = Visibility.Visible;
                        checkersGame();
                    }));
                }
            });
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var gameRooms = crud.LoadGameRooms();
            if (gameRooms == null)
            {
                (sender as DispatcherTimer)?.Stop();
                Console.WriteLine("Przeciwnik opuscil mecz");
                opponentNameLabel2.Content = "";
                Game_Over();
                return;
            }

            currentGame.board = null;

            foreach (var item in gameRooms)
            {
                if (item.Key == currentGame.host)
                {
                    currentGame.board = new List<List<string>>();
                    currentGame.turn = item.Value.turn;

                    foreach (var boardRow in item.Value.board)
                    {
                        currentGame.board.Add(new List<string>(boardRow));
                    }
                }
            }

            if (currentGame.board == null)
            {
                (sender as DispatcherTimer)?.Stop();
                Console.WriteLine("Przeciwnik opuscil mecz");
                opponentNameLabel2.Content = "";
                Game_Over();
                return;
            }

            if (!currentGame.turn.Equals(accountLogin))
            {
                drawPawns();
                Console.WriteLine("XD");
                if (boardUpdated)
                    boardUpdated = false;
            }
            if (currentGame.turn.Equals(accountLogin) && !boardUpdated)
            {
                drawPawns();
                Console.WriteLine("XD");
                boardUpdated = true;
                gameLabelsUpdate();
            }

        }

        public void drawPawns()
        {
            //usuwam wszystkie pionki narysowane na planszy

            var imagesToRemove = gameGrid.Children.OfType<Image>().Where(image => image.Name.StartsWith("pawnImageB") || image.Name.StartsWith("pawnImageW") || image.Name.StartsWith("pawnImageKB") || image.Name.StartsWith("pawnImageKW")).ToList();
            foreach (var imageToRemove in imagesToRemove)
            {
                gameGrid.Children.Remove(imageToRemove);
            }

            int i = 0;
            int b = 12;
            int w = 1;

            int kb = 12;
            int kw = 1;

            //na podstawie tablicy przechowywujacej aktualny stan pionkow na planszy rysuje kolejno pionki na planszy

            foreach (var boardRow in currentGame.board)
            {
                //Console.Write("Board Values: ");
                int j = 0;

                foreach (var value in boardRow)
                {
                    //Console.Write($"{value} ");
                    if (!value.Equals("0"))
                    {
                        Image image = new Image();

                        if (value.Equals("B"))
                        {
                            image.Source = new BitmapImage(new Uri(@"/images/blackPawn.png", UriKind.Relative));
                            string imageName = $"pawnImageB_{b}";
                            image.Name = imageName;

                            if (currentGame.blackPawns.Equals(accountLogin))
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
                        else if (value.Equals("KB"))
                        {
                            image.Source = new BitmapImage(new Uri(@"/images/blackPawnKing.png", UriKind.Relative));
                            string imageName = $"pawnImageKB_{w}";
                            image.Name = imageName;
                            if (currentGame.blackPawns.Equals(accountLogin))
                            {
                                image.MouseLeftButtonDown += ShowPawnPaths;
                                image.Cursor = Cursors.Hand;
                            }
                            kb--;
                        }
                        else if (value.Equals("KW"))
                        {
                            image.Source = new BitmapImage(new Uri(@"/images/whitePawnKing.png", UriKind.Relative));
                            string imageName = $"pawnImageKW_{w}";
                            image.Name = imageName;
                            if (currentGame.whitePawns.Equals(accountLogin))
                            {
                                image.MouseLeftButtonDown += ShowPawnPaths;
                                image.Cursor = Cursors.Hand;
                            }
                            kw--;
                        }

                        int rowIndex = i + 1;
                        int columnIndex = j + 1;

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
                //Console.WriteLine();
            }
        }

        void gameLabelsUpdate()
        {
            if (!currentGame.turn.Equals(accountLogin))
                whoseTurnLabel.Content = "YOUR OPPONENT's TURN";
            else
                whoseTurnLabel.Content = "YOUR TURN";

            int countPawnImageB = 0;
            int countPawnImageW = 0;

            foreach (var child in gameGrid.Children)
            {
                if (child is Image image)
                {
                    if (image.Name.StartsWith("pawnImageB") || image.Name.StartsWith("pawnImageKB"))
                    {
                        countPawnImageB++;
                    }
                    else if (image.Name.StartsWith("pawnImageW") || image.Name.StartsWith("pawnImageKW"))
                    {
                        countPawnImageW++;
                    }
                }
            }

            numberOfBlackPawnsCaptured.Content = 12 - countPawnImageB;
            numberOfWhitePawnsCaptured.Content = 12 - countPawnImageW;

            if(countPawnImageB == 0|| countPawnImageW == 0)
            {
                Game_Over();
            }

        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            crud.DeleteGameRoom(currentGame.host);
        }

        void Game_Over()
        {
            DoubleAnimation db = new DoubleAnimation();
            db.From = 0;
            db.To = 600;
            db.Duration = TimeSpan.FromSeconds(2);
            gameWidgetsGridTransform.BeginAnimation(TranslateTransform.YProperty, db);

            if (opponentNameLabel2.Content.Equals(""))
            {
                DoubleAnimation db2 = new DoubleAnimation();
                db2.From = -600;
                db2.To = 0;
                db2.Duration = TimeSpan.FromSeconds(2);
                opponentLeftGridTransform.BeginAnimation(TranslateTransform.YProperty, db2);
                opponentLeftGrid.Visibility = Visibility.Visible;
            }
            else
            {

            }
        }
    }
}
