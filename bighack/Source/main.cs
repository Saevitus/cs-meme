using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using SteamKit2;

namespace programMain
{
    public struct accInfo
    {
        static public string user, pass;
        static public string authCode;
    }

    public class mainClass
    {
        static SteamClient steamClient;
        static SteamUser steamUser;
        static CallbackManager callbackManager;
        static SteamFriends friends;

        static bool running;

        static void OnConnected(SteamClient.ConnectedCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("Error: {0}", callback.Result);
                running = false;
                return;
            }
            else
            {
                Console.WriteLine("Logging into steam with user: {0}", accInfo.user);

                byte[] sentryHash = null;

                if (File.Exists("abc123.sentry"))
                {
                    byte[] sentryFile = File.ReadAllBytes("abc123.sentry");
                    sentryHash = CryptoHelper.SHAHash(sentryFile);             
                }

                steamUser.LogOn(new SteamUser.LogOnDetails
                {
                    Username = accInfo.user,
                    Password = accInfo.pass,
                    AuthCode = accInfo.authCode,
                    SentryFileHash = sentryHash,
                });

            }
        }

        static void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            Thread.Sleep(TimeSpan.FromSeconds(5));
            steamClient.Connect();
        }

        static void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            bool steamGuard = callback.Result == EResult.AccountLogonDenied;
            if (steamGuard)
            {
                Console.Write("Enter steam authcode: ");
                accInfo.authCode = Console.ReadLine();
                return;
            }

            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("Can't logon to steam: {0} | {1}", callback.Result, callback.ExtendedResult);
                Thread.Sleep(TimeSpan.FromSeconds(5));
                running = false;
                return;
            }

            Console.WriteLine("Logged on.");
        }

        static void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Console.WriteLine("Logged off: {0}", callback.Result);
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }

        static void OnAuth(SteamUser.UpdateMachineAuthCallback callback)
        {
            Console.WriteLine("test");

            int size;
            byte[] sentryHash;

            using (var fs = File.Open("abc123.sentry", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                fs.Seek(callback.Offset, SeekOrigin.Begin);
                fs.Write(callback.Data, 0, callback.BytesToWrite);
                size = (int)fs.Length;
                fs.Seek(0, SeekOrigin.Begin);

                using (var sha = new SHA1CryptoServiceProvider())
                    sentryHash = sha.ComputeHash(fs);

                steamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails
                {
                    JobID = callback.JobID,
                    FileName = callback.FileName,
                    BytesWritten = callback.BytesToWrite,
                    FileSize = size,
                    Offset = callback.Offset,
                    Result = EResult.OK,
                    LastError = 0,
                    OneTimePassword = callback.OneTimePassword,
                    SentryFileHash = sentryHash,
                });
                Console.WriteLine("Done.");
            }
        }

        static void OnAccountInfo(SteamUser.AccountInfoCallback callback)
        {
            
            string meme = friends.GetPersonaName();
            Console.WriteLine("{0}", meme);
        }

        static void Main(string[] args)
        {

            Console.Write("Enter username: ");
            accInfo.user = Console.ReadLine();

            Console.Write("Enter password: ");
            accInfo.pass = Console.ReadLine();

            steamClient = new SteamClient();
            callbackManager = new CallbackManager(steamClient);
            steamUser = steamClient.GetHandler<SteamUser>();
            friends = steamClient.GetHandler<SteamFriends>();

            callbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            callbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
            callbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            callbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
            callbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnAuth);
            callbackManager.Subscribe<SteamUser.AccountInfoCallback>(OnAccountInfo);

            running = true;

            Console.WriteLine("Connecting.");

            steamClient.Connect();

            while (running)
            {
                callbackManager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }

        }

    }

}
