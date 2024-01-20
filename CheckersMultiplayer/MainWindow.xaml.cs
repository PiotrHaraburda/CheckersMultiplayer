using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CheckersMultiplayer.scripts;
using FireSharp.Response;

namespace CheckersMultiplayer
{
    public partial class MainWindow
    {
        private readonly FirebaseCrud _firebaseCrud = new FirebaseCrud(); //Klasa z operacjami CRUD wykonywanymi na bazie danych Firebase Realtime Database i autoryzacją
        private readonly FirebaseConnection _firebaseConnection = new FirebaseConnection(); //Klasa z połączeniem do bazy danych

        //dane zalogowanego konta
        private readonly string _accountName;
        private readonly string _accountLogin;
        private readonly int _accountAge;
        private readonly string _accountEmail;
        private readonly bool _accountInGame;
        private readonly bool _accountOnline;
        private int _accountVR;
        private int _enemyVR;

        //dane nt. pionków podczas gry
        private int _countPawnImageB;
        private int _countPawnImageW;
        private int _countPawnImageKB;
        private int _countPawnImageKW;

        private EventStreamResponse _response1; //odpowiedz na zapytanie o przeciwnika w lobby

        private readonly GameRooms _currentGame = new GameRooms(); //dane nt. aktualnego pokoju gier

        private Image _selectedPawnImage; //obrazek wybranego pionka
        private string _selectedPawnName; //nazwa wybranego pionka

        private DispatcherTimer _timer; //timer aktualizujacy narysowane pionki na planszy
        private DispatcherTimer _timer2; //timer sprawdzajacy czy pokoj gier nadal istnieje
        private TimeSpan _remainingTime; //pozostaly czas ruchu

        private bool _boardUpdated; //flaga sprawdzajaca czy pionki zostaly narysowane po ostatnim ruchu przeciwnika

        private bool _loggedOut; //flaga sprawdzajaca czy uzytkownik juz sie wylogowal

        private ObservableCollection<PlayerRankingData> playerRankingData { get; set; } //kolekcja widgetow ktore umieszczam w listboxie rankingowym

        //playery odtwarzajace dzwieki
        System.Media.SoundPlayer moveMediaPlayer;
        System.Media.SoundPlayer pathMediaPlayer;
        System.Media.SoundPlayer clickMediaPlayer;
        System.Media.SoundPlayer loseMediaPlayer;
        System.Media.SoundPlayer winMediaPlayer;

        public MainWindow(string accountName, string accountLogin, int accountAge, string accountEmail, bool accountInGame, bool accountOnline, int accountVR)
        {
            InitializeComponent();

            //pobranie danych o koncie gracza poprzez odczytanie ich z parametrow konstruktora
            _accountName = accountName;
            _accountLogin = accountLogin;
            _accountAge = accountAge;
            _accountEmail = accountEmail;
            _accountInGame = accountInGame;
            _accountOnline = accountOnline;
            _accountVR = accountVR;

            //zaladowanie tekstu w account grid
            string[] nameParts = accountName.Split(' ');

            if (nameParts.Length >= 2)
            {
                firstNameValueLabel.Content = nameParts[0];
                lastNameValueLabel.Content = nameParts[1];
            }
            else
            {
                firstNameValueLabel.Content = accountName;
                lastNameValueLabel.Content = "";
            }

            loginValueLabel.Content = accountLogin;
            emailValueLabel.Content = accountEmail;
            ageValueLabel.Content = accountAge;
            VRValueLabel.Content = accountVR;
            accountWelcomeLabel.Content = "Welcome to your account settings, " + accountName + "!";

            //zainicjowanie playerow
            InitializeMediaPlayer();
        }

        private void InitializeMediaPlayer()
        {
            //podanie sciezki do kazdego playera
            moveMediaPlayer = new System.Media.SoundPlayer("..\\..\\sounds\\moveAudio.wav");
            pathMediaPlayer = new System.Media.SoundPlayer("..\\..\\sounds\\pathAudio.wav");
            clickMediaPlayer = new System.Media.SoundPlayer("..\\..\\sounds\\clickAudio.wav");
            winMediaPlayer = new System.Media.SoundPlayer("..\\..\\sounds\\winAudio.wav");
            loseMediaPlayer = new System.Media.SoundPlayer("..\\..\\sounds\\loseAudio.wav");
        }

