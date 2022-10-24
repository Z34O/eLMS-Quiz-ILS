using System;
using System.Linq;
using System.Threading;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Web;

using Spectre.Console;
using Newtonsoft.Json;
using System.IO;

namespace eLMSQuizILS
{
    class Program
    {

        static void Main(string[] args)
        {
            // Allow certain characters to properly display
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            QuizUI.Menu();

            Console.ReadKey();
        }
    }

    // I recommend using Windows Terminal app instead of CMD and Powershell
    // Limitations of the terminal may affect the quality :(
    public class QuizUI
    {
        public static string Difficulty;
        private static int Score = 0;

        public static int MathScore = 0;
        public static int ScienceScore = 0;
        public static int ComputerScore = 0;
        public static int HistoryScore = 0;

        public static string Name;
        public static string StudentNumber;

        public static string DatabaseAPI = "https://kvdb.io/N5xTdNFiuDraHNe6oPqyQ6/";

        // Calculate where to place string horizontally to center it
        static string center(string text)
        {
            return new String(' ', (Console.WindowWidth - text.Length) / 2) + text;
        }

        static void post(string url, string body)
        {
            // Build our request
            WebRequest request = WebRequest.Create(url);

            // Turn our body data (post data) into a byte array
            string postData = body;
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            // Add request headers
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;

            // Send :)
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            request.GetResponse();
        }

        static string get(string url)
        {
            WebClient wc = new WebClient();
            return wc.DownloadString(url);
        }

        // The first thing the user sees
        public static void Menu()
        {
            string[] stiLogo = {
                  "                  ,----,            ",
                  "                ,/    .`|           ",
                  "  .--.--.      ,`   .'  :   ,---,   ",
                  " /  /    '.  ;    ;     /,`--.' |   ",
                  "|  :  /`. /.'___,/    ,' |   :  :   ",
                  ";  |  |--` |    :     |  :   |  '   ",
                  "|  :  ;_   ;    |.';  ;  |   :  |   ",
                  "\\  \\    `.`----'  |  |  '   '  ;  ",
                  "  `----.   \\   '   :  ;  |   |  |   ",
                  "  _ \\  \\  |   |   |  '  '   :  ;  ",
                  " /  /`--'  /   '   :  |  |   |  '   ",
                  "'--'.     /    ;   |.'   '   :  |   ",
                  "  `--'---'     '---'     ;   |.'    ",
                  "                         '---'      "
            };

            int blue = 255;

            Console.Clear();
            AnsiConsole.MarkupLine("\n\n\n");


            foreach (string logoLine in stiLogo)
            {
                AnsiConsole.MarkupLineInterpolated($"[rgb(255,255,{blue})]{center(logoLine)}[/]");
                blue -= 15;  // Achieve yellow gradient effect
            }

            AnsiConsole.MarkupLineInterpolated($"[bold]{center("Systems Technology Institute 1992")}[/]");
            AnsiConsole.MarkupLineInterpolated($"[italic]{center("Education For Real Life\n")}[/]");

            AnsiConsole.MarkupLineInterpolated($"[yellow]{center("eLMS version 0.0.1")}[/]");
            AnsiConsole.MarkupLineInterpolated($"[rapidblink dodgerblue2]{center("Press any key to proceed to your quiz...")}[/]");

            Console.ReadKey();
            Console.Clear();

            // Proceed to next UI component
            Login();
        }

