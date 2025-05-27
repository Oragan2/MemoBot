// File: Program.cs

using System;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices; // Required for Marshal.Copy

// Make sure your Vosk and PortAudioWrapper namespaces are correct
using Vosk; // Assuming Vosk is installed as a NuGet package
using VoskSpeechToTextApp; // Assuming PortAudioWrapper.cs is in the same namespace

namespace VoskSpeechToTextApp
{
    class Program
    {
        // IMPORTANT:
        // 1. Download the Vosk model (e.g., vosk-model-small-fr-0.22) from alphacephei.com/vosk/models
        // 2. Unzip it into a folder named 'model' directly in your project directory.
        //    So you should have a path like: YourProjectFolder/model/vosk-model-small-fr-0.22/...
        // 3. Ensure the model files are copied to the output directory (e.g., set 'Copy to Output Directory' to 'Copy if newer' in VS Code/Visual Studio)
        private const string ModelPath = "model/vosk-model-small-en-us-0.15"; // Adjust if you use a different model/language
        private const int SampleRate = 16000; // Vosk models are typically 16 kHz
        private const uint FramesPerBuffer = 4096; // Size of audio buffer per callback

        private static Model _model = null!;
        private static VoskRecognizer _recognizer = null!;
        private static IntPtr _audioStream = IntPtr.Zero;
        private static PortAudio.PaStreamCallback _streamCallbackDelegate = null!;

