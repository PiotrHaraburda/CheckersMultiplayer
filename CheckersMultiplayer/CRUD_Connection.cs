using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Auth.Providers;
using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;

namespace CheckersMultiplayer
{
    class CRUD_Connection
    {
        //firebase connection Settings
        public IFirebaseConfig fc = new FirebaseConfig()
        {
            AuthSecret = "kD2SaWj1hzgdv5HPdVLUwNDIzyDUpRsWCtNoqZWh",
            BasePath = "https://checkersmultiplayer-ea56b-default-rtdb.europe-west1.firebasedatabase.app/"
        };

        public FirebaseAuthConfig config = new FirebaseAuthConfig
        {
            ApiKey = "AIzaSyD8jC0nVSHwS_y4M-BXRfJyZXbke6Lfeec",
            AuthDomain = "checkersmultiplayer-ea56b.firebaseapp.com",
            Providers = new FirebaseAuthProvider[]
            {
                new EmailProvider()
            }
        };

        public IFirebaseClient client;
        public FirebaseAuthClient authClient;
        //Code to warn console if class cannot connect when called.
        public CRUD_Connection()
        {
            try
            {
                client = new FirebaseClient(fc);
                authClient = new FirebaseAuthClient(config);

            }
            catch (Exception)
            {
                Console.WriteLine("database connection error");
            }
        }
    }
}