        // The UI that will be shown after the Main Menu screen
        public static void Login()
        {
            string difficultyColor;

            var nameRule = new Rule("[red]Student Information[/]");
            nameRule.Alignment = Justify.Left;

            var configRule = new Rule("[red]Configure your quiz[/]");
            configRule.Alignment = Justify.Left;

            var detailsRule = new Rule("[red]Quiz details[/]");
            detailsRule.Alignment = Justify.Left;

            AnsiConsole.Markup("\n");

            AnsiConsole.Write(nameRule);
            Name = AnsiConsole.Ask<string>("Last name: ");
            StudentNumber = AnsiConsole.Ask<string>("Student number: ");

            AnsiConsole.Status().Spinner(Spinner.Known.Dots)
                .Start("Validating data...", ctx =>
                {
                    // We don't really validate any data, just fake it for the UX
                    Random rand = new Random();
                    Thread.Sleep(rand.Next(1234, 5432));
                });

            AnsiConsole.MarkupLine("[bold lime]\nStudent Verified!\n[/]");

            AnsiConsole.Write(configRule);
            Difficulty = AnsiConsole.Prompt(new SelectionPrompt<string>()
                                .Title("Choose difficulty")
                                .AddChoices(new[] { "Easy", "Medium", "Hard" })
            );

            // Colored difficulty
            switch (Difficulty)
            {
                case "Easy":
                    difficultyColor = "lime";
                    break;
                case "Medium":
                    difficultyColor = "orange3";
                    break;
                case "Hard":
                    difficultyColor = "red";
                    break;
                default:
                    difficultyColor = "grey";
                    break;
            }

            AnsiConsole.MarkupLineInterpolated($"Difficulty successfully set to [{difficultyColor}]{Difficulty}[/]\n");

            AnsiConsole.Write(detailsRule);
            AnsiConsole.MarkupLine("This quiz is composed of 20 randomized items.");
            AnsiConsole.MarkupLine("Topics include [italic red]Mathematics[/], [italic yellow]Science & Nature[/], [italic lime]Computer Studies[/], and [italic orange4_1]History[/].");
            AnsiConsole.MarkupLine("The quiz is not timed, but you cannot leave else your answers won't be submitted");

            AnsiConsole.MarkupLine("[rapidblink blue]\nPress any key to start the quiz...[/]");
            Console.ReadKey();
            Console.Clear();

            Start();
        }

        // The main quiz UI
        private static void Start()
        {
            Rule header;
            string choice;
            Table details;

            int currentItem = 1;

            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .Start("Fetching quiz questions...", ctx =>
                {
                    OpenTDB.fetch();
                });

            // Loop through our questions
            foreach (Question question in OpenTDB.Questions)
            {
                header = new Rule($"[bold]{question.category}[/]");
                header.Alignment = Justify.Left;

                details = new Table();
                details.Alignment = Justify.Left;

                AnsiConsole.MarkupLine("");
                AnsiConsole.Write(header);

                details.AddColumn("Score");
                details.AddColumn("Current item");
                details.AddColumn("Difficulty");

                details.AddRow($"[bold green]{Score}[/]/[red]20[/]", $"[bold]{currentItem}[/]", $"{Difficulty}");

                AnsiConsole.Write(details);

                AnsiConsole.MarkupLine("");

                var table = new Table().Centered();

                choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title($"[italic yellow]{HttpUtility.HtmlDecode(question.question)}[/]")
                    .AddChoices(question.incorrect_answers.Concat(new List<string>() { HttpUtility.HtmlDecode(question.correct_answer) })));

                question.user_choice = choice;

                if (choice == question.correct_answer)
                {
                    AnsiConsole.MarkupLine(":check_mark: [bold lime]Correct![/]");
                    Score += 1;

                    // For score statistics
                    switch(question.category)
                    {
                        case "Science: Mathematics":
                            MathScore++;
                            break;
                        case "Science: Computers":
                            ComputerScore++;
                            break;
                        case "History":
                            HistoryScore++;
                            break;
                        case "Science & Nature":
                            ScienceScore++;
                            break;
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine(":exclamation_question_mark: [bold red]Wrong answer[/]");
                }

                currentItem++;

                Thread.Sleep(1000);
                Console.Clear();
            }

            // Overview of answers here

            Results();
        }

