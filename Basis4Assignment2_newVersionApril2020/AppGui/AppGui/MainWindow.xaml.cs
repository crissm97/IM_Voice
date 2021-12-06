using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;
using mmisharp;
using Newtonsoft.Json;

namespace AppGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MmiCommunication mmiC;

        //  new 16 april 2020
        private MmiCommunication mmiSender;
        private LifeCycleEvents lce;
        private MmiCommunication mmic;
        private string game = "";

        public MainWindow()
        {
            InitializeComponent();


            mmiC = new MmiCommunication("localhost",8000, "User1", "GUI");
            mmiC.Message += MmiC_Message;
            mmiC.Start();

            // NEW 16 april 2020
            //init LifeCycleEvents..
            lce = new LifeCycleEvents("APP", "TTS", "User1", "na", "command"); // LifeCycleEvents(string source, string target, string id, string medium, string mode
            // MmiCommunication(string IMhost, int portIM, string UserOD, string thisModalityName)
            mmic = new MmiCommunication("localhost", 8000, "User1", "GUI");
            

        }

        private async void MmiC_Message(object sender, MmiEventArgs e)
        {
            Console.WriteLine(e.Message);
            var doc = XDocument.Parse(e.Message);
            var com = doc.Descendants("command").FirstOrDefault().Value;
            dynamic json = JsonConvert.DeserializeObject(com);
            Console.WriteLine(json);
            if ((string)json.action.ToString() == "joga")
            {
                string move = "";
                move += (string)json.initial_letter.ToString();
                move += (string)json.initial_number.ToString();
                move += (string)json.final_letter.ToString();
                move += (string)json.final_number.ToString();
                Task<String> move_piece = Task.Run(() => MovePiece(game, move));
                String result = move_piece.Result;
                await mmic.Send(lce.NewContextRequest());
                var exNot = lce.ExtensionNotification(0 + "", 0 + "", 1, result);
                await mmic.Send(exNot);
            }
            else if ((string)json.action.ToString() == "envia")
            {
                string message = "";
                message += (string)json.message.ToString();
                Task<String> send_msg = Task.Run(() => SendMessage(game, message));
                String result = send_msg.Result;
                await mmic.Send(lce.NewContextRequest());
                var exNot = lce.ExtensionNotification(0 + "", 0 + "", 1, result);
                await mmic.Send(exNot);
            }
            else if ((string)json.action.ToString() == "desisto")
            {
                Task<String> resign = Task.Run(() => Resign(game));
                String result = resign.Result;
                await mmic.Send(lce.NewContextRequest());
                var exNot = lce.ExtensionNotification(0 + "", 0 + "", 1, result);
                await mmic.Send(exNot);
            }
            else if ((string)json.action.ToString() == "desafia")
            {
                string user = "";
                user += (string)json.user.ToString();
                Task<String> challenge = Task.Run(() => Challenge(user));
                string result = challenge.Result;
                string data = getBetween(result, "url", "status");
                char[] removeStuff = { '"', ':', ',' };
                string auxgame = getBetween(result, "id", "url");
                string newgame = auxgame.TrimEnd(removeStuff);
                game = newgame.TrimStart(removeStuff);
                string auxdata = data.TrimEnd(removeStuff);
                string newdata = auxdata.TrimStart(removeStuff);
                System.Diagnostics.Process.Start("firefox.exe", newdata);
                await mmic.Send(lce.NewContextRequest());
                var exNot = lce.ExtensionNotification(0 + "", 0 + "", 1, result);
                await mmic.Send(exNot);
            }
            else if ((string)json.action.ToString() == "engano")
            {
                string message = "yes";
                Task<String> send_msg = Task.Run(() => TakeBack(game, message));
                String result = send_msg.Result;
                await mmic.Send(lce.NewContextRequest());
                var exNot = lce.ExtensionNotification(0 + "", 0 + "", 1, result);
                await mmic.Send(exNot);
            }
            else if ((string)json.action.ToString() == "list")
            {
                Task<String> list = Task.Run(() => ListChallenges());
                String result = list.Result;
                string data = "";
                if (result.Contains(@"""in"":[]"))
                {
                    data = "";
                }
                else
                {
                    data = getBetween(result, "id", "url");
                    char[] removeStuff = { '"', ':', ',' };
                    data = data.TrimStart(removeStuff);
                    data = data.TrimEnd(removeStuff);
                    game = data;
                }
                
                await mmic.Send(lce.NewContextRequest());

                if (String.IsNullOrEmpty(data))
                {
                    result = "Não";
                }
                else
                {
                    result = "Sim";
                }
                var exNot = lce.ExtensionNotification(0 + "", 0 + "", 1, result);
                await mmic.Send(exNot);
            }
            else if ((string)json.action.ToString() == "accept")
            {
                Task<String> accept = Task.Run(() => Accept(game));
                string result = accept.Result;
                System.Diagnostics.Process.Start("firefox.exe", "https://lichess.org/" + game);
                await mmic.Send(lce.NewContextRequest());
                var exNot = lce.ExtensionNotification(0 + "", 0 + "", 1, result);
                await mmic.Send(exNot);
            }

        }
        public static string getBetween(string strSource, string strStart, string strEnd)
        {
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                int Start, End;
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }

            return "";
        }

        static async Task<String> MovePiece(String Game, String Move)
        {
            var client = new HttpClient();
            var url = new Uri("https://lichess.org/api/board/game/" + Game + "/move/" + Move);
            var values = new Dictionary<string, string>()
            {
                {"", "" }
            };
            var content = new FormUrlEncodedContent(values);
            client.DefaultRequestHeaders.Authorization
                            = new AuthenticationHeaderValue("Bearer", "lip_I0iYfH1quLT2GUWlxrAq");
            var result = await client.PostAsync(url, content);
            string resultContent = await result.Content.ReadAsStringAsync();
            return resultContent;
        }

        static async Task<String> SendMessage(String Game, String Message)
        {
            var client = new HttpClient();
            var url = new Uri("https://lichess.org/api/board/game/" + Game + "/chat");
            var values = new Dictionary<string, string>()
            {
                {"text", Message },
                {"room", "player" }
            };
            var content = new FormUrlEncodedContent(values);
            client.DefaultRequestHeaders.Authorization
                            = new AuthenticationHeaderValue("Bearer", "lip_I0iYfH1quLT2GUWlxrAq");
            var result = await client.PostAsync(url, content);
            string resultContent = await result.Content.ReadAsStringAsync();
            return resultContent;
        }

        static async Task<String> Resign(String Game)
        {
            var client = new HttpClient();
            var url = new Uri("https://lichess.org/api/board/game/" + Game + "/resign");
            var values = new Dictionary<string, string>()
            {
                {"", "" }
            };
            var content = new FormUrlEncodedContent(values);
            client.DefaultRequestHeaders.Authorization
                            = new AuthenticationHeaderValue("Bearer", "lip_I0iYfH1quLT2GUWlxrAq");
            var result = await client.PostAsync(url, content);
            string resultContent = await result.Content.ReadAsStringAsync();
            return resultContent;
        }

        static async Task<String> Challenge(String User)
        {
            var client = new HttpClient();
            var url = new Uri("https://lichess.org/api/challenge/" + User);
            var values = new Dictionary<string, string>()
            {
                {"", "" }
            };
            var content = new FormUrlEncodedContent(values);
            client.DefaultRequestHeaders.Authorization
                            = new AuthenticationHeaderValue("Bearer", "lip_I0iYfH1quLT2GUWlxrAq");
            var result = await client.PostAsync(url, content);
            string resultContent = await result.Content.ReadAsStringAsync();
            return resultContent;
        }

        static async Task<String> TakeBack(String Game, String Message)
        {
            var client = new HttpClient();
            var url = new Uri("https://lichess.org/api/board/game/" + Game + "/takeback/" + Message);
            var values = new Dictionary<string, string>()
            {
                {"", "" },
            };
            var content = new FormUrlEncodedContent(values);
            client.DefaultRequestHeaders.Authorization
                            = new AuthenticationHeaderValue("Bearer", "lip_I0iYfH1quLT2GUWlxrAq");
            var result = await client.PostAsync(url, content);
            string resultContent = await result.Content.ReadAsStringAsync();
            return resultContent;
        }

        static async Task<String> ListChallenges()
        {
            var client = new HttpClient();
            var url = new Uri("https://lichess.org/api/challenge");
            client.DefaultRequestHeaders.Authorization
                            = new AuthenticationHeaderValue("Bearer", "lip_I0iYfH1quLT2GUWlxrAq");
            var result = await client.GetAsync(url);
            string resultContent = await result.Content.ReadAsStringAsync();
            return resultContent;
        }

        static async Task<String> Accept(String Accept)
        {
            var client = new HttpClient();
            var url = new Uri("https://lichess.org/api/challenge/" + Accept + "/accept");
            var values = new Dictionary<string, string>()
            {
                {"", "" }
            };
            var content = new FormUrlEncodedContent(values);
            client.DefaultRequestHeaders.Authorization
                            = new AuthenticationHeaderValue("Bearer", "lip_I0iYfH1quLT2GUWlxrAq");
            var result = await client.PostAsync(url, content);
            string resultContent = await result.Content.ReadAsStringAsync();
            return resultContent;
        }

    }
}