        private void Timer_GameRoomExisting(object sender, EventArgs e)
        {
            //ustawienie flagi na odpowiednia wartosc w zaleznosci czy pokoj gier istnieje 
            bool gameRoomExists = false;
            var gameRooms = _firebaseCrud.LoadGameRooms();

            if (gameRooms != null)
            {
                foreach (var item in gameRooms)
                {
                    if (item.Key.Equals(_currentGame.host))
                    {
                        gameRoomExists = true;
                    }
                }
            }

            if (gameRoomExists) return;

            //jesli pokoj gier nie istnieje, zatrzymujemy ten timer i chowamy widgety
            
            var dispatcherTimer = (DispatcherTimer)sender;
            dispatcherTimer.Stop();
            _currentGame.roomName = null;
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

        private async Task OpponentListenerAsync(string host)
        {
            //nasluchiwanie zmian w bazie danych wprowadzonych za pomoca komendy "set" w miejscu "whitePawns" w odpowiednim pokoju
            _response1 = await _firebaseConnection.Client.OnAsync("gameRooms/" + host, (sender, args, context) =>
            {
                if (args.Path.Equals("/whitePawns"))
                {
                    Dispatcher.Invoke(() =>
                    {
                        opponentNameLabel.Content = args.Data.ToString();
                        waitingForOpponentsLabel.Content = "Waiting for you to start the game....";
                        startGameButton.IsEnabled = true;
                    });
                    OpponentListenerAsync(host);
                }
            });
        }

        private async Task GameStartedListenerAsync(string host)
        {
            //nasluchiwanie zmian w bazie danych wprowadzonych za pomoca komendy "set" w miejscu "inProgress" w odpowiednim pokoju
            await _firebaseConnection.Client.OnAsync("gameRooms/" + host, (sender, args, context) =>
            {
                if (args.Path.Equals("/inProgress") && args.Data.Equals("True"))
                {
                    Dispatcher.Invoke(() =>
                    {
                        createLobbyGrid.Visibility = Visibility.Hidden;
                        gameGrid.Visibility = Visibility.Visible;
                        CheckersGame();
                    });
                }
            });
        }

        private void Timer_BoardUpdate(object sender, EventArgs e)
        {
            var gameRooms = _firebaseCrud.LoadGameRooms(); //pobieram dane o pokojach gier

            //jezeli nie ma zadnego pokoju gier to aktualna gra napewno zostala zakonczona
            if (gameRooms == null)
            {
                var dispatcherTimer = (DispatcherTimer)sender;
                dispatcherTimer.Stop();
                _timer.Stop();
                _currentGame.inProgress = false;
                if (_countPawnImageW != 0 && _countPawnImageB != 0)
                    opponentNameLabel2.Content = "";
                GameOver();
                return;
            }

            _currentGame.board = null;

            //aktualizuje swoja liste list przechowujaca aktualna pozycje wszystkich pionkow na planszy
            foreach (var item in gameRooms)
            {
                if (item.Key != _currentGame.host) continue;

                _currentGame.board = new List<List<string>>();
                _currentGame.turn = item.Value.turn;

                foreach (var boardRow in item.Value.board)
                {
                    _currentGame.board.Add(new List<string>(boardRow));
                }
            }

            //jezeli lista list nie zostala zaktualizowana to gra zostala zakonczona
            if (_currentGame.board == null)
            {
                var dispatcherTimer = (DispatcherTimer)sender;
                dispatcherTimer.Stop();
                _timer.Stop();
                _currentGame.inProgress = false;
                if (_countPawnImageW != 0 && _countPawnImageB != 0)
                    opponentNameLabel2.Content = "";
                GameOver();
                return;
            }

            //jezeli nie jest teraz nasz ruch to rysujemy na nowo pionki na planszy
            if (!_currentGame.turn.Equals(_accountLogin))
            {
                DrawPawns();
                if (_boardUpdated)
                    _boardUpdated = false;
            }

            //jezeli jest teraz nasz ruch to zmniejszamy czas i ewentualnie konczymy gre jesli czas sie skonczyl
            //rysujemy tez pionki w przypadku gdy plansza nie zostala zaktualizowana i aktualizujemy widgety
            if (_currentGame.turn.Equals(_accountLogin))
            {
                _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));

                timeRemainingLabel.Content = _remainingTime.ToString("mm':'ss");

                if (_remainingTime <= TimeSpan.Zero)
                {
                    if (_accountLogin.Equals(_currentGame.whitePawns))
                    {
                        var dispatcherTimer = (DispatcherTimer)sender;
                        dispatcherTimer.Stop();
                        _currentGame.inProgress = false;
                        if (_countPawnImageW != 0 && _countPawnImageB != 0)
                            opponentNameLabel2.Content = "";
                        _firebaseCrud.ClearGameRoomWhitePawns(_currentGame.host, _currentGame.blackPawns, _currentGame.password, _currentGame.roomName, _currentGame.board, _currentGame.turn);
                        _countPawnImageW = 0;
                    }
                    else if (_accountLogin.Equals(_currentGame.blackPawns))
                    {
                        var dispatcherTimer = (DispatcherTimer)sender;
                        dispatcherTimer.Stop();
                        _currentGame.inProgress = false;
                        if (_countPawnImageW != 0 && _countPawnImageB != 0)
                            opponentNameLabel2.Content = "";
                        _firebaseCrud.DeleteGameRoom(_accountLogin);
                        _countPawnImageB = 0;
                    }
                }

                if (!_boardUpdated)
                {
                    DrawPawns();
                    _boardUpdated = true;
                    GameLabelsUpdate();
                }
            }
        }

