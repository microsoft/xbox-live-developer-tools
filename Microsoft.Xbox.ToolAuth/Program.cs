using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xbox.ToolAuth.Win32.Csharp;
using System.Windows;
using System.Windows.Controls;

namespace ToolAuthSample
{
    class Program
    {
        static ToolAuth m_toolAuth;

        [STAThread]
        static void Main(string[] args)
        {
            m_toolAuth = new ToolAuth();
            Console.WriteLine("1- Get EToken(DevCenter), 2- Get EToken(XDP), 3- Exit\n");

            while (true)
            {
                var inputKey = Console.ReadKey();
                Console.WriteLine();
                switch (inputKey.Key)
                {
                    case ConsoleKey.D1:
                        GetToken(true);
                        break;

                    case ConsoleKey.D2:
                        GetToken(false);
                        break;

                    case ConsoleKey.D3:
                        return;

                    default:
                        Console.WriteLine("Key not recognized");
                        break;
                }
            }
        }

        static async void GetToken(bool isDevCenter)
        {
            try
            {
                if(isDevCenter)
                {
                    var eToken = await GetDevCenterEToken();
                    Console.WriteLine("Token: " + eToken);
                }
                else
                {
                    var eToken = GetXDPEToken();
                    Console.WriteLine("Token: " + eToken);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [STAThread]
        static string GetXDPEToken()
        {
            try
            {
                return m_toolAuth.GetETokenSilently();
            }
            catch (Exception)
            {
                Console.WriteLine("Requesting Token...\n");

                return m_toolAuth.GetEToken();
            }
        }

        static async Task<string> GetDevCenterEToken()
        {
            try
            {
                return m_toolAuth.GetETokenSilently();
            }
            catch (Exception)
            {
                string userName;
                string tenant;
                string password;

                Console.WriteLine("Enter Tenant: ");
                tenant = Console.ReadLine();

                Console.WriteLine("Enter User Name: ");
                userName = Console.ReadLine();

                Console.WriteLine("Enter Password: ");
                password = Console.ReadLine();

                Console.WriteLine("Requesting Token...\n");

                return await m_toolAuth.GetEToken(tenant, userName, password);
            }
        }
    }
}
