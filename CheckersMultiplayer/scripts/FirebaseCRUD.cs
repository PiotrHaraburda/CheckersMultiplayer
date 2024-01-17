using System;
using System.Collections.Generic;
using System.Net.Http;
using Firebase.Auth;
using Newtonsoft.Json;

namespace CheckersMultiplayer.scripts
{
    internal class FirebaseCrud
    {
        private readonly FirebaseConnection _firebaseConnection = new FirebaseConnection();

        public bool RegisterUser(string email,string password)
        {
            try
            {
                var registerTask = _firebaseConnection.AuthClient.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task => {
                    if (task.IsCanceled)
                    {
                        Console.WriteLine(@"CreateUserWithEmailAndPasswordAsync was canceled.");
                    }
                    if (task.IsFaulted)
                    {
                        Console.WriteLine(@"CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                    }

                    var result = task.Result;
                    Console.WriteLine(@"Firebase user created successfully:" +result.User.Uid);
                });

                registerTask.Wait();
                return true;
            }
            catch(HttpRequestException)
            {
                return false;
            }
            catch(FirebaseAuthHttpException)
            {
                return false;
            }
            catch (AggregateException)
            {
                return false;
            }
        }


        public bool LoginUser(string email, string password)
        {
            try
            {
                var loginTask = _firebaseConnection.AuthClient.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task => {
                    if (task.IsCanceled)
                    {
                        Console.WriteLine(@"SignInWithEmailAndPasswordAsync was canceled.");
                    }
                    if (task.IsFaulted)
                    {
                        Console.WriteLine(@"SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                    }

                    var result = task.Result;

                    Console.WriteLine(@"User signed in successfully: " + result.User.Uid);
                });

                loginTask.Wait();
                return true;
            }
            catch(HttpRequestException)
            {
                return false;
            }
            catch(FirebaseAuthHttpException)
            {
                return false;
            }
            catch (AggregateException)
            {
                return false;
            }
        }

        public void DeleteUser()
        {
            var user = _firebaseConnection.AuthClient.User;
            user?.DeleteAsync().ContinueWith(task => {
                if (task.IsCanceled)
                {
                    Console.WriteLine(@"DeleteAsync was canceled.");
                    return;
                }
                if (task.IsFaulted)
                {
                    Console.WriteLine(@"DeleteAsync encountered an error: " + task.Exception);
                    return;
                }

                Console.WriteLine(@"User deleted successfully.");
            });

        }

        public User LogoutUser()
        {
            _firebaseConnection.AuthClient.SignOut();

            return _firebaseConnection.AuthClient.User;
        }

        public void UpdateUserPassword(string newPassword)
        {
            var user = _firebaseConnection.AuthClient.User;
            
            user?.ChangePasswordAsync(newPassword).ContinueWith(task => {
                if (task.IsCanceled)
                {
                    Console.WriteLine(@"UpdatePasswordAsync was canceled.");
                    return;
                }
                if (task.IsFaulted)
                {
                    Console.WriteLine(@"UpdatePasswordAsync encountered an error: " + task.Exception);
                    return;
                }

                Console.WriteLine(@"Password updated successfully.");
            });
        }

        public void SetData(string name, string login, string email, int age, bool online, bool inGame, int vr)
        {
            try
            {
                var set = new Players()
                {
                    name = name,
                    login = login,
                    email = email,
                    age = age,
                    online = online,
                    inGame = inGame,
                    vr = vr
                };
                _firebaseConnection.Client.Set("players/" + login, set);
            }
            catch (Exception)
            {
                Console.WriteLine(@"error");
            }

        }

        public GameRooms CreateGameRoom(string login)
        {
            try
            {
                var set = new GameRooms()
                {
                    host = login,
                    blackPawns = login,
                    whitePawns = "",
                    password = "",
                    roomName= login+"'s room",
                    board = new List<List<string>>
                    {
                        new List<string> { "0", "B", "0", "B", "0", "B", "0", "B" },
                        new List<string> { "B", "0", "B", "0", "B", "0", "B", "0" },
                        new List<string> { "0", "B", "0", "B", "0", "B", "0", "B" },
                        new List<string> { "0", "0", "0", "0", "0", "0", "0", "0" },
                        new List<string> { "0", "0", "0", "0", "0", "0", "0", "0" },
                        new List<string> { "W", "0", "W", "0", "W", "0", "W", "0" },
                        new List<string> { "0", "W", "0", "W", "0", "W", "0", "W" },
                        new List<string> { "W", "0", "W", "0", "W", "0", "W", "0" }
                    },
                    turn = login
                };
                _firebaseConnection.Client.Set("gameRooms/" + login, set);

                return set;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Error: {ex.Message}");
                return null;
            }
        }

        public void UpdateGameRoom(string login,string password, string roomName,string whitePawns, bool inProgress)
        {
            try
            {
                var set = new GameRooms()
                {
                    host = login,
                    blackPawns = login,
                    whitePawns = whitePawns,
                    password = password,
                    roomName = roomName,
                    board = new List<List<string>>
                    {
                        new List<string> { "0", "B", "0", "B", "0", "B", "0", "B" },
                        new List<string> { "B", "0", "B", "0", "B", "0", "B", "0" },
                        new List<string> { "0", "B", "0", "B", "0", "B", "0", "B" },
                        new List<string> { "0", "0", "0", "0", "0", "0", "0", "0" },
                        new List<string> { "0", "0", "0", "0", "0", "0", "0", "0" },
                        new List<string> { "W", "0", "W", "0", "W", "0", "W", "0" },
                        new List<string> { "0", "W", "0", "W", "0", "W", "0", "W" },
                        new List<string> { "W", "0", "W", "0", "W", "0", "W", "0" }
                    },
                    inProgress=inProgress,
                    turn = login
                };
                _firebaseConnection.Client.Update("gameRooms/" + login, set);
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Error: {ex.Message}");
            }
        }

        public void UpdateGameRoomBoard(string login, List<List<string>> newBoard)
        {
            try
            {
                _firebaseConnection.Client.Update("gameRooms/" + login, new
                {
                    board = newBoard
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Error: {ex.Message}");
            }
        }

        public void UpdateGameRoomTurn(string login, string newTurn)
        {
            try
            {
                _firebaseConnection.Client.Update("gameRooms/" + login, new
                {
                    turn = newTurn
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Error: {ex.Message}");
            }
        }

        public void ClearGameRoomWhitePawns(string host,string blackPawns, string password, string roomName, List<List<string>> board, string turn)
        {
            try
            {
                var set = new GameRooms()
                {
                    host = host,
                    blackPawns = blackPawns,
                    whitePawns = "",
                    password = password,
                    roomName = roomName,
                    board = board,
                    turn = turn
                };
                _firebaseConnection.Client.Update("gameRooms/" + host, set);
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Error: {ex.Message}");
            }
        }


        public void UpdateGameRoomOpponent(string login, string password, string roomName,string opponent)
        {
            try
            {
                GameRooms set = new GameRooms()
                {
                    host = login,
                    blackPawns = login,
                    whitePawns = opponent,
                    password = password,
                    roomName = roomName,
                    board = new List<List<string>>
                    {
                        new List<string> { "0", "B", "0", "B", "0", "B", "0", "B" },
                        new List<string> { "B", "0", "B", "0", "B", "0", "B", "0" },
                        new List<string> { "0", "B", "0", "B", "0", "B", "0", "B" },
                        new List<string> { "0", "0", "0", "0", "0", "0", "0", "0" },
                        new List<string> { "0", "0", "0", "0", "0", "0", "0", "0" },
                        new List<string> { "W", "0", "W", "0", "W", "0", "W", "0" },
                        new List<string> { "0", "W", "0", "W", "0", "W", "0", "W" },
                        new List<string> { "W", "0", "W", "0", "W", "0", "W", "0" }
                    },
                    turn = login
                };
                _firebaseConnection.Client.Update("gameRooms/" + login, set);
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Error: {ex.Message}");
            }
        }

        public void DeleteGameRoom(string accountLogin)
        {
            try
            {
                _firebaseConnection.Client.Delete("gameRooms/" + accountLogin);
            }
            catch (Exception)
            {
                Console.WriteLine(@"error");
            }
        }

        public void UpdateData(string name, string login, string email, int age, bool online, bool inGame, int vr)
        {
            try
            {
                var set = new Players()
                {
                    name = name,
                    login = login,
                    email = email,
                    age = age,
                    online = online,
                    inGame = inGame,
                    vr = vr
                };
                _firebaseConnection.Client.Update("players/" + login, set);
            }
            catch (Exception)
            {
                Console.WriteLine(@"error");
            }
        }

        public void DeleteData(string login)
        {
            try
            {
                _firebaseConnection.Client.Delete("players/" + login);
            }
            catch (Exception)
            {
                Console.WriteLine(@"error");
            }
        }

        public Dictionary<string, Players> LoadPlayers()
        {
            try
            {
                var firebasePlayersResponse = _firebaseConnection.Client.Get("players");
                var listData = JsonConvert.DeserializeObject<Dictionary<string, Players>>(firebasePlayersResponse.Body);
                return listData;
            }
            catch (Exception)
            {
                Console.WriteLine(@"error");
                return null;
            }
        }

        public Dictionary<string, GameRooms> LoadGameRooms()
        {
            try
            {
                var firebaseGameRoomsResponse = _firebaseConnection.Client.Get("gameRooms");
                string json = firebaseGameRoomsResponse.Body;

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                var gameRooms = JsonConvert.DeserializeObject<Dictionary<string, GameRooms>>(json, settings);

                return gameRooms;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Error: {ex.Message}");
                return null;
            }
        }


    }
}