        private void CheckersGame() //funkcja wywolywana po rozpoczeciu gry
        {
            //gra rozpoczela sie wiec konczymy nasluchiwanie inProgress
            _timer2?.Stop();

            var gameRooms = _firebaseCrud.LoadGameRooms();
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
                if (!item.Key.Equals(_currentGame.host)) continue;
                
                _currentGame.host = item.Value.host;
                _currentGame.password = item.Value.password;
                _currentGame.roomName = item.Value.roomName;
                _currentGame.whitePawns = item.Value.whitePawns;
                _currentGame.blackPawns = item.Value.blackPawns;
                _currentGame.board = item.Value.board;
                _currentGame.turn = item.Value.turn;
                _currentGame.inProgress = item.Value.inProgress;
            }

            //pobranie danych o vr przeciwnika

            if (_firebaseCrud.LoadPlayers() == null) return;

            foreach (var item in _firebaseCrud.LoadPlayers())
            {
                if(item.Key.Equals(_currentGame.host))
                {
                    _enemyVR = item.Value.vr;
                    break;
                }
            }

            //ustawienie widgetow

            if (_currentGame.whitePawns.Equals(_accountLogin))
                opponentNameLabel2.Content = _currentGame.blackPawns;
            else
                opponentNameLabel2.Content = _currentGame.whitePawns;

            _remainingTime = TimeSpan.FromMinutes(5); //ustawienie ogolnego czasu na ruchy

            //uruchomienie zegara sprawdzającego czy polozenie pionkow w bazie zostalo zmienione
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_BoardUpdate;

            _timer.Start();

            //narysowanie pionkow na podstawie pocztakowych danych z bazy

