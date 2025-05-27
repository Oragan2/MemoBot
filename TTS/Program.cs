using System;
using System.Speech.Synthesis;

namespace BasicTextToSpeech
{
    class Program
    {
        static void Main(string[] args)
        {
            // 1. Create a SpeechSynthesizer object
            using (SpeechSynthesizer synth = new SpeechSynthesizer())
            {
                // 2. Configure the output to the default audio device (speakers)
                synth.SetOutputToDefaultAudioDevice();

                // 3. Speak the text synchronously (program waits until speech finishes)
                Console.WriteLine("Speaking synchronously: Hello, this is a basic text to speech example in C#.");
                synth.Speak("Hello, this is a basic text to speech example in C#.");

                Console.WriteLine("\nSpeaking asynchronously: You can also speak without blocking the main thread.");
                // 4. Speak the text asynchronously (program continues immediately)
                synth.SpeakAsync("You can also speak without blocking the main thread.");

                // To ensure the async speech completes before the program exits in a console app:
                Console.WriteLine("Press any key to exit after asynchronous speech completes...");
                Console.ReadKey();

                // --- Optional: Customize voice and properties ---

                // List available voices
                Console.WriteLine("\nAvailable Voices:");
                foreach (InstalledVoice voice in synth.GetInstalledVoices())
                {
                    VoiceInfo info = voice.VoiceInfo;
                    Console.WriteLine($"  Name: {info.Name}, Gender: {info.Gender}, Age: {info.Age}, Culture: {info.Culture}");
                }

                // Select a specific voice (e.g., "Microsoft Zira Desktop" or "Microsoft David Desktop")
                // Replace with a voice name you found above, if available on your system.
                // try
                // {
                //     synth.SelectVoice("Microsoft Zira Desktop");
                //     Console.WriteLine("\nSwitched to Zira voice.");
                //     synth.Speak("This is Zira speaking.");
                // }
                // catch (ArgumentException ex)
                // {
                //     Console.WriteLine($"\nVoice not found: {ex.Message}");
                // }

                // Adjust volume (0 to 100) and rate (-10 to 10)
                synth.Volume = 80;   // 80% volume
                synth.Rate = 1;      // Slightly faster than default
                Console.WriteLine("\nAdjusted volume and rate. Speaking again.");
                synth.Speak("This text is spoken with adjusted volume and rate.");

                // Save speech to a WAV file
                string filePath = "output.wav";
                synth.SetOutputToWaveFile(filePath);
                synth.Speak("This text will be saved to a WAV file.");
                synth.SetOutputToDefaultAudioDevice(); // Reset output to speakers
                Console.WriteLine($"\nSpeech saved to {filePath}");

                Console.WriteLine("\nProgram finished.");
            }
        }
    }
}