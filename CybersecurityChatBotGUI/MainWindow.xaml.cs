using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using NAudio.Wave;

namespace CybersecurityAwarenessBot
{
    public partial class MainWindow : Window
    {
        private Random random = new Random();
        private string lastTopic = "";
        private string favoriteTopic = "";

        private Dictionary<string, Func<string>> topicResponders;

        private List<TaskItem> tasks = new List<TaskItem>();
        private DispatcherTimer reminderTimer;

        // For audio playback (to keep alive)
        private WaveOutEvent waveOut;
        private AudioFileReader audioFileReader;

        // Quiz-related fields
        private List<QuizQuestion> quizQuestions;
        private int currentQuestionIndex = 0;
        private int score = 0;
        private bool isInQuiz = false;

        // Conversation state enum
        private enum ConversationState
        {
            None,
            WaitingForTaskTitle,
            WaitingForTaskDescription,
            WaitingForTaskReminder,
            WaitingForTaskReminderDate,
            WaitingForTaskReminderTime,
            WaitingForMarkCompletedTaskName,
            WaitingForDeleteTaskName
        }

        private ConversationState currentState = ConversationState.None;

        // Temp vars to store user inputs during multi-step task addition
        private string tempTaskTitle = "";
        private string tempTaskDescription = "";
        private DateTime? tempTaskReminder = null;

        // Flag for waiting for description input
        private bool isWaitingForDescriptionInput = false;

        // Activity log to record chatbot actions
        private List<string> activityLog = new List<string>();

        public MainWindow()
        {
            InitializeComponent();

            InitializeTopicResponders();

            DisplayAsciiArt();
            PlayWelcomeAudio();
            InitializeReminderTimer();
        }

