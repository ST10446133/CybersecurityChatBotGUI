using System;
using System.Collections.Generic;

namespace CybersecurityAwarenessBot
{
    public class CyberChatBot
    {
        private Random random = new Random();
        private string lastTopic = "";
        private string favoriteTopic = "";

        private Dictionary<string, Func<string>> topicResponders;

        public CyberChatBot()
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

        public string ProcessInput(string input)
        {
            string userInput = input.ToLower();

            if (userInput.Contains("exit") || userInput.Contains("bye"))
            {
                return "Goodbye! Stay safe online!";
            }

            if (userInput.Contains("i'm interested in") || userInput.Contains("my favorite topic is"))
            {
                foreach (var topic in topicResponders.Keys)
                {
                    if (userInput.Contains(topic))
                    {
                        favoriteTopic = topic;
                        return $"Great! I'll remember that you're interested in {topic}. It's a crucial part of staying safe online.";
                    }
                }
                return "Thanks for sharing! I'll try to remember that.";
            }

            if (userInput.Contains("remind me my topic") || userInput.Contains("what's my favorite"))
            {
                if (!string.IsNullOrEmpty(favoriteTopic))
                {
                    return $"You told me you're interested in {favoriteTopic}. That's a smart choice!";
                }
                else
                {
                    return "You haven't told me your favorite topic yet. Feel free to share it!";
                }
            }

            if (DetectAndRespondToSentiment(userInput, out string sentimentResponse))
            {
                return sentimentResponse;
            }

            if (userInput.Contains("how are you") || userInput.Contains("how are you doing"))
            {
                return "I'm just a chatbot, but I'm doing great! Thanks for asking. How can I help you with cybersecurity today?";
            }

            if (userInput.Contains("what's your purpose") || userInput.Contains("what do you do"))
            {
                return "I'm here to help you stay safe online! I can provide tips on password safety, phishing prevention, and safe browsing.";
            }

            if (userInput.Contains("what can i ask you about"))
            {
                return "You can ask me about:\n- Password Safety\n- Phishing\n- Safe Browsing";
            }

            bool isFollowUp = userInput.Contains("another") || userInput.Contains("more") ||
                              userInput.Contains("explain") || userInput.Contains("tell me again") ||
                              userInput.Contains("i'm confused");

            if (isFollowUp)
            {
                if (!string.IsNullOrEmpty(lastTopic) && topicResponders.ContainsKey(lastTopic))
                {
                    return topicResponders[lastTopic].Invoke();
                }
                else
                {
                    return "Could you clarify what topic you're referring to? You can ask about password safety, phishing, or safe browsing.";
                }
            }

            foreach (var topic in topicResponders.Keys)
            {
                if (userInput.Contains(topic))
                {
                    lastTopic = topic;
                    return topicResponders[topic].Invoke();
                }
            }

            return "I didn't quite understand that. Could you rephrase your question or ask something related to cybersecurity?";
        }

        private bool DetectAndRespondToSentiment(string input, out string response)
        {
            if (input.Contains("worried") || input.Contains("anxious") || input.Contains("scared"))
            {
                response = "It's completely understandable to feel that way. Scammers can be very convincing. Let me share some tips to help you stay safe.\n" + GetPhishingTip();
                lastTopic = "phishing";
                return true;
            }
            else if (input.Contains("curious") || input.Contains("interested"))
            {
                response = "I'm glad you're curious! Cybersecurity knowledge is power. What would you like to learn more about?";
                return true;
            }
            else if (input.Contains("frustrated") || input.Contains("confused") || input.Contains("overwhelmed"))
            {
                response = "No worries, these topics can be tricky. I'm here to help — feel free to ask for explanations or examples anytime.";
                return true;
            }
            response = null;
            return false;
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
    }
}
