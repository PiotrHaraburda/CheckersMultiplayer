using System;
using Firebase.Auth;
using Firebase.Auth.Providers;
using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;

namespace CheckersMultiplayer.scripts
{
    internal class FirebaseConnection
    {
        //firebase connection Settings
        private readonly IFirebaseConfig _firebaseConfig = new FirebaseConfig()
        {
            AuthSecret = "kD2SaWj1hzgdv5HPdVLUwNDIzyDUpRsWCtNoqZWh",
            BasePath = "https://checkersmultiplayer-ea56b-default-rtdb.europe-west1.firebasedatabase.app/"
        };

        private readonly FirebaseAuthConfig _firebaseAuthConfig = new FirebaseAuthConfig
        {
            ApiKey = "AIzaSyD8jC0nVSHwS_y4M-BXRfJyZXbke6Lfeec",
            AuthDomain = "checkersmultiplayer-ea56b.firebaseapp.com",
            Providers = new FirebaseAuthProvider[]
            {
                new EmailProvider()
            }
        };

        public readonly IFirebaseClient Client;
        public readonly FirebaseAuthClient AuthClient;
        //Code to warn console if class cannot connect when called.
        public FirebaseConnection()
        {
            try
            {
                Client = new FirebaseClient(_firebaseConfig);
                AuthClient = new FirebaseAuthClient(_firebaseAuthConfig);

            }
            catch (Exception)
            {
                Console.WriteLine(@"Database connection error");
            }
        }
    }
}
