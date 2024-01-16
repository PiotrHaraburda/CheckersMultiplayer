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

        int countPawnImageB = 0;
        int countPawnImageW = 0;
        int countPawnImageKB = 0;
        int countPawnImageKW = 0;

        EventStreamResponse response1;
        EventStreamResponse response2;
        EventStreamResponse response3;

        CRUDgame_rooms currentGame = new CRUDgame_rooms();

        Image selectedPawn;
        string selectedPawnName;

        private DispatcherTimer timer;
        private DispatcherTimer timer2;
        private TimeSpan remainingTime;

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
            mainMenuPanel.Visibility = Visibility.Hidden;
            multiplayerPanel.Visibility = Visibility.Visible;
        }

        private void quitMultiplayerButton_Click(object sender, RoutedEventArgs e)
        {
            multiplayerPanel.Visibility = Visibility.Hidden;
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
            multiplayerPanel.Visibility = Visibility.Hidden;

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
            multiplayerPanel.Visibility = Visibility.Hidden;
            createLobbyGrid.Visibility = Visibility.Visible;
            createLobbyMainLabel.Visibility = Visibility.Visible;
            roomNameLabel.Visibility = Visibility.Visible;
            roomNameTextBox.Visibility = Visibility.Visible;
            roomPasswordLabel.Visibility = Visibility.Visible;
            roomPasswordTextBox.Visibility = Visibility.Visible;
            startGameButton.Visibility = Visibility.Visible;
            waitingForOpponentsLabel.Visibility = Visibility.Visible;
            opponentLabel.Visibility = Visibility.Visible;
            saveRoomInfoButton.Visibility = Visibility.Visible;
            opponentNameLabel.Visibility = Visibility.Visible;

            CRUDgame_rooms game_room = crud.CreateGameRoom(accountLogin);
            roomNameTextBox.Text = game_room.roomName;
            currentGame.roomName = game_room.roomName;
            roomPasswordTextBox.Text = game_room.password;
            currentGame.host = accountLogin;
            waitingForOpponentsLabel.Content = "Waiting for opponents...";
            saveRoomInfoButton.IsEnabled = true;
            startGameButton.IsEnabled = true;
            roomNameTextBox.IsEnabled = true;
            roomPasswordTextBox.IsEnabled = true;
            opponentNameLabel.Content = "";
            startGameButton.IsEnabled = false;

            OpponentListenerAsync(accountLogin);
        }

        private void quitLobbyCreation_Click(object sender, RoutedEventArgs e)
        {
            multiplayerPanel.Visibility = Visibility.Visible;
            createLobbyGrid.Visibility = Visibility.Hidden;
            if(currentGame.roomName!=null)
            {
                if (accountLogin.Equals(currentGame.host))
                {
                    crud.DeleteGameRoom(accountLogin);
                    response1.Dispose();
                }
                else
                    crud.ClearGameRoomWhitePawns(currentGame.host, currentGame.blackPawns, currentGame.password, currentGame.roomName, currentGame.board, currentGame.turn);
            }

        }

        private void saveRoomInfoButton_Click(object sender, RoutedEventArgs e)
        {
            crud.UpdateGameRoom(accountLogin, roomPasswordTextBox.Text, roomNameTextBox.Text, opponentNameLabel.Content.ToString(), false);
        }

        private void quitGameRoomsButton_Click(object sender, RoutedEventArgs e)
        {
            gameRoomsGrid.Visibility = Visibility.Hidden;
            multiplayerPanel.Visibility = Visibility.Visible;
        }

        private void joinGameRoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (gameRoomsListBox.SelectedItem != null)
            {
                roomPasswordGrid.Visibility = Visibility.Visible;
                gameRoomsGrid.Visibility = Visibility.Hidden;
            }
        }

        private void quitRoomPasswordGridButton_Click(object sender, RoutedEventArgs e)
        {
            roomPasswordGrid.Visibility = Visibility.Hidden;
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
                            roomPasswordGrid.Visibility = Visibility.Hidden;
                            createLobbyGrid.Visibility = Visibility.Visible;
                            createLobbyMainLabel.Visibility = Visibility.Visible;
                            roomNameLabel.Visibility = Visibility.Visible;
                            roomNameTextBox.Visibility = Visibility.Visible;
                            roomPasswordLabel.Visibility = Visibility.Visible;
                            roomPasswordTextBox.Visibility = Visibility.Visible;
                            startGameButton.Visibility = Visibility.Visible;
                            waitingForOpponentsLabel.Visibility = Visibility.Visible;
                            opponentLabel.Visibility = Visibility.Visible;
                            saveRoomInfoButton.Visibility = Visibility.Visible;
                            opponentNameLabel.Visibility = Visibility.Visible;
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
                            currentGame.inProgress = item.Value.inProgress;


                            opponentNameLabel.Content = item.Value.blackPawns;
                            waitingForOpponentsLabel.Content = "Waiting for room host to start the game....";
                            saveRoomInfoButton.IsEnabled = false;
                            startGameButton.IsEnabled = false;

                            GameStartedListenerAsync(item.Value.host);

                            timer2 = new DispatcherTimer();
                            timer2.Interval = TimeSpan.FromSeconds(2);
                            timer2.Tick += Timer_GameRoomExisting;

                            timer2.Start();
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

        void Timer_GameRoomExisting(object sender, EventArgs e)
        {
            bool gameRoomExists = false;
            var gameRooms = crud.LoadGameRooms();

            if (gameRooms != null)
            {
                foreach (var item in gameRooms)
                {
                    if (item.Key.Equals(currentGame.host))
                    {
                        gameRoomExists = true;
                    }
                }
            }

            if(gameRoomExists==false)
            {
                DispatcherTimer dispatcherTimer = (DispatcherTimer)sender;
                dispatcherTimer.Stop();
                currentGame.roomName = null;
                createLobbyMainLabel.Visibility = Visibility.Hidden;
                roomNameLabel.Visibility = Visibility.Hidden;
                roomNameTextBox.Visibility = Visibility.Hidden;
                roomPasswordLabel.Visibility = Visibility.Hidden;
                roomPasswordTextBox.Visibility = Visibility.Hidden;
                startGameButton.Visibility = Visibility.Hidden;
                waitingForOpponentsLabel.Visibility = Visibility.Hidden;
                opponentLabel.Visibility = Visibility.Hidden;
                saveRoomInfoButton.Visibility = Visibility.Hidden;
                opponentNameLabel.Visibility = Visibility.Hidden;
            }

        }

        private void startGameButton_Click(object sender, RoutedEventArgs e)
        {
            crud.UpdateGameRoom(accountLogin, roomPasswordTextBox.Text, roomNameTextBox.Text, opponentNameLabel.Content.ToString(), true);
            createLobbyGrid.Visibility = Visibility.Hidden;
            gameGrid.Visibility = Visibility.Visible;
            checkersGame();
        }

        void checkersGame()
        {
            if(timer2!=null)
                timer2.Stop();

            var gameRooms = crud.LoadGameRooms();
            if (gameRooms == null)
                return;

            gameWidgetsGrid.Visibility = Visibility.Visible;
            opponentLeftGrid.Visibility = Visibility.Hidden;
            youLostGrid.Visibility = Visibility.Hidden;
            youWonGrid.Visibility = Visibility.Hidden;
            drawGrid.Visibility = Visibility.Hidden;

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
                    currentGame.inProgress = item.Value.inProgress;
                }
            }


            if (currentGame.whitePawns.Equals(accountLogin))
                opponentNameLabel2.Content = currentGame.blackPawns;
            else
                opponentNameLabel2.Content = currentGame.whitePawns;


            //uruchomienie zegara sprawdzającego czy polozenie pionkow w bazie zostalo zmienione

            remainingTime = TimeSpan.FromMinutes(5);
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
            if (sender is Image clickedImage && currentGame.turn.Equals(accountLogin) && currentGame.inProgress)
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
                                    if ((i < kingLeftUpCapture || kingLeftUpCapture == 0) && (pawnImageName.StartsWith("pawnImageB") || pawnImageName.StartsWith("pawnImageKB")))
                                        kingLeftUpCapture = i;
                                    else if ((i < kingLeftUpAlly || kingLeftUpAlly == 0) && (pawnImageName.StartsWith("pawnImageW") || pawnImageName.StartsWith("pawnImageKW")))
                                        kingLeftUpAlly = i;
                                }

                                if (row == targetRow - i && column == targetColumn + i)
                                {
                                    kingRightUpPathAvailable[i - 1] = false;
                                    if ((i < kingRightUpCapture || kingRightUpCapture == 0) && (pawnImageName.StartsWith("pawnImageB") || pawnImageName.StartsWith("pawnImageKB")))
                                        kingRightUpCapture = i;
                                    else if ((i < kingRightUpAlly || kingRightUpAlly == 0) && (pawnImageName.StartsWith("pawnImageW") || pawnImageName.StartsWith("pawnImageKW")))
                                        kingRightUpAlly = i;
                                }

                                if (row == targetRow + i && column == targetColumn - i)
                                {
                                    kingLeftDownPathAvailable[i - 1] = false;
                                    if ((i < kingLeftDownCapture || kingLeftDownCapture == 0) && (pawnImageName.StartsWith("pawnImageB") || pawnImageName.StartsWith("pawnImageKB")))
                                        kingLeftDownCapture = i;
                                    else if ((i < kingLeftDownAlly || kingLeftDownAlly == 0) && (pawnImageName.StartsWith("pawnImageW") || pawnImageName.StartsWith("pawnImageKW")))
                                        kingLeftDownAlly = i;
                                }

                                if (row == targetRow + i && column == targetColumn + i)
                                {
                                    kingRightDownPathAvailable[i - 1] = false;
                                    if ((i < kingRightDownCapture || kingRightDownCapture == 0) && (pawnImageName.StartsWith("pawnImageB") || pawnImageName.StartsWith("pawnImageKB")))
                                        kingRightDownCapture = i;
                                    else if ((i < kingRightDownAlly || kingRightDownAlly == 0) && (pawnImageName.StartsWith("pawnImageW") || pawnImageName.StartsWith("pawnImageKW")))
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
                                    if ((i < kingLeftUpCapture || kingLeftUpCapture == 0) && (pawnImageName.StartsWith("pawnImageW") || pawnImageName.StartsWith("pawnImageKW")))
                                        kingLeftUpCapture = i;
                                    else if ((i < kingLeftUpAlly || kingLeftUpAlly == 0) && (pawnImageName.StartsWith("pawnImageB") || pawnImageName.StartsWith("pawnImageKB")))
                                        kingLeftUpAlly = i;
                                }

                                if (row == targetRow - i && column == targetColumn + i)
                                {
                                    kingRightUpPathAvailable[i - 1] = false;
                                    if ((i < kingRightUpCapture || kingRightUpCapture == 0) && (pawnImageName.StartsWith("pawnImageW") || pawnImageName.StartsWith("pawnImageKW")))
                                        kingRightUpCapture = i;
                                    else if ((i < kingRightUpAlly || kingRightUpAlly == 0) && (pawnImageName.StartsWith("pawnImageB") || pawnImageName.StartsWith("pawnImageKB")))
                                        kingRightUpAlly = i;
                                }

                                if (row == targetRow + i && column == targetColumn - i)
                                {
                                    kingLeftDownPathAvailable[i - 1] = false;
                                    if ((i < kingLeftDownCapture || kingLeftDownCapture == 0) && (pawnImageName.StartsWith("pawnImageW") || pawnImageName.StartsWith("pawnImageKW")))
                                        kingLeftDownCapture = i;
                                    else if ((i < kingLeftDownAlly || kingLeftDownAlly == 0) && (pawnImageName.StartsWith("pawnImageB") || pawnImageName.StartsWith("pawnImageKB")))
                                        kingLeftDownAlly = i;
                                }

                                if (row == targetRow + i && column == targetColumn + i)
                                {
                                    kingRightDownPathAvailable[i - 1] = false;
                                    if ((i < kingRightDownCapture || kingRightDownCapture == 0) && (pawnImageName.StartsWith("pawnImageW") || pawnImageName.StartsWith("pawnImageKW")))
                                        kingRightDownCapture = i;
                                    else if ((i < kingRightDownAlly || kingRightDownAlly == 0) && (pawnImageName.StartsWith("pawnImageB") || pawnImageName.StartsWith("pawnImageKB")))
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
                        Console.WriteLine("JD");
                        opponent = args.Data.ToString();
                        opponentNameLabel.Content = args.Data.ToString();
                        waitingForOpponentsLabel.Content = "Waiting for you to start the game...."; ;
                        startGameButton.IsEnabled = true;
                    }));
                    OpponentListenerAsync(host);
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
                        createLobbyGrid.Visibility = Visibility.Hidden;
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
                DispatcherTimer dispatcherTimer = (DispatcherTimer)sender;
                dispatcherTimer.Stop();
                Console.WriteLine("Przeciwnik opuscil mecz");
                currentGame.inProgress = false;
                if(countPawnImageW!=0&&countPawnImageB!=0)
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
                DispatcherTimer dispatcherTimer = (DispatcherTimer)sender;
                dispatcherTimer.Stop();
                Console.WriteLine("Przeciwnik opuscil mecz");
                currentGame.inProgress = false;
                if (countPawnImageW != 0 && countPawnImageB != 0)
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
            if (currentGame.turn.Equals(accountLogin))
            {
                remainingTime = remainingTime.Subtract(TimeSpan.FromSeconds(1));

                timeRemainingLabel.Content = remainingTime.ToString("mm':'ss");

                if (remainingTime <= TimeSpan.Zero)
                {
                    if (accountLogin.Equals(currentGame.whitePawns))
                    {
                        DispatcherTimer dispatcherTimer = (DispatcherTimer)sender;
                        dispatcherTimer.Stop();
                        currentGame.inProgress = false;
                        if (countPawnImageW != 0 && countPawnImageB != 0)
                            opponentNameLabel2.Content = "";
                        crud.ClearGameRoomWhitePawns(currentGame.host, currentGame.blackPawns, currentGame.password, currentGame.roomName, currentGame.board, currentGame.turn);
                        countPawnImageW = 0;
                    }
                    else if (accountLogin.Equals(currentGame.blackPawns))
                    {
                        DispatcherTimer dispatcherTimer = (DispatcherTimer)sender;
                        dispatcherTimer.Stop();
                        currentGame.inProgress = false;
                        if (countPawnImageW != 0 && countPawnImageB != 0)
                            opponentNameLabel2.Content = "";
                        crud.DeleteGameRoom(accountLogin);
                        countPawnImageB = 0;
                    }
                }

                if (!boardUpdated)
                {
                    drawPawns();
                    Console.WriteLine("XD");
                    boardUpdated = true;
                    gameLabelsUpdate();
                }
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
                            string imageName = $"pawnImageKB_{kb}";
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
                            string imageName = $"pawnImageKW_{kw}";
                            image.Name = imageName;
                            if (currentGame.whitePawns.Equals(accountLogin))
                            {
                                image.MouseLeftButtonDown += ShowPawnPaths;
                                image.Cursor = Cursors.Hand;
                            }
                            kw++;
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
            countPawnImageB = 0;
            countPawnImageW = 0;
            countPawnImageKB = 0;
            countPawnImageKW = 0;

            if (!currentGame.turn.Equals(accountLogin))
                whoseTurnLabel.Content = "YOUR OPPONENT's TURN";
            else
                whoseTurnLabel.Content = "YOUR TURN";


            foreach (var child in gameGrid.Children)
            {
                if (child is Image image)
                {
                    if (image.Name.StartsWith("pawnImageB"))
                    {
                        countPawnImageB++;
                    }
                    else if (image.Name.StartsWith("pawnImageKB"))
                    {
                        countPawnImageB++;
                        countPawnImageKB++;
                    }
                    else if (image.Name.StartsWith("pawnImageW"))
                    {
                        countPawnImageW++;
                    }
                    else if (image.Name.StartsWith("pawnImageKW"))
                    {
                        countPawnImageW++;
                        countPawnImageKW++;
                    }
                }
            }

            numberOfBlackPawnsCaptured.Content = 12 - countPawnImageB;
            numberOfWhitePawnsCaptured.Content = 12 - countPawnImageW;

            if(countPawnImageB == 0 || countPawnImageW == 0 || (countPawnImageKB==1 && countPawnImageKW==1 && countPawnImageB == 1 && countPawnImageW == 1))
            {
                timer.Stop();
                currentGame.inProgress = false;
                if (countPawnImageW != 0 && countPawnImageB != 0)
                    opponentNameLabel2.Content = "";
                Console.WriteLine("OKKKK3");
                Game_Over();
                return;
            }

        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            crud.DeleteGameRoom(currentGame.host);
        }

        void Game_Over()
        {
            if(mainMenuPanel.Visibility == Visibility.Hidden)
            {
                DoubleAnimation db = new DoubleAnimation();
                db.From = 0;
                db.To = 600;
                db.Duration = TimeSpan.FromSeconds(2);

                db.Completed += (sender, e) =>
                {
                    gameWidgetsGrid.Visibility = Visibility.Hidden;
                    DoubleAnimation reverseDb = new DoubleAnimation();
                    reverseDb.From = 600;
                    reverseDb.To = 0;
                    reverseDb.Duration = TimeSpan.FromSeconds(2);
                    gameWidgetsGridTransform.BeginAnimation(TranslateTransform.YProperty, reverseDb);
                };

                gameWidgetsGridTransform.BeginAnimation(TranslateTransform.YProperty, db);
            }


            if (opponentNameLabel2.Content.Equals("")&&(countPawnImageKB == 1 && countPawnImageKW == 1 && countPawnImageB == 1 && countPawnImageW == 1))
            {
                DoubleAnimation db2 = new DoubleAnimation();
                db2.From = -600;
                db2.To = 0;
                db2.Duration = TimeSpan.FromSeconds(2);
                drawGridTransform.BeginAnimation(TranslateTransform.YProperty, db2);
                drawGrid.Visibility = Visibility.Visible;
            }
            else if (opponentNameLabel2.Content.Equals("") && !(countPawnImageKB == 1 && countPawnImageKW == 1 && countPawnImageB == 1 && countPawnImageW == 1))
            {
                DoubleAnimation db2 = new DoubleAnimation();
                db2.From = -600;
                db2.To = 0;
                db2.Duration = TimeSpan.FromSeconds(2);
                opponentLeftGridTransform.BeginAnimation(TranslateTransform.YProperty, db2);
                opponentLeftGrid.Visibility = Visibility.Visible;
            }
            else if(!opponentNameLabel2.Content.Equals("")&&accountLogin.Equals(currentGame.whitePawns) && countPawnImageW == 0)
            {
                DoubleAnimation db2 = new DoubleAnimation();
                db2.From = -600;
                db2.To = 0;
                db2.Duration = TimeSpan.FromSeconds(2);
                youLostGridTransform.BeginAnimation(TranslateTransform.YProperty, db2);
                youLostGrid.Visibility = Visibility.Visible;
            }
            else if (!opponentNameLabel2.Content.Equals("") && accountLogin.Equals(currentGame.blackPawns) && countPawnImageB == 0)
            {
                DoubleAnimation db2 = new DoubleAnimation();
                db2.From = -600;
                db2.To = 0;
                db2.Duration = TimeSpan.FromSeconds(2);
                youLostGridTransform.BeginAnimation(TranslateTransform.YProperty, db2);
                youLostGrid.Visibility = Visibility.Visible;
            }
            else if (!opponentNameLabel2.Content.Equals("") && accountLogin.Equals(currentGame.whitePawns) && countPawnImageB == 0)
            {
                DoubleAnimation db2 = new DoubleAnimation();
                db2.From = -600;
                db2.To = 0;
                db2.Duration = TimeSpan.FromSeconds(2);
                youWonGridTransform.BeginAnimation(TranslateTransform.YProperty, db2);
                youWonGrid.Visibility = Visibility.Visible;
            }
            else if (!opponentNameLabel2.Content.Equals("") && accountLogin.Equals(currentGame.blackPawns) && countPawnImageW == 0)
            {
                DoubleAnimation db2 = new DoubleAnimation();
                db2.From = -600;
                db2.To = 0;
                db2.Duration = TimeSpan.FromSeconds(2);
                youWonGridTransform.BeginAnimation(TranslateTransform.YProperty, db2);
                youWonGrid.Visibility = Visibility.Visible;
            }
        }

        private void quitGameButton_Click(object sender, RoutedEventArgs e)
        {
            gameGrid.Visibility = Visibility.Hidden;
            mainMenuPanel.Visibility = Visibility.Visible;
            crud.DeleteGameRoom(currentGame.host);
            opponentNameLabel2.Content = "";

            if (accountLogin.Equals(currentGame.whitePawns))
            {
                countPawnImageW = 0;
            }
            else if (accountLogin.Equals(currentGame.blackPawns))
            {
                countPawnImageB = 0;
            }

        }
    }
}
