# GuyOllamaAI

A cross-platform desktop application built with AvaloniaUI that connects to your local Ollama AI server. Chat with AI models running on your own machine - no API keys, no tokens, no payments required!

## Features

- **Cross-Platform**: Works on Windows, macOS, and Linux
- **Local AI**: Connects directly to your Ollama server running locally
- **Privacy-First**: All conversations stay on your machine
- **Multiple Models**: Support for any model available in Ollama
- **Modern UI**: Clean, dark-themed interface with smooth animations
- **Chat History**: Keep track of your conversations
- **Streaming Responses**: See AI responses in real-time as they generate

## Screenshots

The app features:
- An animated splash screen introducing GuyOllamaAI
- A main chat interface with message history
- Model selection dropdown
- Connection status indicator
- Settings panel for server configuration
- Built-in guide for installing Ollama

## Prerequisites

Before running GuyOllamaAI, you need to have Ollama installed and running on your machine.

### Installing Ollama

1. **Download Ollama**
   - Visit [https://ollama.ai](https://ollama.ai)
   - Download the installer for your operating system

2. **Install and Run**
   - Run the installer and follow the prompts
   - Ollama will run as a service on your machine

3. **Pull a Model**
   ```bash
   # Pull the default model
   ollama pull llama3.2

   # Or try other popular models
   ollama pull mistral
   ollama pull codellama
   ollama pull phi3
   ollama pull gemma2
   ```

4. **Verify Ollama is Running**
   ```bash
   curl http://localhost:11434/api/tags
   ```

## Building the Application

### Requirements

- .NET 8.0 SDK or later

### Build Commands

```bash
# Clone the repository
git clone https://github.com/yourusername/guy-ollama-ai.git
cd guy-ollama-ai

# Restore dependencies
dotnet restore

# Build the application
dotnet build

# Run the application
dotnet run --project src/GuyOllamaAI
```

### Publishing for Different Platforms

```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained

# macOS (Intel)
dotnet publish -c Release -r osx-x64 --self-contained

# macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained

# Linux
dotnet publish -c Release -r linux-x64 --self-contained
```

## Usage

1. **Start Ollama**: Make sure Ollama is running on your machine
2. **Launch GuyOllamaAI**: Run the application
3. **Get Started**: Click "Get Started" on the splash screen
4. **Select a Model**: Choose from available models in the dropdown
5. **Start Chatting**: Type your message and press Enter or click Send

### Configuration

- **Server URL**: Default is `http://localhost:11434`. Can be changed in Settings if Ollama runs on a different host/port.
- **Connection Status**: The sidebar shows whether the app is connected to Ollama

## Project Structure

```
guy-ollama-ai/
├── GuyOllamaAI.sln
├── README.md
└── src/
    └── GuyOllamaAI/
        ├── GuyOllamaAI.csproj
        ├── Program.cs
        ├── App.axaml
        ├── App.axaml.cs
        ├── Models/
        │   ├── ChatMessage.cs
        │   └── ChatSession.cs
        ├── Services/
        │   └── OllamaService.cs
        ├── ViewModels/
        │   ├── ViewModelBase.cs
        │   ├── SplashViewModel.cs
        │   └── MainViewModel.cs
        ├── Views/
        │   ├── SplashWindow.axaml
        │   ├── SplashWindow.axaml.cs
        │   ├── MainWindow.axaml
        │   └── MainWindow.axaml.cs
        └── Styles/
            └── AppStyles.axaml
```

## Technologies

- [AvaloniaUI](https://avaloniaui.net/) - Cross-platform .NET UI framework
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) - MVVM toolkit
- [Ollama](https://ollama.ai/) - Local AI model runner

## License

MIT License - feel free to use this project for your own purposes.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