        private void InitializeTopicResponders()
        {
            topicResponders = new Dictionary<string, Func<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "password", GetPasswordSafetyTip },
                { "phishing", GetPhishingTip },
                { "browsing", GetSafeBrowsingTip },
                { "safe browsing", GetSafeBrowsingTip },
                { "password safety", GetPasswordSafetyTip }
            };
        }

        private void DisplayAsciiArt()
        {
            AsciiLogoTextBox.Text = @"
  ██████╗  █████╗ ██████╗  
  ██╔══██╗██╔══██╗██╔══██╗ 
  ██║    ║███████║██║████║ 
  ██║  ██║██╔══██║██║══██║ 
  ██████╔╝██║  ██║██████╔╝ 
  ╚═════╝ ╚═╝  ╚═╝╚═════╝  
  C.A.B - Cybersecurity Awareness Bot
";
        }

        private void PlayWelcomeAudio()
        {
            try
            {
                string filePath = @"C:\Users\edith\OneDrive\Documents\Rosebank College\ST10446133_POE3_2_completed\ST10446133_POE3_2_completed\CybersecurityChatBotGUI\welcom.wav.wav";
                if (File.Exists(filePath))
                {
                    audioFileReader = new AudioFileReader(filePath);
                    waveOut = new WaveOutEvent();
                    waveOut.Init(audioFileReader);
                    waveOut.Play();

                    waveOut.PlaybackStopped += (s, e) =>
                    {
                        waveOut.Dispose();
                        audioFileReader.Dispose();
                    };
                }
                else
                {
                    AppendChatMessage("Audio file not found. Skipping sound...");
                }
            }
            catch (Exception ex)
            {
                AppendChatMessage($"Error playing sound: {ex.Message}");
            }
        }

        private void InitializeReminderTimer()
        {
            reminderTimer = new DispatcherTimer();
            reminderTimer.Interval = TimeSpan.FromMinutes(1);
            reminderTimer.Tick += ReminderTimer_Tick;
            reminderTimer.Start();
        }

        private void ReminderTimer_Tick(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            var dueTasks = tasks.Where(t => t.ReminderDateTime != null && t.ReminderDateTime <= now && !t.IsCompleted && !t.ReminderNotified).ToList();

            foreach (var task in dueTasks)
            {
                AppendChatMessage($"[Reminder] Task '{task.Title}' is due now or soon!");
                activityLog.Add($"{DateTime.Now}: Reminder triggered for task '{task.Title}'.");
                task.ReminderNotified = true; // Avoid repeated reminders
            }
        }

        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            // Normal Add Task from UI panel
            string title = TaskTitleTextBox.Text.Trim();
            string description = TaskDescTextBox.Text.Trim();

            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Please enter a task title.", "Missing Title", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime? reminderDateTime = null;
            if (TaskReminderDatePicker.SelectedDate != null)
            {
                if (TimeSpan.TryParse(TaskReminderTimeComboBox.Text.Trim(), out TimeSpan time))
                {
                    reminderDateTime = TaskReminderDatePicker.SelectedDate.Value.Date + time;
                }
                else if (string.IsNullOrWhiteSpace(TaskReminderTimeComboBox.Text))
                {
                    reminderDateTime = TaskReminderDatePicker.SelectedDate.Value.Date + new TimeSpan(9, 0, 0);
                }
                else
                {
                    MessageBox.Show("Invalid time format. Use HH:mm (24-hour).", "Invalid Time", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            TaskItem newTask = new TaskItem
            {
                Title = title,
                Description = description,
                ReminderDateTime = reminderDateTime,
                IsCompleted = false,
                ReminderNotified = false
            };

            tasks.Add(newTask);
            RefreshTaskList();

            AppendChatMessage($"Task added: '{title}'.");
            activityLog.Add($"{DateTime.Now}: Task '{title}' added via UI.");

            if (reminderDateTime != null)
            {
                AppendChatMessage($"Reminder set for {reminderDateTime.Value:f}.");
                activityLog.Add($"{DateTime.Now}: Reminder set for task '{title}' at {reminderDateTime.Value:f}.");
            }

            TaskTitleTextBox.Clear();
            TaskDescTextBox.Clear();
            TaskReminderDatePicker.SelectedDate = null;
            TaskReminderTimeComboBox.SelectedIndex = -1;
        }

        private void RefreshTaskList()
        {
            TaskListBox.Items.Clear();
            foreach (var task in tasks)
            {
                string status = task.IsCompleted ? "✅" : "";
                string reminder = task.ReminderDateTime != null ? $"(Reminder: {task.ReminderDateTime.Value:f})" : "";
                string desc = string.IsNullOrWhiteSpace(task.Description) ? "" : $" - {task.Description}";
                TaskListBox.Items.Add($"{status} {task.Title}{desc} {reminder}");
            }
        }

        private void AppendChatMessage(string message)
        {
            ChatDisplayTextBox.AppendText(message + Environment.NewLine);
            ChatDisplayTextBox.ScrollToEnd();
        }

        private void ChatSendButton_Click(object sender, RoutedEventArgs e)
        {
            string userInput = ChatInputTextBox.Text.Trim();
            if (string.IsNullOrEmpty(userInput))
                return;

            AppendChatMessage($"You: {userInput}");
            ChatInputTextBox.Clear();

            List<string> responses = ProcessInput(userInput);

            foreach (var response in responses)
            {
                AppendChatMessage($"Bot: {response}");
            }
        }

        private void ChatInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                ChatSendButton_Click(sender, null);
            }
        }

        public void StartQuizButton_Click(object sender, RoutedEventArgs e)
        {
            InitQuizQuestions();
            currentQuestionIndex = 0;
            score = 0;
            isInQuiz = true;

            AppendChatMessage("🎯 Cybersecurity Quiz Started!");
            activityLog.Add($"{DateTime.Now}: Quiz started.");

            DisplayCurrentQuizQuestion();
        }

        private void InitQuizQuestions()
        {
            quizQuestions = new List<QuizQuestion>
            {
                new QuizQuestion("What should you do if you receive an email asking for your password?",
                    new[] { "A) Reply with your password", "B) Delete the email", "C) Report the email as phishing", "D) Ignore it" }, "C"),
                new QuizQuestion("True or False: It's safe to use the same password for multiple accounts.",
                    new[] { "A) True", "B) False" }, "B"),
                // Add more questions as needed
            };
        }

        private void DisplayCurrentQuizQuestion()
        {
            if (currentQuestionIndex >= quizQuestions.Count)
            {
                isInQuiz = false;
                AppendChatMessage($"🎉 Quiz Completed! Final Score: {score}/{quizQuestions.Count}");
                activityLog.Add($"{DateTime.Now}: Quiz completed with score {score}/{quizQuestions.Count}.");

                if (score >= 8)
                    AppendChatMessage("🏆 Excellent work! You're a cybersecurity pro!");
                else if (score >= 5)
                    AppendChatMessage("👍 Not bad! Keep practicing to improve your knowledge.");
                else
                    AppendChatMessage("📘 Keep learning to stay safe online. You're on the right path!");
                return;
            }

            var q = quizQuestions[currentQuestionIndex];
            AppendChatMessage($"Q{currentQuestionIndex + 1}: {q.Question}");
            foreach (string option in q.Options)
            {
                AppendChatMessage(option);
            }
            AppendChatMessage("👉 Please answer with the letter (e.g., A, B, C, D).");
        }

        private void ProcessQuizAnswer(string userInput)
        {
            var q = quizQuestions[currentQuestionIndex];
            if (string.Equals(userInput.Trim(), q.Answer, StringComparison.OrdinalIgnoreCase))
            {
                AppendChatMessage("✅ Correct!");
                score++;
            }
            else
            {
                AppendChatMessage($"❌ Incorrect. The correct answer was {q.Answer}.");
            }

            currentQuestionIndex++;
            DisplayCurrentQuizQuestion();
        }

        private List<string> ProcessInput(string input)
        {
            List<string> output = new List<string>();
            string userInput = input.ToLower();

            // Show activity log commands
            if (userInput == "show activity log" || userInput == "what have you done for me?" || userInput == "show my activity log")
            {
                if (activityLog.Count == 0)
                {
                    output.Add("Activity log is empty.");
                }
                else
                {
                    output.Add("📋 Here's what I've done recently (showing latest 10 actions):");
                    int count = 1;
                    var recentActions = activityLog.Skip(Math.Max(0, activityLog.Count - 10)).Reverse();
                    foreach (var entry in recentActions)
                    {
                        output.Add($"{count}. {entry}");
                        count++;
                    }
                    if (activityLog.Count > 10)
                    {
                        output.Add("Type 'show all logs' to see the full history.");
                    }
                }
                return output;
            }

            // Optionally add handling to show all logs if needed:
            if (userInput == "show all logs")
            {
                if (activityLog.Count == 0)
                {
                    output.Add("Activity log is empty.");
                }
                else
                {
                    output.Add("📋 Full activity log:");
                    int count = 1;
                    foreach (var entry in activityLog)
                    {
                        output.Add($"{count}. {entry}");
                        count++;
                    }
                }
                return output;
            }

            // Handle quiz mode
            if (isInQuiz)
            {
                ProcessQuizAnswer(input.Trim().ToUpper());
                return new List<string>(); // Quiz handles feedback via chat
            }

            // Conversation state machine for multi-step task management
            if (currentState == ConversationState.WaitingForTaskTitle)
            {
                if (string.IsNullOrWhiteSpace(input))
                {
                    output.Add("Please specify the task title.");
                    return output;
                }
                tempTaskTitle = input.Trim();
                currentState = ConversationState.WaitingForTaskDescription;
                output.Add("Got it. Would you like to add a description? (yes/no)");
                return output;
            }
            else if (currentState == ConversationState.WaitingForTaskDescription)
            {
                if (input.Trim().ToLower() == "yes")
                {
                    output.Add("Please enter the task description.");
                    // Stay in this state until user inputs actual description
                    currentState = ConversationState.WaitingForTaskDescription;
                    // Actually, better to track a substate or check input again below
                    // Let's add a flag instead:
                    isWaitingForDescriptionInput = true;
                    return output;
                }
                else if (input.Trim().ToLower() == "no")
                {
                    tempTaskDescription = "";
                    currentState = ConversationState.WaitingForTaskReminder;
                    output.Add("Would you like to add a reminder? (yes/no)");
                    return output;
                }
                else if (isWaitingForDescriptionInput)
                {
                    tempTaskDescription = input.Trim();
                    isWaitingForDescriptionInput = false;
                    currentState = ConversationState.WaitingForTaskReminder;
                    output.Add("Would you like to add a reminder? (yes/no)");
                    return output;
                }
                else
                {
                    output.Add("Please reply 'yes' or 'no'. Would you like to add a description?");
                    return output;
                }
            }
            else if (currentState == ConversationState.WaitingForTaskReminder)
            {
                string trimmedInput = input.Trim().ToLower();

                if (tempTaskReminder == null)
                {
                    // Expecting yes/no or date input
                    if (trimmedInput == "yes")
                    {
                        output.Add("Please enter the reminder date in format yyyy-MM-dd (e.g., 2025-07-01):");
                        return output;
                    }
                    else if (trimmedInput == "no")
                    {
                        AddNewTaskFromTemp(output);
                        currentState = ConversationState.None;
                        return output;
                    }
                    else if (DateTime.TryParse(input.Trim(), out DateTime reminderDate))
                    {
                        tempTaskReminder = reminderDate.Date; // Store date only, time will come next
                        output.Add("Great! Now please enter the reminder time in 24-hour format HH:mm (e.g., 14:30):");
                        return output;
                    }
                    else
                    {
                        output.Add("I didn't understand that. Please reply 'yes' to set a reminder, 'no' to skip, or enter the reminder date in yyyy-MM-dd format.");
                        return output;
                    }
                }
                else
                {
                    // Date has been set, now expecting time input or no reminder
                    if (trimmedInput == "no")
                    {
                        // User decided not to set time, default to 9:00 AM
                        tempTaskReminder = tempTaskReminder.Value.Date + new TimeSpan(9, 0, 0);
                        AddNewTaskFromTemp(output);
                        currentState = ConversationState.None;
                        return output;
                    }
                    else if (TimeSpan.TryParse(input.Trim(), out TimeSpan reminderTime))
                    {
                        // Combine date + time
                        tempTaskReminder = tempTaskReminder.Value.Date + reminderTime;
                        AddNewTaskFromTemp(output);
                        currentState = ConversationState.None;
                        return output;
                    }
                    else
                    {
                        output.Add("That doesn't look like a valid time. Please enter the time in 24-hour format HH:mm (e.g., 14:30), or type 'no' to skip setting the reminder time.");
                        return output;
                    }
                }
            }
            else if (currentState == ConversationState.WaitingForMarkCompletedTaskName)
            {
                string taskName = input.Trim();
                if (string.IsNullOrEmpty(taskName))
                {
                    output.Add("Please specify the task name you want to mark as completed.");
                    return output;
                }

                var task = tasks.FirstOrDefault(t => t.Title.Equals(taskName, StringComparison.OrdinalIgnoreCase));
                if (task == null)
                {
                    output.Add($"I couldn't find a task named '{taskName}'. Please check the name and try again.");
                    return output;
                }
                if (task.IsCompleted)
                {
                    output.Add($"Task '{task.Title}' is already marked as completed.");
                    currentState = ConversationState.None;
                    return output;
                }

                task.IsCompleted = true;
                RefreshTaskList();
                output.Add($"Task '{task.Title}' marked as completed.");
                activityLog.Add($"{DateTime.Now}: Task '{task.Title}' marked as completed.");
                currentState = ConversationState.None;
                return output;
            }
            else if (currentState == ConversationState.WaitingForDeleteTaskName)
            {
                string taskName = input.Trim();
                if (string.IsNullOrEmpty(taskName))
                {
                    output.Add("Please specify the task name you want to delete.");
                    return output;
                }

                var task = tasks.FirstOrDefault(t => t.Title.Equals(taskName, StringComparison.OrdinalIgnoreCase));
                if (task == null)
                {
                    output.Add($"I couldn't find a task named '{taskName}'. Please check the name and try again.");
                    return output;
                }

                tasks.Remove(task);
                RefreshTaskList();
                output.Add($"Task '{task.Title}' deleted.");
                activityLog.Add($"{DateTime.Now}: Task '{task.Title}' deleted.");
                currentState = ConversationState.None;
                return output;
            }

            // No current conversation state, process commands normally

            if (userInput.Contains("exit") || userInput.Contains("bye"))
            {
                output.Add("Goodbye! Stay safe online!");
                return output;
            }

            if (userInput.Contains("i'm interested in") || userInput.Contains("my favorite topic is"))
            {
                foreach (var topic in topicResponders.Keys)
                {
                    if (userInput.Contains(topic))
                    {
                        favoriteTopic = topic;
                        output.Add($"Great! I'll remember that you're interested in {topic}. It's a crucial part of staying safe online.");
                        activityLog.Add($"{DateTime.Now}: User favorite topic set to '{topic}'.");
                        return output;
                    }
                }
                output.Add("Thanks for sharing! I'll try to remember that.");
                return output;
            }

            if (userInput.Contains("remind me my topic") || userInput.Contains("what's my favorite"))
            {
                if (!string.IsNullOrEmpty(favoriteTopic))
                {
                    output.Add($"You told me you're interested in {favoriteTopic}. That's a smart choice!");
                }
                else
                {
                    output.Add("You haven't told me your favorite topic yet. Feel free to share it!");
                }
                return output;
            }

            if (DetectAndRespondToSentiment(userInput, out List<string> sentimentResponses))
            {
                output.AddRange(sentimentResponses);
                return output;
            }

            if (userInput.Contains("how are you") || userInput.Contains("how are you doing"))
            {
                output.Add("I'm just a chatbot, but I'm doing great! Thanks for asking. How can I help you with cybersecurity today?");
                return output;
            }

            if (userInput.Contains("what's your purpose") || userInput.Contains("what do you do"))
            {
                output.Add("I'm here to help you stay safe online! I can provide tips on password safety, phishing prevention, and safe browsing.");
                return output;
            }

            if (userInput.Contains("what can i ask you about"))
            {
                output.Add("You can ask me about:\n- Password Safety\n- Phishing\n- Safe Browsing");
                return output;
            }

            // Handle "what is [topic]" questions
            if (userInput.StartsWith("what is "))
            {
                string topic = userInput.Substring(8).Trim();

                switch (topic)
                {
                    case "phishing":
                        output.Add("📖 *Phishing* is a type of cyberattack where attackers impersonate trusted sources (like banks or companies) to trick people into revealing personal information such as passwords, credit card numbers, or login credentials.");
                        return output;
                    case "password":
                    case "password safety":
                        output.Add("📖 *Password safety* refers to best practices for creating, storing, and managing passwords to protect your online accounts from unauthorized access.");
                        return output;
                    case "browsing":
                    case "safe browsing":
                        output.Add("📖 *Safe browsing* means using the internet in a secure way — avoiding suspicious links, using HTTPS sites, and protecting your privacy and data while online.");
                        return output;
                    default:
                        output.Add("I'm not sure what that topic is. You can ask me about phishing, password safety, or safe browsing.");
                        return output;
                }
            }

            bool isFollowUp = userInput.Contains("another") || userInput.Contains("more") ||
                              userInput.Contains("explain") || userInput.Contains("tell me again") ||
                              userInput.Contains("i'm confused");

            if (isFollowUp)
            {
                if (!string.IsNullOrEmpty(lastTopic) && topicResponders.ContainsKey(lastTopic))
                {
                    output.Add(GetTopicResponse(lastTopic));
                    return output;
                }
                else
                {
                    output.Add("Could you clarify what topic you're referring to? You can ask about password safety, phishing, or safe browsing.");
                    return output;
                }
            }

            // Enhanced keyword detection for tasks, reminders, marking complete, deleting
            if (userInput.StartsWith("add task") || userInput.Contains("add a task") || userInput.Contains("create task") || userInput.Contains("new task") || userInput.Contains("remind me to") || userInput.Contains("set reminder"))
            {
                currentState = ConversationState.WaitingForTaskTitle;

                string[] addTaskKeywords = { "add task", "add a task", "create task", "new task" };
                string taskTitleFromInput = null;

                foreach (var keyword in addTaskKeywords)
                {
                    int idx = userInput.IndexOf(keyword);
                    if (idx >= 0)
                    {
                        taskTitleFromInput = userInput.Substring(idx + keyword.Length).Trim();
                        break;
                    }
                }

                if (taskTitleFromInput == null)
                {
                    if (userInput.Contains("remind me to"))
                    {
                        int idx = userInput.IndexOf("remind me to");
                        taskTitleFromInput = userInput.Substring(idx + "remind me to".Length).Trim();
                    }
                    else if (userInput.Contains("set reminder to"))
                    {
                        int idx = userInput.IndexOf("set reminder to");
                        taskTitleFromInput = userInput.Substring(idx + "set reminder to".Length).Trim();
                    }
                }

                if (!string.IsNullOrWhiteSpace(taskTitleFromInput))
                {
                    tempTaskTitle = taskTitleFromInput;
                    currentState = ConversationState.WaitingForTaskDescription;
                    output.Add($"Got it. You want to add task: '{tempTaskTitle}'. Would you like to add a description? (yes/no)");
                }
                else
                {
                    output.Add("What task would you like to add? Please specify the task title.");
                }

                return output;
            }

            if (userInput.StartsWith("mark completed") || userInput.StartsWith("complete task") || userInput.StartsWith("finish task") || userInput.StartsWith("done with task") || userInput.StartsWith("mark task"))
            {
                currentState = ConversationState.WaitingForMarkCompletedTaskName;
                output.Add("Please specify the exact task title you want to mark as completed.");
                return output;
            }

            if (userInput.StartsWith("delete task") || userInput.StartsWith("remove task") || userInput.StartsWith("delete a task"))
            {
                currentState = ConversationState.WaitingForDeleteTaskName;
                output.Add("Please specify the exact task title you want to delete.");
                return output;
            }

            // Respond to cybersecurity topics
            foreach (var topic in topicResponders.Keys)
            {
                if (userInput.Contains(topic))
                {
                    output.Add(GetTopicResponse(topic));
                    lastTopic = topic;
                    return output;
                }
            }

            output.Add("I didn't quite understand that. Could you rephrase your question or ask something related to cybersecurity?");
            return output;
        }

        private void AddNewTaskFromTemp(List<string> output)
        {
            TaskItem newTask = new TaskItem
            {
                Title = tempTaskTitle,
                Description = tempTaskDescription,
                ReminderDateTime = tempTaskReminder,
                IsCompleted = false,
                ReminderNotified = false
            };

            tasks.Add(newTask);
            RefreshTaskList();

            output.Add($"Task '{tempTaskTitle}' added successfully!");
            activityLog.Add($"{DateTime.Now}: Task '{tempTaskTitle}' added.");

            if (tempTaskReminder != null)
            {
                output.Add($"Reminder set for {tempTaskReminder.Value:f}.");
                activityLog.Add($"{DateTime.Now}: Reminder set for task '{tempTaskTitle}' at {tempTaskReminder.Value:f}.");
            }

            // Reset temp vars
            tempTaskTitle = "";
            tempTaskDescription = "";
            tempTaskReminder = null;
            isWaitingForDescriptionInput = false;
        }

        private bool DetectAndRespondToSentiment(string input, out List<string> responses)
        {
            responses = new List<string>();
            if (input.Contains("worried") || input.Contains("anxious") || input.Contains("scared"))
            {
                responses.Add("It's completely understandable to feel that way. Scammers can be very convincing. Let me share some tips to help you stay safe.");
                responses.Add(GetPhishingTip());
                lastTopic = "phishing";
                return true;
            }
            else if (input.Contains("curious") || input.Contains("interested"))
            {
                responses.Add("I'm glad you're curious! Cybersecurity knowledge is power. What would you like to learn more about?");
                return true;
            }
            else if (input.Contains("frustrated") || input.Contains("confused") || input.Contains("overwhelmed"))
            {
                responses.Add("No worries, these topics can be tricky. I'm here to help — feel free to ask for explanations or examples anytime.");
                return true;
            }
            return false;
        }

        private string GetTopicResponse(string topic)
        {
            switch (topic.ToLower())
            {
                case "password":
                case "password safety":
                    return GetPasswordSafetyTip();
                case "phishing":
                    return GetPhishingTip();
                case "browsing":
                case "safe browsing":
                    return GetSafeBrowsingTip();
                default:
                    return "Sorry, I don't have info on that topic yet.";
            }
        }

        private string GetPasswordSafetyTip()
        {
            List<string> passwordTips = new List<string>
            {
                "Use a mix of uppercase, lowercase, numbers, and special characters in your passwords.",
                "Never reuse passwords across different accounts – if one gets compromised, others could too.",
                "Use a password manager to generate and store complex passwords securely.",
                "Avoid using easily guessable info like birthdays or pet names in your passwords.",
                "Enable two-factor authentication (2FA) for an added layer of security."
            };
            return "Password Safety Tip: " + passwordTips[random.Next(passwordTips.Count)];
        }

        private string GetPhishingTip()
        {
            List<string> phishingTips = new List<string>
            {
                "Be cautious of emails asking for personal information. Scammers often disguise themselves as trusted organisations.",
                "Check the sender's email address closely – phishing emails often use misspelled or lookalike domains.",
                "Avoid clicking on suspicious links or downloading attachments from unknown senders.",
                "Phishing messages often create a sense of urgency. Stay calm and verify the message before acting.",
                "Look for generic greetings like 'Dear Customer' – legitimate companies usually address you by name."
            };
            return "Phishing Tip: " + phishingTips[random.Next(phishingTips.Count)];
        }

        private string GetSafeBrowsingTip()
        {
            List<string> browsingTips = new List<string>
            {
                "Always look for 'https://' in the URL to ensure a secure connection.",
                "Avoid clicking on pop-ups or ads that seem too good to be true.",
                "Use an up-to-date antivirus and keep your browser patched.",
                "Don’t enter sensitive information on unfamiliar websites.",
                "Be cautious when using public Wi-Fi – avoid logging into bank accounts or private systems."
            };
            return "Safe Browsing Tip: " + browsingTips[random.Next(browsingTips.Count)];
        }

        // BUTTON HANDLERS FOR TASK LIST UI BUTTONS
        private void MarkCompletedButton_Click(object sender, RoutedEventArgs e)
        {
            if (TaskListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a task to mark as completed.", "No Task Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string selectedItem = TaskListBox.SelectedItem.ToString();
            var task = tasks.FirstOrDefault(t => $"{(t.IsCompleted ? "✅ " : "")}{t.Title}{(string.IsNullOrWhiteSpace(t.Description) ? "" : $" - {t.Description}")}{(t.ReminderDateTime != null ? $" (Reminder: {t.ReminderDateTime.Value:f})" : "")}" == selectedItem);

            if (task != null)
            {
                if (task.IsCompleted)
                {
                    MessageBox.Show("Task is already marked as completed.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                task.IsCompleted = true;
                RefreshTaskList();
                AppendChatMessage($"Task marked as completed: {task.Title}");
                activityLog.Add($"{DateTime.Now}: Task '{task.Title}' marked as completed via UI.");
            }
            else
            {
                MessageBox.Show("Could not find the selected task in the task list.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (TaskListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a task to delete.", "No Task Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string selectedItem = TaskListBox.SelectedItem.ToString();
            var task = tasks.FirstOrDefault(t => $"{(t.IsCompleted ? "✅ " : "")}{t.Title}{(string.IsNullOrWhiteSpace(t.Description) ? "" : $" - {t.Description}")}{(t.ReminderDateTime != null ? $" (Reminder: {t.ReminderDateTime.Value:f})" : "")}" == selectedItem);

            if (task != null)
            {
                tasks.Remove(task);
                RefreshTaskList();
                AppendChatMessage($"Task deleted: {task.Title}");
                activityLog.Add($"{DateTime.Now}: Task '{task.Title}' deleted via UI.");
            }
            else
            {
                MessageBox.Show("Could not find the selected task in the task list.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // TaskItem class
        public class TaskItem
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public DateTime? ReminderDateTime { get; set; }
            public bool IsCompleted { get; set; }
            public bool ReminderNotified { get; set; }
        }

        // QuizQuestion class
        public class QuizQuestion
        {
            public string Question { get; }
            public string[] Options { get; }
            public string Answer { get; }

            public QuizQuestion(string question, string[] options, string answer)
            {
                Question = question;
                Options = options;
                Answer = answer.ToUpper();
            }
        }
    }
}
