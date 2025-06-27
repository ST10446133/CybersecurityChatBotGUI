Cybersecurity ChatBot GUI

A WPF-based Cybersecurity Awareness Chatbot designed to educate users on best practices for online safety. This interactive assistant supports task tracking, reminders, and educational mini-games, making cybersecurity awareness engaging and practical.

Features
Interactive Chatbot: Responds to cybersecurity-related queries such as password safety, phishing, and data protection.
Task Manager: Add, complete, and view cybersecurity-related tasks or personal reminders.
Reminders: Set task descriptions and get reminded based on user-defined date and time.
Mini-Games (optional): Engage users with short cybersecurity-themed games.
Sentiment Analysis: Understands user mood based on input.
Audio Playback: Uses NAudio for voice feedback (if enabled).
User-Friendly GUI: Built with WPF (XAML) for a modern, responsive interface.

Technologies Used
C#
.NET Framework
WPF (XAML)
NAudio (for audio features)
Regex and NLP (basic tokenization and keyword detection)

Project Structure
The solution consists of a single WPF project with the following key files:
CybersecurityChatBotGUI.sln
The main Visual Studio solution file.
CybersecurityChatBotGUI/ (Project Folder)
MainWindow.xaml
Defines the graphical user interface using XAML.
MainWindow.xaml.cs
Contains the code-behind logic for handling user interaction, task management, and chatbot communication.
CyberChatBot.cs
Handles the core chatbot logic, including responses, keyword detection, and basic NLP.
Assets/
Contains supporting resources such as logo images and audio files.

How to Run
Open the solution CybersecurityChatBotGUI.sln in Visual Studio 2022 or newer.
Restore NuGet packages (e.g., NAudio if used).
Build the solution.
Run the project using F5 or Ctrl + F5.

Example Commands
Add task
Mark task as completed
Show activity log
What is phishing?
Give me a password tip
Play a game

Dependencies
NAudio - For playing audio responses
.NET WPF Libraries

Future Enhancements
Full-fledged natural language processing with LUIS or GPT APIs
Advanced quiz module
Cloud-based task sync
User login & profiles

License
MIT License – Feel free to use, modify, and distribute.
