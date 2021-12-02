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
                Task<String> move_piece = Task.Run(() => MovePiece("WSEeAv9uljDu", move));
                String result = move_piece.Result;
                await mmic.Send(lce.NewContextRequest());
                var exNot = lce.ExtensionNotification(0 + "", 0 + "", 1, result);
                await mmic.Send(exNot);
            }
            else if ((string)json.action.ToString() == "envia")
            {
                string message = "";
                message += (string)json.message.ToString();
                Task<String> move_piece = Task.Run(() => SendMessage("WSEeAv9uljDu", message));
                String result = move_piece.Result;
                await mmic.Send(lce.NewContextRequest());
                var exNot = lce.ExtensionNotification(0 + "", 0 + "", 1, result);
                await mmic.Send(exNot);
            }

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
                            = new AuthenticationHeaderValue("Bearer", "lip_WRQzhAeD2ZGNt1MZXsAI");
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
                            = new AuthenticationHeaderValue("Bearer", "lip_WRQzhAeD2ZGNt1MZXsAI");
            var result = await client.PostAsync(url, content);
            string resultContent = await result.Content.ReadAsStringAsync();
            Console.WriteLine(resultContent);
            return resultContent;
        }

    }
}