            DrawPawns();
            GameLabelsUpdate();

        }

        private void DrawPawns() //funkcja rysujaca pionki na podstawie listy list przechowywanej w programie
        {
            //usuwam wszystkie pionki narysowane na planszy jesli jakies istnieja

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

            foreach (var boardRow in _currentGame.board)
            {
                int j = 0;

                foreach (string value in boardRow)
                {
                    if (!value.Equals("0"))
                    {
                        var image = new Image();

                        switch (value)
                        {
                            //wybieram obrazek
                            case "B":
                                {
                                    image.Source = new BitmapImage(new Uri(@"/images/blackPawn.png", UriKind.Relative));
                                    string imageName = $"pawnImageB_{b}";
                                    image.Name = imageName;

                                    if (_currentGame.blackPawns.Equals(_accountLogin))
                                    {
                                        image.MouseLeftButtonDown += ShowPawnPaths;
                                        image.Cursor = Cursors.Hand;
                                    }
                                    b--;
                                    break;
                                }
                            case "W":
                                {
                                    image.Source = new BitmapImage(new Uri(@"/images/whitePawn.png", UriKind.Relative));
                                    string imageName = $"pawnImageW_{w}";
                                    image.Name = imageName;
                                    if (_currentGame.whitePawns.Equals(_accountLogin))
                                    {
                                        image.MouseLeftButtonDown += ShowPawnPaths;
                                        image.Cursor = Cursors.Hand;
                                    }
                                    w++;
                                    break;
                                }
                            case "KB":
                                {
                                    image.Source = new BitmapImage(new Uri(@"/images/blackPawnKing.png", UriKind.Relative));
                                    string imageName = $"pawnImageKB_{kb}";
                                    image.Name = imageName;
                                    if (_currentGame.blackPawns.Equals(_accountLogin))
                                    {
                                        image.MouseLeftButtonDown += ShowPawnPaths;
                                        image.Cursor = Cursors.Hand;
                                    }
                                    kb--;
                                    break;
                                }
                            case "KW":
                                {
                                    image.Source = new BitmapImage(new Uri(@"/images/whitePawnKing.png", UriKind.Relative));
                                    string imageName = $"pawnImageKW_{kw}";
                                    image.Name = imageName;
                                    if (_currentGame.whitePawns.Equals(_accountLogin))
                                    {
                                        image.MouseLeftButtonDown += ShowPawnPaths;
                                        image.Cursor = Cursors.Hand;
                                    }
                                    kw++;
                                    break;
                                }
                        }

                        //rysuje obrazek
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
            }
        }

        private void ShowPawnPaths(object sender, MouseButtonEventArgs e) //funkcja pokazujaca sciezki dla kliknietego pionka
        {
            pathMediaPlayer.Play(); //wywolanie dzwieku po nacisnieciu na pionek

            if (sender is Image clickedImage && _currentGame.turn.Equals(_accountLogin) && _currentGame.inProgress)
            {
                //usuniecie wszystkich widocznych sciezek

                var imagesToRemove = gameGrid.Children.OfType<Image>().Where(img => img.Name != null && img.Name.StartsWith("pawnPath_"));

                foreach (var imageToRemove in imagesToRemove.ToList())
                {
                    gameGrid.Children.Remove(imageToRemove);
                }


                //pobranie polozenia pionka ktory zostal klikniety

                int targetRow = Grid.GetRow(clickedImage);
                int targetColumn = Grid.GetColumn(clickedImage);

                _selectedPawnImage = clickedImage;
                _selectedPawnName = _selectedPawnImage.Name;

                //---flagi dla pionka zwyklego
                //flagi dotyczace braku zablokowania sciezki poprzez inny pionek
                bool leftPathAvailable = true;
                bool rightPathAvailable = true;

                bool leftCaptureAvailable = true;
                bool rightCaptureAvailable = true;

                //flagi dotyczace zablokowania sciezki poprzez wyjscie poza plansze
                bool leftPathBlocked = false;
                bool rightPathBlocked = false;

                bool leftCaptureBlocked = false;
                bool rightCaptureBlocked = false;

                //---zmienne dla pionka krola
                //lista dotyczaca braku zablokowania sciezki poprzez inny pionek
                bool[] kingLeftUpPathAvailable = new bool[] { true, true, true, true, true, true };
                bool[] kingRightUpPathAvailable = new bool[] { true, true, true, true, true, true };

                bool[] kingLeftDownPathAvailable = new bool[] { true, true, true, true, true, true };
                bool[] kingRightDownPathAvailable = new bool[] { true, true, true, true, true, true };

                //jak daleko od pionka krola znajduje sie pierwszy przeciwnik
                int kingLeftUpCapture = 0;
                int kingRightUpCapture = 0;
                int kingLeftDownCapture = 0;
                int kingRightDownCapture = 0;

                //jak daleko od pionka krola znajduje sie pierwszy sojusznik
                int kingLeftUpAlly = 0;
                int kingRightUpAlly = 0;
                int kingLeftDownAlly = 0;
                int kingRightDownAlly = 0;

                //sprawdzenie zablokowania sciezek przez wyjscie poza plansze dla zwyklego pionka
                if (_currentGame.whitePawns.Equals(_accountLogin))
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
                else if (_currentGame.blackPawns.Equals(_accountLogin))
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


                //sprawdzenie zablokowania sciezek przez wyjscie poza plansze dla krola pionka
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
                foreach (object child in gameGrid.Children)
                {
                    if (child is Image pawnImage)
                    {
                        string pawnImageName = pawnImage.Name;

                        //pobranie polozenia pionka
                        int row = Grid.GetRow(pawnImage);
                        int column = Grid.GetColumn(pawnImage);

                        // jezeli sterujemy pionkami bialymi sprawdzamy czy jakis pionek znajduje sie nad kliknietym pionkiem po lewej i prawej
                        // jesli natomiast sterujemy czarnymi to patrzymy pod spodem

                        if (_currentGame.whitePawns.Equals(_accountLogin))
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
                        else if (_currentGame.blackPawns.Equals(_accountLogin))
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

                        //jezeli natomiast kliknietym pionkiem jest krol to aktualizujemy atrybuty ruchu krola
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
                
                //jezeli klikniety zostal krol
                if (clickedImage.Name.StartsWith("pawnImageK"))
                {
                    //ustawiamy false w kazdym miejscu ruchu krola oddalonym bardziej niz najblizszy sojusznik i najblizszy przeciwnik+1
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

                    for (int i = 1; i <= 6; i++)
                    {
                        if (kingLeftUpPathAvailable[i - 1])
                        {
                            var image = new Image();
                            image.Source = new BitmapImage(new Uri(@"/images/greyPawn.png", UriKind.Relative));
                            const string pathImageName = "pawnPath_K";
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
                        if (kingRightUpPathAvailable[i - 1])
                        {
                            var image = new Image();
                            image.Source = new BitmapImage(new Uri(@"/images/greyPawn.png", UriKind.Relative));
                            const string pathImageName = "pawnPath_K";
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
                        if (kingLeftDownPathAvailable[i - 1])
                        {
                            var image = new Image();
                            image.Source = new BitmapImage(new Uri(@"/images/greyPawn.png", UriKind.Relative));
                            const string pathImageName = "pawnPath_K";
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
                        if (kingRightDownPathAvailable[i - 1])
                        {
                            var image = new Image();
                            image.Source = new BitmapImage(new Uri(@"/images/greyPawn.png", UriKind.Relative));
                            const string pathImageName = "pawnPath_K";
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

                //jezeli klinkiety zostal zwykly pionek i jesli miejsce po prawej jest wolne
                if ((leftPathAvailable && !leftPathBlocked) || (leftCaptureAvailable && !leftCaptureBlocked))
                {
                    //dodaje obrazek imitujacy sciezke w odpowiednim miejscu

                    var image = new Image();
                    image.Source = new BitmapImage(new Uri(@"/images/greyPawn.png", UriKind.Relative));
                    const string pathImageName = "pawnPath_1";
                    image.Name = pathImageName;

                    Grid.SetRow(image, targetRow);
                    Grid.SetColumn(image, targetColumn);

                    if (_currentGame.whitePawns.Equals(_accountLogin))
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
                    else if (_currentGame.blackPawns.Equals(_accountLogin))
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

                //jezeli klinkiety zostal zwykly pionek i jesli miejsce po lewej jest wolne
                if ((rightPathAvailable && !rightPathBlocked) || (rightCaptureAvailable && !rightCaptureBlocked))
                {
                    //dodaje obrazek imitujacy sciezke w odpowiednim miejscu

                    var image = new Image();
                    image.Source = new BitmapImage(new Uri(@"/images/greyPawn.png", UriKind.Relative));
                    const string pathImageName = "pawnPath_2";
                    image.Name = pathImageName;


                    if (_currentGame.whitePawns.Equals(_accountLogin))
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
                    else if (_currentGame.blackPawns.Equals(_accountLogin))
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

        private void MovePawn(object sender, MouseButtonEventArgs e) //funkcja wywolywana po nacisnieciu na jakakolwiek sciezke
        {
            moveMediaPlayer.Play();
            if (sender is Image clickedPawnPath)
            {
                //pobieram dane o kliknietej sciezce

                int targetRow = Grid.GetRow(clickedPawnPath);
                int targetColumn = Grid.GetColumn(clickedPawnPath);

                int selectedPawnRow = Grid.GetRow(_selectedPawnImage);
                int selectedPawnColumn = Grid.GetColumn(_selectedPawnImage);

                //implementuje usuwanie zbitego pionka

                if (_selectedPawnImage.Name.StartsWith("pawnImageB"))
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
                else if (_selectedPawnImage.Name.StartsWith("pawnImageW"))
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
                else if (_selectedPawnImage.Name.StartsWith("pawnImageK"))
                {
                    if (Math.Abs(selectedPawnRow - targetRow) != 1 && Math.Abs(selectedPawnColumn - targetColumn) != 1)
                        //zaleznie od tego w ktora strone wykonany zostal ruch
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

                //pobieram obrazek rowny nazwie ruszanego pionka i zmieniam polozenie
                var foundImage = gameGrid.Children.OfType<Image>().FirstOrDefault(i => i.Name == _selectedPawnName);

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

                _currentGame.board.Clear();
                _currentGame.board = new List<List<string>>();

                for (int i = 0; i < 8; i++)
                {
                    var row = new List<string>(Enumerable.Repeat("0", 8));
                    _currentGame.board.Add(row);
                }

                //uzupelniam cala tablice odwzorowujaca plansze na podstawie pionkow umieszczonych na planszy

                foreach (object child in gameGrid.Children)
                {
                    if (child is Image image)
                    {
                        int row = Grid.GetRow(image);
                        int column = Grid.GetColumn(image);

                        if (image.Name.StartsWith("pawnImageW_"))
                        {
                            if (row == 1)
                                _currentGame.board[row - 1][column - 1] = "KW";
                            else
                                _currentGame.board[row - 1][column - 1] = "W";
                        }
                        else if (image.Name.StartsWith("pawnImageB_"))
                        {
                            if (row == 8)
                                _currentGame.board[row - 1][column - 1] = "KB";
                            else
                                _currentGame.board[row - 1][column - 1] = "B";

                        }
                        else if (image.Name.StartsWith("pawnImageKW_"))
                        {
                            _currentGame.board[row - 1][column - 1] = "KW";
                        }
                        else if (image.Name.StartsWith("pawnImageKB_"))
                        {
                            _currentGame.board[row - 1][column - 1] = "KB";
                        }

                    }
                }

                //aktualizuje dane o planszy przechowywane w bazie danych

                _firebaseCrud.UpdateGameRoomBoard(_currentGame.host, _currentGame.board);
                if (_currentGame.whitePawns.Equals(_accountLogin))
                {
                    _firebaseCrud.UpdateGameRoomTurn(_currentGame.host, _currentGame.blackPawns);
                    _currentGame.turn = _currentGame.blackPawns;
                }
                else
                {
                    _firebaseCrud.UpdateGameRoomTurn(_currentGame.host, _currentGame.whitePawns);
                    _currentGame.turn = _currentGame.whitePawns;
                }

                GameLabelsUpdate();

            }
        }

        private void GameLabelsUpdate() //aktualizacja widgetow podczas gry
        {
            _countPawnImageB = 0;
            _countPawnImageW = 0;
            _countPawnImageKB = 0;
            _countPawnImageKW = 0;

            if (!_currentGame.turn.Equals(_accountLogin))
                whoseTurnLabel.Content = "YOUR OPPONENT's TURN";
            else
                whoseTurnLabel.Content = "YOUR TURN";


            foreach (object child in gameGrid.Children)
            {
                if (child is Image image)
                {
                    if (image.Name.StartsWith("pawnImageB"))
                    {
                        _countPawnImageB++;
                    }
                    else if (image.Name.StartsWith("pawnImageKB"))
                    {
                        _countPawnImageB++;
                        _countPawnImageKB++;
                    }
                    else if (image.Name.StartsWith("pawnImageW"))
                    {
                        _countPawnImageW++;
                    }
                    else if (image.Name.StartsWith("pawnImageKW"))
                    {
                        _countPawnImageW++;
                        _countPawnImageKW++;
                    }
                }
            }

            numberOfBlackPawnsCaptured.Content = 12 - _countPawnImageB;
            numberOfWhitePawnsCaptured.Content = 12 - _countPawnImageW;

            if(_countPawnImageB == 0 || _countPawnImageW == 0 || (_countPawnImageKB==1 && _countPawnImageKW==1 && _countPawnImageB == 1 && _countPawnImageW == 1))
            {
                _timer.Stop();
                _currentGame.inProgress = false;
                if (_countPawnImageW != 0 && _countPawnImageB != 0)
                    opponentNameLabel2.Content = "";
                GameOver();
            }

        }

        private void MainWindow_Closing(object sender, CancelEventArgs e) //funkcja wywolywana przy zamykaniu okna
        {
            if (_loggedOut) return;

            if (_firebaseCrud.LogoutUser() != null) return;
            
            _firebaseCrud.UpdateData(_accountName, _accountLogin, _accountEmail, _accountAge, false, _accountInGame, _accountVR);
            _firebaseCrud.DeleteGameRoom(_currentGame.host);
        }

        private void GameOver() //funkcja wywolywana gdy gra zostaje zakonczona
        {
            int extraVR = (_enemyVR - _accountVR) / 100; //ekstra punkty naliczane na podstawie punktow vr przeciwnika

            if(mainMenuPanel.Visibility == Visibility.Hidden) //efekt zjezdzania widgetow gry
            {
                var db = new DoubleAnimation();
                db.From = 0;
                db.To = 600;
                db.Duration = TimeSpan.FromSeconds(2);

                db.Completed += (sender, e) =>
                {
                    gameWidgetsGrid.Visibility = Visibility.Hidden;
                    var reverseDb = new DoubleAnimation();
                    reverseDb.From = 600;
                    reverseDb.To = 0;
                    reverseDb.Duration = TimeSpan.FromSeconds(2);
                    gameWidgetsGridTransform.BeginAnimation(TranslateTransform.YProperty, reverseDb);
                };

                gameWidgetsGridTransform.BeginAnimation(TranslateTransform.YProperty, db);
            }


            if (opponentNameLabel2.Content.Equals("")&&(_countPawnImageKB == 1 && _countPawnImageKW == 1 && _countPawnImageB == 1 && _countPawnImageW == 1))
            {
                //wjechanie z gory panelu z informacja o remisie
                winMediaPlayer.Play();
                var db2 = new DoubleAnimation();
                db2.From = -600;
                db2.To = 0;
                db2.Duration = TimeSpan.FromSeconds(2);
                drawGridTransform.BeginAnimation(TranslateTransform.YProperty, db2);
                drawGrid.Visibility = Visibility.Visible;
            }
            else if (opponentNameLabel2.Content.Equals("") && !(_countPawnImageKB == 1 && _countPawnImageKW == 1 && _countPawnImageB == 1 && _countPawnImageW == 1))
            {
                //wjechanie z gory panelu z informacja o wygranej
                winMediaPlayer.Play();
                _accountVR += 8 + extraVR;
                _firebaseCrud.UpdateData(_accountName, _accountLogin, _accountEmail, _accountAge, true, _accountInGame, _accountVR);
                var db2 = new DoubleAnimation();
                db2.From = -600;
                db2.To = 0;
                db2.Duration = TimeSpan.FromSeconds(2);
                opponentLeftGridTransform.BeginAnimation(TranslateTransform.YProperty, db2);
                opponentLeftGrid.Visibility = Visibility.Visible;
            }
            else if(!opponentNameLabel2.Content.Equals("")&&_accountLogin.Equals(_currentGame.whitePawns) && _countPawnImageW == 0)
            {
                //wjechanie z gory panelu z informacja o przegranej
                loseMediaPlayer.Play();
                _accountVR -= 8 - extraVR;
                _firebaseCrud.UpdateData(_accountName, _accountLogin, _accountEmail, _accountAge, true, _accountInGame, _accountVR);
                var db2 = new DoubleAnimation();
                db2.From = -600;
                db2.To = 0;
                db2.Duration = TimeSpan.FromSeconds(2);
                youLostGridTransform.BeginAnimation(TranslateTransform.YProperty, db2);
                youLostGrid.Visibility = Visibility.Visible;
            }
            else if (!opponentNameLabel2.Content.Equals("") && _accountLogin.Equals(_currentGame.blackPawns) && _countPawnImageB == 0)
            {
                //wjechanie z gory panelu z informacja o przegranej
                loseMediaPlayer.Play();
                _accountVR -= 8 - extraVR;
                _firebaseCrud.UpdateData(_accountName, _accountLogin, _accountEmail, _accountAge, true, _accountInGame, _accountVR);
                var db2 = new DoubleAnimation();
                db2.From = -600;
                db2.To = 0;
                db2.Duration = TimeSpan.FromSeconds(2);
                youLostGridTransform.BeginAnimation(TranslateTransform.YProperty, db2);
                youLostGrid.Visibility = Visibility.Visible;
            }
            else if (!opponentNameLabel2.Content.Equals("") && _accountLogin.Equals(_currentGame.whitePawns) && _countPawnImageB == 0)
            {
                //wjechanie z gory panelu z informacja o wygranej
                winMediaPlayer.Play();
                _accountVR += 8 + extraVR;
                _firebaseCrud.UpdateData(_accountName, _accountLogin, _accountEmail, _accountAge, true, _accountInGame, _accountVR);
                var db2 = new DoubleAnimation();
                db2.From = -600;
                db2.To = 0;
                db2.Duration = TimeSpan.FromSeconds(2);
                youWonGridTransform.BeginAnimation(TranslateTransform.YProperty, db2);
                youWonGrid.Visibility = Visibility.Visible;
            }
            else if (!opponentNameLabel2.Content.Equals("") && _accountLogin.Equals(_currentGame.blackPawns) && _countPawnImageW == 0)
            {
                //wjechanie z gory panelu z informacja o wygranej
                winMediaPlayer.Play();
                _accountVR += 8 + extraVR;
                _firebaseCrud.UpdateData(_accountName, _accountLogin, _accountEmail, _accountAge, true, _accountInGame, _accountVR);
                var db2 = new DoubleAnimation();
                db2.From = -600;
                db2.To = 0;
                db2.Duration = TimeSpan.FromSeconds(2);
                youWonGridTransform.BeginAnimation(TranslateTransform.YProperty, db2);
                youWonGrid.Visibility = Visibility.Visible;
            }
        }

        //funkcje wywolywane po nacisnieciu odpowiedniego guzika

        private void quitGameButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            gameGrid.Visibility = Visibility.Hidden;
            mainMenuPanel.Visibility = Visibility.Visible;
            _firebaseCrud.DeleteGameRoom(_currentGame.host);
            VRValueLabel.Content = _accountVR;

            if (_accountLogin.Equals(_currentGame.whitePawns))
            {
                _countPawnImageW = 0;
            }
            else if (_accountLogin.Equals(_currentGame.blackPawns))
            {
                _countPawnImageB = 0;
            }
        }

        private void multiplayerButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            mainMenuPanel.Visibility = Visibility.Hidden;
            multiplayerPanel.Visibility = Visibility.Visible;
        }

        private void quitMultiplayerButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            multiplayerPanel.Visibility = Visibility.Hidden;
            mainMenuPanel.Visibility = Visibility.Visible;
        }

        private void logoutButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            if (_firebaseCrud.LogoutUser() == null)
            {
                _loggedOut = true;
                _firebaseCrud.UpdateData(_accountName, _accountLogin, _accountEmail, _accountAge, false, _accountInGame, _accountVR);
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                Close();
            }
        }

        private void joinLobbyButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            gameRoomsListBox.Items.Clear();
            gameRoomsGrid.Visibility = Visibility.Visible;
            multiplayerPanel.Visibility = Visibility.Hidden;

            var gameRooms = _firebaseCrud.LoadGameRooms();
            if (gameRooms == null)
                return;

            foreach (var item in gameRooms)
            {
                gameRoomsListBox.Items.Add(item.Value.roomName);
            }
        }

        private void statisticsButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            rankingListBox.ItemsSource = null;
            statisticsGrid.Visibility = Visibility.Visible;
            multiplayerPanel.Visibility = Visibility.Hidden;

            if (_firebaseCrud.LoadPlayers() == null) return;

            playerRankingData = new ObservableCollection<PlayerRankingData>();

            foreach (var item in _firebaseCrud.LoadPlayers())
            {
                string imagePath;

                if (item.Value.online)
                    imagePath = @"/images/online.png";
                else
                    imagePath = @"/images/offline.png";

                playerRankingData.Add(new PlayerRankingData
                {
                    login = item.Value.login,
                    vr = item.Value.vr,
                    statusImagePath = imagePath
                });
            }
            var sortedPlayerRankingData = playerRankingData.OrderByDescending(player => player.vr).ToList();

            rankingListBox.ItemsSource = new ObservableCollection<PlayerRankingData>(sortedPlayerRankingData);
        }

        private void createLobbyButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
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

            var gameRoom = _firebaseCrud.CreateGameRoom(_accountLogin);
            roomNameTextBox.Text = gameRoom.roomName;
            _currentGame.roomName = gameRoom.roomName;
            roomPasswordTextBox.Text = gameRoom.password;
            _currentGame.host = _accountLogin;
            waitingForOpponentsLabel.Content = "Waiting for opponents...";
            saveRoomInfoButton.IsEnabled = true;
            startGameButton.IsEnabled = true;
            roomNameTextBox.IsEnabled = true;
            roomPasswordTextBox.IsEnabled = true;
            opponentNameLabel.Content = "";
            startGameButton.IsEnabled = false;

            OpponentListenerAsync(_accountLogin);
        }

        private void quitLobbyCreation_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            multiplayerPanel.Visibility = Visibility.Visible;
            createLobbyGrid.Visibility = Visibility.Hidden;

            if (_currentGame.roomName == null) return;

            if (_accountLogin.Equals(_currentGame.host))
            {
                _firebaseCrud.DeleteGameRoom(_accountLogin);
                _response1.Dispose();
            }
            else
                _firebaseCrud.ClearGameRoomWhitePawns(_currentGame.host, _currentGame.blackPawns, _currentGame.password, _currentGame.roomName, _currentGame.board, _currentGame.turn);

        }

        private void saveRoomInfoButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            _firebaseCrud.UpdateGameRoom(_accountLogin, roomPasswordTextBox.Text, roomNameTextBox.Text, opponentNameLabel.Content.ToString(), false);
        }

        private void quitGameRoomsButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            gameRoomsGrid.Visibility = Visibility.Hidden;
            multiplayerPanel.Visibility = Visibility.Visible;
        }

        private void joinGameRoomButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();

            if (gameRoomsListBox.SelectedItem == null) return;

            roomPasswordGrid.Visibility = Visibility.Visible;
            gameRoomsGrid.Visibility = Visibility.Hidden;
        }

        private void quitRoomPasswordGridButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            roomPasswordGrid.Visibility = Visibility.Hidden;
            gameRoomsGrid.Visibility = Visibility.Visible;
        }

        private void checkGameRoomPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            var gameRooms = _firebaseCrud.LoadGameRooms();
            if (gameRooms == null)
                return;

            foreach (var item in gameRooms)
            {
                if (!item.Value.roomName.Equals(gameRoomsListBox.SelectedItem.ToString())) continue;

                if (item.Value.password.Equals(enterRoomPasswordTextBox.Text))
                {
                    if (item.Value.whitePawns.Equals(""))
                    {
                        Console.WriteLine(@"Valid password");
                        _firebaseCrud.UpdateGameRoomOpponent(item.Value.blackPawns, item.Value.password, item.Value.roomName, _accountLogin);
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
                        _currentGame.host = item.Value.host;
                        _currentGame.password = item.Value.password;
                        _currentGame.roomName = item.Value.roomName;
                        _currentGame.whitePawns = item.Value.whitePawns;
                        _currentGame.blackPawns = item.Value.blackPawns;
                        _currentGame.board = item.Value.board;
                        _currentGame.turn = item.Value.turn;
                        _currentGame.inProgress = item.Value.inProgress;


                        opponentNameLabel.Content = item.Value.blackPawns;
                        waitingForOpponentsLabel.Content = "Waiting for room host to start the game....";
                        saveRoomInfoButton.IsEnabled = false;
                        startGameButton.IsEnabled = false;

                        GameStartedListenerAsync(item.Value.host);

                        _timer2 = new DispatcherTimer();
                        _timer2.Interval = TimeSpan.FromSeconds(2);
                        _timer2.Tick += Timer_GameRoomExisting;

                        _timer2.Start();
                    }
                    else
                    {
                        Console.WriteLine(@"Room full");
                    }
                }
                else
                    Console.WriteLine(@"Invalid password");
                return;
            }

            Console.WriteLine(@"Game room not existing");
        }

        private void startGameButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            _firebaseCrud.UpdateGameRoom(_accountLogin, roomPasswordTextBox.Text, roomNameTextBox.Text, opponentNameLabel.Content.ToString(), true);
            createLobbyGrid.Visibility = Visibility.Hidden;
            gameGrid.Visibility = Visibility.Visible;
            CheckersGame();
        }

        private void accountButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            mainMenuPanel.Visibility = Visibility.Hidden;
            accountGrid.Visibility = Visibility.Visible;
        }

        private void quitAccountPanelButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            mainMenuPanel.Visibility = Visibility.Visible;
            accountGrid.Visibility = Visibility.Hidden;
        }

        private void changePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            passwordChangeGrid.Visibility = Visibility.Visible;
            accountGrid.Visibility = Visibility.Hidden;
        }

        private void quitPasswordChangeGridButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            passwordChangeGrid.Visibility = Visibility.Hidden;
            accountGrid.Visibility = Visibility.Visible;
        }

        private void confirmPasswordChangeButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            _firebaseCrud.UpdateUserPassword(newPasswordTextBox.Text);
        }

        private void deleteAccountButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            accountDeleteGrid.Visibility = Visibility.Visible;
            accountGrid.Visibility = Visibility.Hidden;
        }

        private void quitAccountDeleteGridButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            accountDeleteGrid.Visibility = Visibility.Hidden;
            accountGrid.Visibility = Visibility.Visible;
        }

        private void confirmAccountDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            _loggedOut = true;
            _firebaseCrud.DeleteUser();
            _firebaseCrud.DeleteData(_accountLogin);
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }

        private void quitStatisticsGridButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            statisticsGrid.Visibility = Visibility.Hidden;
            multiplayerPanel.Visibility = Visibility.Visible;
        }

        private void creditsButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            mainMenuPanel.Visibility = Visibility.Hidden;
            creditsGrid.Visibility = Visibility.Visible;
        }

        private void quitCreditsPanelButton_Click(object sender, RoutedEventArgs e)
        {
            clickMediaPlayer.Play();
            mainMenuPanel.Visibility = Visibility.Visible;
            creditsGrid.Visibility = Visibility.Hidden;
        }

        private void rankingListBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