        private static void Results()
        {
            TreeNode questionNode;

            Tree root;

            int showLimit = 5;
            int questionNumber = 1;
            int currentPage = 1;

            root = new Tree($"[bold lime]Your results: eLMS Quiz[/] {currentPage}/4 [bold red]>>>[/]");

            // Show the user his/her answers separated in 5 pages
            foreach (Question question in OpenTDB.Questions)
            {
                questionNode = root.AddNode($"[italic yellow]{ HttpUtility.HtmlDecode(question.question) }[/]");
                questionNode.AddNode($"[{(question.user_choice == question.correct_answer ? "lime" : "red")}]{question.user_choice}[/]");

                if (questionNumber % showLimit == 0)
                {
                    AnsiConsole.Write(root);

                    if(questionNumber == 20)
                    {
                        AnsiConsole.MarkupLine("\n[rapidblink red]:warning: Clicking any key will submit your answers[/]");
                    }

                    currentPage++;
                    root = new Tree($"[bold lime]Your answers: eLMS Quiz[/] {currentPage}/4 [bold red]>>>[/]");

                    Console.ReadKey();
                    Console.Clear();
                }
                
                questionNumber++;
            }

            var endRule = new Rule("[bold lime]You have successfully finished the quiz[/]");
            endRule.Alignment = Justify.Left;

            Table leaderboards = new Table();
            leaderboards.Title("[bold yellow underline]HIGHSCORER[/]");

            AnsiConsole.Write(endRule);
            AnsiConsole.MarkupLine("[italic]Check your score statistics below[/]\n");

            AnsiConsole.Write(new BarChart()
                .Width(60)
                .Label("[bold lime underline]Scores per topic[/]")
                .AddItem("Mathematics", MathScore, Color.Red)
                .AddItem("Science", ScienceScore, Color.Yellow)
                .AddItem("Computer Studies", ComputerScore, Color.Green)
                .AddItem("History", HistoryScore, Color.SandyBrown));

            AnsiConsole.MarkupLine("");

            AnsiConsole.Write(new BreakdownChart()
                .Width(60)
                .AddItem("Correct answers", Score, Color.Lime)
                .AddItem("Mistakes", 20 - Score, Color.Red));

            AnsiConsole.MarkupLine("\n");

            AnsiConsole.MarkupLine("\n[bold]You have finished the exam. Your scores are automatically uploaded to the database.[/]\n");

            // Upload it like we promised, only if high score
            if(Score > Convert.ToInt32(get(DatabaseAPI + "score")))
            {
                post(DatabaseAPI + "name", Name);
                post(DatabaseAPI + "student_number", StudentNumber);
                post(DatabaseAPI + "score", Convert.ToString(Score));
            }

            leaderboards.AddColumn("Name");
            leaderboards.AddColumn("Student Number");
            leaderboards.AddColumn("Score");

            // Add data to our leaderboards
            leaderboards.AddRow(get(DatabaseAPI + "name"), get(DatabaseAPI + "student_number"), get(DatabaseAPI + "score"));

            AnsiConsole.Write(leaderboards);

            AnsiConsole.MarkupLine("[rapidblink yellow italic]Press any key to exit...[/]");
        }
    }


    // Fetching of data from API and conversion happens here
    public class OpenTDB
    {
        private static WebClient wc = new WebClient();
        private static Response mathResponse;
        private static Response scienceResponse;
        private static Response computerResponse;
        private static Response historyResponse;

        // All questions
        internal static List<Question> Questions = new List<Question>();

        // Fetches and converts questions (API randomizes them by default)
        // We only need 20 questions so 5 per each topics
        public static void fetch()
        {
            mathResponse = JsonConvert.DeserializeObject<Response>(wc.DownloadString($"https://opentdb.com/api.php?amount=5&category=19&difficulty={QuizUI.Difficulty.ToLower()}&type=multiple"));
            scienceResponse = JsonConvert.DeserializeObject<Response>(wc.DownloadString($"https://opentdb.com/api.php?amount=5&category=17&difficulty={QuizUI.Difficulty.ToLower()}&type=multiple"));
            computerResponse = JsonConvert.DeserializeObject<Response>(wc.DownloadString($"https://opentdb.com/api.php?amount=5&category=18&difficulty={QuizUI.Difficulty.ToLower()}&type=multiple"));
            historyResponse = JsonConvert.DeserializeObject<Response>(wc.DownloadString($"https://opentdb.com/api.php?amount=5&category=23&difficulty={QuizUI.Difficulty.ToLower()}&type=multiple"));
           
            foreach (Question mixedQuestions in mathResponse.results
                        .Concat(scienceResponse.results)
                        .Concat(computerResponse.results)
                        .Concat(historyResponse.results)
                        // Randomize
                        .OrderBy(question => Guid.NewGuid()).ToList())
            {
                Questions.Add(mixedQuestions);
            }
        }
    }

    // JSON response by API (https://opentdb.com/) converts to the following objects below
    internal class Question
    {
        public string category { get; set; }
        public string difficulty { get; set; }
        public string question { get; set; }
        public string correct_answer { get; set; }
        public string user_choice { get; set; }
        public List<string> incorrect_answers { get; set; }
    }

    internal class Response
    {
        public int response_code { get; set; }
        public List<Question> results { get; set; }
    }
}
