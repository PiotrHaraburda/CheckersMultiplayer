using FireSharp.Response;
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

        CRUDgame_rooms currentGame=new CRUDgame_rooms();

        Image selectedPawn;

        private DispatcherTimer timer;

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

            response1.Dispose();
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
                if(item.Key.Equals(currentGame.host))
                {
                    currentGame.roomName = item.Value.roomName;
                    currentGame.whitePawns = item.Value.whitePawns;
                    currentGame.blackPawns = item.Value.blackPawns;
                    currentGame.board=item.Value.board;
                }
            }

            //uruchomienie zegara sprawdzającego czy polozenie pionkow w bazie zostalo zmienione

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += Timer_Tick;

            timer.Start();

            //narysowanie pionkow na podstawie aktualnych danych z bazy

            drawPawns();

        }

        private void ShowPawnPaths(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image clickedImage)
            {
                //usuniecie wszystkich widocznych sciezek

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

                //pobranie polozenia pionka ktory zostal klikniety

                string imageName = clickedImage.Name;

                int targetRow = Grid.GetRow(clickedImage);
                int targetColumn = Grid.GetColumn(clickedImage);


                //System.Console.WriteLine(targetRow + " " + targetColumn);

                selectedPawn = clickedImage;


                bool leftPathNotAvailable = false;
                bool rightPathNotAvailable = false;

                bool leftCaptureNotAvailable = false;
                bool rightCaptureNotAvailable = false;

                bool leftPathBlocked = false;
                bool rightPathBlocked = false;

                //dla kazdego pionka narysowanego na planszy

                foreach (var child in gameGrid.Children)
                {
                    if (child is Image)
                    {
                        Image pawnImage = (Image)child;

                        //pobranie polozenia pionka
                        int row = Grid.GetRow(pawnImage);
                        int column = Grid.GetColumn(pawnImage);

                        // jezeli sterujemy pionkami bialymi sprawdzamy czy jakis pionek znajduje sie nad kliknietym pionkiem po lewej i prawej
                        // jesli natomiast sterujemy czarnymi to patrzymy pod spodem

                        if (currentGame.whitePawns.Equals(accountLogin))
                        {
                            if ((row == targetRow - 1 && column == targetColumn + 1))
                            {
                                leftPathNotAvailable = true;
                            }
                            if ((row == targetRow - 1 && column == targetColumn - 1))
                            {
                                rightPathNotAvailable = true;
                            }

                            if ((row == targetRow - 2 && column == targetColumn + 2))
                            {
                                leftCaptureNotAvailable = true;
                            }
                            if ((row == targetRow - 2 && column == targetColumn - 2))
                            {
                                rightCaptureNotAvailable = true;
                            }

                            if ((targetRow - 1 < 1) || (targetColumn + 1 > 8))
                            {
                                leftPathBlocked = true;
                            }
                            if ((targetRow - 1 < 1) || (targetColumn - 1 < 1))
                            {
                                rightPathBlocked = true;
                            }
                        }
                        else if (currentGame.blackPawns.Equals(accountLogin))
                        {
                            if ((row == targetRow + 1 && column == targetColumn + 1))
                            {
                                leftPathNotAvailable = true;
                            }
                            if ((row == targetRow + 1 && column == targetColumn - 1))
                            {
                                rightPathNotAvailable = true;
                            }

                            if ((row == targetRow + 2 && column == targetColumn + 2))
                            {
                                leftCaptureNotAvailable = true;
                            }
                            if ((row == targetRow + 2 && column == targetColumn - 2))
                            {
                                rightCaptureNotAvailable = true;
                            }

                            if ((targetRow + 1 > 8) || (targetColumn + 1 > 8))
                            {
                                leftPathBlocked = true;

                            }
                            if((targetRow + 1 > 8) || (targetColumn - 1 < 1))
                            {
                                rightPathBlocked = true;
                            }
                        }
                    }
                }

                //jesli miejsce po prawej jest wolne

                if ((!leftPathNotAvailable&&!leftPathBlocked)||(!leftCaptureNotAvailable))
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
                        if(!leftPathNotAvailable)
                        {
                            image.Margin = new Thickness(63 + (targetColumn - 1 + 1) * 46.5, 63 + (targetRow - 1 - 1) * 46.5, 0, 0);
                            Grid.SetRow(image, targetRow - 1);
                            Grid.SetColumn(image, targetColumn + 1);
                        }
                        else if(!leftCaptureNotAvailable)
                        {
                            image.Margin = new Thickness(63 + (targetColumn - 1 + 2) * 46.5, 63 + (targetRow - 1 - 2) * 46.5, 0, 0);
                            Grid.SetRow(image, targetRow - 2);
                            Grid.SetColumn(image, targetColumn + 2);
                        }
                    }
                    else if (currentGame.blackPawns.Equals(accountLogin))
                    {
                        if (!leftPathNotAvailable)
                        {
                            image.Margin = new Thickness(63 + (targetColumn - 1 + 1) * 46.5, 63 + (targetRow - 1 + 1) * 46.5, 0, 0);
                            Grid.SetRow(image, targetRow + 1);
                            Grid.SetColumn(image, targetColumn + 1);
                        }
                        else if (!leftCaptureNotAvailable)
                        {
                            image.Margin = new Thickness(63 + (targetColumn - 1 + 2) * 46.5, 63 + (targetRow - 1 + 2) * 46.5, 0, 0);
                            Grid.SetRow(image, targetRow + 2);
                            Grid.SetColumn(image, targetColumn + 2);
                        }
                    }

                    image.Width = 36;
                    image.Height = 36;
                    image.HorizontalAlignment = HorizontalAlignment.Left;
                    image.VerticalAlignment = VerticalAlignment.Top;
                    image.Cursor = Cursors.Hand;
                    image.MouseLeftButtonDown += MovePawn;

                    gameGrid.Children.Add(image);
                }
                
                //jesli miejsce po lewej jest wolne

                if((!rightPathNotAvailable && !rightPathBlocked) || (!rightCaptureNotAvailable))
                {
                    //dodaje obrazek imitujacy sciezke w odpowiednim miejscu

                    Image image = new Image();
                    image.Source = new BitmapImage(new Uri(@"/images/greyPawn.png", UriKind.Relative));
                    string pathImageName = "pawnPath_2";
                    image.Name = pathImageName;


                    if (currentGame.whitePawns.Equals(accountLogin))
                    {
                        if (!rightPathNotAvailable)
                        {
                            image.Margin = new Thickness(63 + (targetColumn - 1 - 1) * 46.5, 63 + (targetRow - 1 - 1) * 46.5, 0, 0);
                            Grid.SetRow(image, targetRow - 1);
                            Grid.SetColumn(image, targetColumn - 1);
                        }
                        else if (!rightCaptureNotAvailable)
                        {
                            image.Margin = new Thickness(63 + (targetColumn - 1 - 2) * 46.5, 63 + (targetRow - 1 - 2) * 46.5, 0, 0);
                            Grid.SetRow(image, targetRow - 2);
                            Grid.SetColumn(image, targetColumn - 2);
                        }
                    }
                    else if (currentGame.blackPawns.Equals(accountLogin))
                    {
                        if (!rightPathNotAvailable)
                        {
                            image.Margin = new Thickness(63 + (targetColumn - 1 - 1) * 46.5, 63 + (targetRow - 1 + 1) * 46.5, 0, 0);
                            Grid.SetRow(image, targetRow + 1);
                            Grid.SetColumn(image, targetColumn - 1);
                        }
                        else if (!rightCaptureNotAvailable)
                        {
                            image.Margin = new Thickness(63 + (targetColumn - 1 - 2) * 46.5, 63 + (targetRow - 1 + 2) * 46.5, 0, 0);
                            Grid.SetRow(image, targetRow + 2);
                            Grid.SetColumn(image, targetColumn - 2);
                        }
                    }

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

        private void MovePawn(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image clickedPawnPath)
            {
                //pobieram dane o kliknietej sciezce

                string imageName = clickedPawnPath.Name;

                int targetRow = Grid.GetRow(clickedPawnPath);
                int targetColumn = Grid.GetColumn(clickedPawnPath);

                //stawiam tam pionka ktorego wczesniej wybralem

                selectedPawn.Margin = new Thickness(63 + (targetColumn-1) * 46.5, 63 + (targetRow-1) * 46.5, 0, 0);


                Grid.SetRow(selectedPawn, targetRow);
                Grid.SetColumn(selectedPawn, targetColumn);

                //usuwam wszystkie narysowane sciezki

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
                            currentGame.board[row-1][column-1] = "W";
                        }
                        else if (image.Name.StartsWith("pawnImageB_"))
                        {
                            currentGame.board[row-1][column-1] = "B";
                        }

                    }
                }

                //aktualizuje dane o planszy przechowywane w bazie danych

                crud.UpdateGameRoomBoard(currentGame.host, currentGame.board);
            }
        }

        private async Task OpponentListenerAsync(string host)
        {
            response1 = await conn.client.OnAsync("gameRooms/" + host, (sender, args, context) =>
            {
                if (args.Path.Equals("/whitePawns"))
                {
                    Dispatcher.Invoke(new Action(() => {
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
                    Dispatcher.Invoke(new Action(() => {
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
                return;

            foreach (var item in gameRooms)
            {
                if (item.Key == currentGame.host)
                {
                    currentGame.board = new List<List<string>>();

                    foreach (var boardRow in item.Value.board)
                    {
                        // Copy the values from gameRooms into currentBoard
                        currentGame.board.Add(new List<string>(boardRow));
                    }
                }
            }

            drawPawns();

            Console.WriteLine("XD");
        }

        public void drawPawns()
        {
            //usuwam wszystkie pionki narysowane na planszy

            var imagesToRemove = gameGrid.Children.OfType<Image>().Where(image => image.Name.StartsWith("pawnImageB") || image.Name.StartsWith("pawnImageW")).ToList();
            foreach (var imageToRemove in imagesToRemove)
            {
                gameGrid.Children.Remove(imageToRemove);
            }

            int i = 0;
            int b = 12;
            int w = 1;

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

                        // Calculate row and column indices based on i and j
                        int rowIndex = i + 1;
                        int columnIndex = j + 1;

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
                //Console.WriteLine();
            }
        }

    }
}