        static void Main(string[] args)
        {
            Console.WriteLine("Starting Vosk Speech-to-Text application...");

            // --- Model Path Check ---
            if (!Directory.Exists(ModelPath))
            {
                Console.WriteLine($"Error: Vosk model not found at '{ModelPath}'");
                Console.WriteLine("Please download a model from https://alphacephei.com/vosk/models,");
                Console.WriteLine("unzip it into your project's 'model' folder, and ensure its files are copied to the output directory.");
                return;
            }

            try
            {
                _model = new Model(ModelPath);
                Console.WriteLine($"Vosk model loaded from: {ModelPath}");

                _recognizer = new VoskRecognizer(_model, SampleRate);

                // --- PortAudio Initialization ---
                int err = PortAudio.Pa_Initialize();
                if (err != 0)
                {
                    string errorMessage = Marshal.PtrToStringAnsi(PortAudio.Pa_GetErrorText(err)) ?? "Unknown PortAudio Error";
                    Console.WriteLine($"Error initializing PortAudio: {errorMessage}");
                    return;
                }
                Console.WriteLine("PortAudio initialized.");

                // --- LISTING ALL AVAILABLE INPUT DEVICES ---
                // This section helps you identify your microphone's index.
                // Once you know the index (e.g., 0), you can comment out this whole listing block
                // and directly set 'targetInputDeviceIndex' below.
                Console.WriteLine("\nListing available PortAudio Input Devices:");
                int numDevices = PortAudio.Pa_GetDeviceCount();
                if (numDevices < 0)
                {
                    Console.WriteLine($"Error getting device count: {Marshal.PtrToStringAnsi(PortAudio.Pa_GetErrorText(numDevices))}");
                }
                else
                {
                    for (int i = 0; i < numDevices; i++)
                    {
                        IntPtr deviceInfoPtr = PortAudio.Pa_GetDeviceInfo(i);
                        if (deviceInfoPtr != IntPtr.Zero)
                        {
                            PortAudio.PaDeviceInfo deviceInfo = Marshal.PtrToStructure<PortAudio.PaDeviceInfo>(deviceInfoPtr);
                            if (deviceInfo.maxInputChannels > 0) // Only list input devices
                            {
                                Console.WriteLine($"- Device Index {i}: {Marshal.PtrToStringAnsi(deviceInfo.name)}");
                                Console.WriteLine($"  Max Input Channels: {deviceInfo.maxInputChannels}");
                                Console.WriteLine($"  Default Sample Rate: {deviceInfo.defaultSampleRate}");
                            }
                        }
                    }
                }
                Console.WriteLine("\n--- End Device List ---\n");
                // --- END DEVICE LISTING ---


                _streamCallbackDelegate = new PortAudio.PaStreamCallback(OnAudioDataAvailable);

                // --- OPENING AUDIO STREAM ---
                // IMPORTANT: Replace '0' with the Device Index of YOUR microphone
                // from the list above (e.g., 'VirtIO SoundCard: PCM 0 (hw:0,0)' was index 0 in your output).
                int targetInputDeviceIndex = 0; // <--- SET YOUR MICROPHONE'S DEVICE INDEX HERE!

                var inputParameters = new PortAudio.PaStreamParameters
                {
                    device = targetInputDeviceIndex,
                    channelCount = 1, // Vosk expects mono audio
                    sampleFormat = PortAudio.PaSampleFormat.PaInt16, // 16-bit signed integer format
                    suggestedLatency = 0.5 // Let PortAudio choose a suitable latency
                };

                err = PortAudio.Pa_OpenStream(
                    out _audioStream,
                    ref inputParameters, // Pass input parameters by reference
                    IntPtr.Zero,         // No output parameters (input-only stream)
                    SampleRate,          // The desired sample rate for Vosk (16000 Hz)
                    FramesPerBuffer,     // Number of frames per buffer for the callback
                    PortAudio.PaStreamFlags.PaNoFlag, // Standard flags (no special options)
                    _streamCallbackDelegate,
                    IntPtr.Zero
                );

                if (err != 0)
                {
                    string errorMessage = Marshal.PtrToStringAnsi(PortAudio.Pa_GetErrorText(err)) ?? "Unknown PortAudio Stream Error";
                    Console.WriteLine($"Error opening specific stream (Index {targetInputDeviceIndex}): {errorMessage}");
                    PortAudio.Pa_Terminate();
                    return;
                }

                // --- STARTING AUDIO STREAM ---
                err = PortAudio.Pa_StartStream(_audioStream);
                if (err != 0)
                {
                    string errorMessage = Marshal.PtrToStringAnsi(PortAudio.Pa_GetErrorText(err)) ?? "Unknown PortAudio Start Error";
                    Console.WriteLine($"Error starting stream: {errorMessage}");
                    PortAudio.Pa_CloseStream(_audioStream);
                    PortAudio.Pa_Terminate();
                    return;
                }

                Console.WriteLine($"Listening for speech on device {targetInputDeviceIndex}... (Press Ctrl+C to stop)");


                // --- Keep application running until Ctrl+C is pressed ---
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    Console.WriteLine("\nStopping recognition.");
                    eventArgs.Cancel = true; // Prevent the process from terminating immediately
                    if (_audioStream != IntPtr.Zero && PortAudio.Pa_IsStreamActive(_audioStream) == 1)
                    {
                        PortAudio.Pa_StopStream(_audioStream);
                    }
                };

                // Loop while the stream is active
                while (_audioStream != IntPtr.Zero && PortAudio.Pa_IsStreamActive(_audioStream) == 1)
                {
                    System.Threading.Thread.Sleep(100); // Small delay to prevent busy-waiting
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unhandled error occurred: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
            finally
            {
                // --- Clean up PortAudio and Vosk resources ---
                if (_audioStream != IntPtr.Zero)
                {
                    // Ensure stream is stopped before closing
                    if (PortAudio.Pa_IsStreamActive(_audioStream) == 1)
                    {
                        PortAudio.Pa_StopStream(_audioStream);
                    }
                    PortAudio.Pa_CloseStream(_audioStream);
                    _audioStream = IntPtr.Zero; // Mark as released
                }
                PortAudio.Pa_Terminate(); // Terminate PortAudio library
                _recognizer?.Dispose(); // Dispose Vosk recognizer
                _model?.Dispose();      // Dispose Vosk model
                Console.WriteLine("Application cleanup complete. Exiting.");
            }
        }

        // --- PortAudio Stream Callback ---
        private static PortAudio.PaStreamCallbackResult OnAudioDataAvailable(
            IntPtr input, IntPtr output, UInt32 frameCount,
            ref PortAudio.PaStreamCallbackTimeInfo timeInfo,
            PortAudio.PaStreamCallbackFlags statusFlags,
            IntPtr userData)
        {
            // Debugging: Print callback invocation periodically
            // if (DateTime.Now.Second % 2 == 0 && DateTime.Now.Millisecond < 200) // Reduced frequency
            // {
            //     Console.Write("."); // Print a dot to show activity
            // }

            // Check for potential input errors
            if ((statusFlags & PortAudio.PaStreamCallbackFlags.PaInputOverflow) != 0)
            {
                Console.WriteLine("\n[WARNING] PortAudio Input Overflow!");
            }
            if ((statusFlags & PortAudio.PaStreamCallbackFlags.PaInputUnderflow) != 0)
            {
                Console.WriteLine("\n[WARNING] PortAudio Input Underflow!");
            }

            // Allocate buffer for audio data. Cast to int for array size.
            byte[] buffer = new byte[(int)(frameCount * 2)]; // 2 bytes per sample for PaInt16

            // Copy audio data from native pointer to C# byte array
            Marshal.Copy(input, buffer, 0, buffer.Length);

            // Debugging: Check if the buffer contains actual audio (not all zeros)
            // bool isSilent = true;
            // for (int i = 0; i < buffer.Length; i++)
            // {
            //     if (buffer[i] != 0)
            //     {
            //         isSilent = false;
            //         break;
            //     }
            // }
            // Only print if silent and it's a "reporting" second to avoid spam
            // if (isSilent && (DateTime.Now.Second % 5 == 0 && DateTime.Now.Millisecond < 100))
            // {
            //     Console.WriteLine("\n[DEBUG] Audio buffer appears silent (all zeros). Check microphone input level.");
            // }


            // --- Process audio with Vosk recognizer ---
            if (_recognizer.AcceptWaveform(buffer, buffer.Length))
            {
                string resultJson = _recognizer.Result();
                ProcessVoskResult(resultJson);
            }
            else
            {
                string partialResultJson = _recognizer.PartialResult();
                // IMPORTANT: Uncomment the line below to see partial recognition results
                ProcessVoskPartialResult(partialResultJson);
            }

            return PortAudio.PaStreamCallbackResult.PaContinue;
        }

        // --- Vosk Result Processing ---
        private static void ProcessVoskResult(string jsonResult)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonResult))
                {
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("text", out JsonElement textElement))
                    {
                        string recognizedText = textElement.GetString() ?? string.Empty;
                        // Only print if there's actual text recognized (Vosk can return empty strings for silence)
                        if (!string.IsNullOrWhiteSpace(recognizedText))
                        {
                            Console.WriteLine($"\n-> You said: {recognizedText}");
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"\n[ERROR] Error parsing Vosk final result JSON: {ex.Message}");
            }
        }

        // --- Vosk Partial Result Processing (for debugging live recognition) ---
        private static void ProcessVoskPartialResult(string jsonPartialResult)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonPartialResult))
                {
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("partial", out JsonElement partialElement))
                    {
                        string partialText = partialElement.GetString() ?? string.Empty;
                        // Only print if there's actual partial text (Vosk can return empty strings for silence)
                        if (!string.IsNullOrWhiteSpace(partialText))
                        {
                            // Use Console.Write to keep partials on the same line, then overwrite
                            Console.Write($"\r[PARTIAL] {partialText}               "); // Add spaces to clear previous text
                        }
                        else
                        {
                            // If partial is empty, clear the line to remove old partials
                            Console.Write("\r                                                  ");
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"\n[ERROR] Error parsing Vosk partial result JSON: {ex.Message}");
            }
        }
    }
}