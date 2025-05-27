using System;
using System.Runtime.InteropServices;

public static class PortAudio
{
    // For Windows (64-bit): "portaudio_x64.dll" (you need to download this DLL and place it in your project's output directory)
    // For Windows (32-bit): "portaudio_x86.dll"
    private const string PortAudioLib = "libportaudio.so.2"; // Or "portaudio_x64.dll" for Windows

    // --- Enums and Structs ---

    // PortAudio error codes
    public enum PaError : int
    {
        PaNoError = 0,
        PaNotInitialized = -10000,
        PaUnanticipatedHostError,
        PaInvalidChannelCount,
        PaInvalidSampleRate,
        PaInvalidDevice,
        PaInvalidFlag,
        PaBadBufferPtr,
        PaBadStreamPtr,
        PaTimedOut,
        PaBufferTooBig,
        PaBufferTooSmall,
        PaNullCallback,
        PaBadStreamPtrTimeInfo,
        PaBadCbicPtr,
        PaHostApiNotFound,
        PaInvalidHostApi,
        PaCanNotReadFromACallbackStream,
        PaCanNotWriteToACallbackStream,
        PaCanNotReadFromAnOutputOnlyStream,
        PaCanNotWriteToAnInputOnlyStream,
        PaIncompatibleHostApiSpecificStreamInfo,
        PaStreamIsStopped,
        PaStreamIsNotStopped,
        PaBufferSizingError,
        PaInternalError,
        PaBufferContentsNull,
        PaInvalidHostApiType,
        PaInvalidBufferSize,
        PaStreamCallbackError,
        PaDeviceUnavailable,
        PaIncompatibleStreamHostApi,
        PaBadBufferPtrWithHostError
    }

    // PortAudio sample formats
    public enum PaSampleFormat : uint
    {
        PaFloat32 = 0x00000001,  ///< 32 bit float.
        PaInt32 = 0x00000002,    ///< 32 bit signed integer.
        PaInt24 = 0x00000004,    ///< 24 bit signed integer.
        PaInt16 = 0x00000008,    ///< 16 bit signed integer.
        PaInt8 = 0x00000010,     ///< 8 bit signed integer.
        PaUInt8 = 0x00000020,    ///< 8 bit unsigned integer.

        Pa_CustomFormat = 0x00010000, ///< Custom sample format.
        PaNonInterleaved = 0x80000000 ///< Set if samples are not interleaved. (Used as a flag)
    }

    // PortAudio stream callback return values
    public enum PaStreamCallbackResult : int
    {
        PaContinue = 0,
        PaComplete = 1,
        PaAbort = 2
    }

    // PortAudio stream callback flags
    [Flags]
    public enum PaStreamCallbackFlags : uint
    {
        PaInputUnderflow = 0x00000001,
        PaInputOverflow = 0x00000002,
        PaOutputUnderflow = 0x00000004,
        PaOutputOverflow = 0x00000008,
        PaPrimingOutput = 0x00000010
    }

    // PortAudio stream flags
    public enum PaStreamFlags
    {
        PaNoFlag = 0,
        PaClipOff = 0x00000001,
        PaDitherOff = 0x00000002,
        PaNeverDropInput = 0x00000004,
        PaPrimeOutputBuffersUsingStreamCallback = 0x00000008,
        PaDirectHostProcess = 0x00000010,
        PaNonInterleaved = 0x00000020,
        PaUnderrunCallback = 0x00000040,
        PaBufferRingEnabled = 0x00000080
    }

    // PortAudio device information
    [StructLayout(LayoutKind.Sequential)]
    public struct PaDeviceInfo
    {
        public int structVersion;
        public IntPtr name; // char*
        public int hostApi;
        public int maxInputChannels;
        public int maxOutputChannels;
        public double defaultLowInputLatency;
        public double defaultLowOutputLatency;
        public double defaultHighInputLatency;
        public double defaultHighOutputLatency;
        public double defaultSampleRate;
    }

    // PortAudio stream parameters
    [StructLayout(LayoutKind.Sequential)]
    public struct PaStreamParameters
    {
        public int device;
        public int channelCount;
        public PaSampleFormat sampleFormat;
        public double suggestedLatency;
        public IntPtr hostApiSpecificStreamInfo;
    }

    // PortAudio stream callback time info
    [StructLayout(LayoutKind.Sequential)]
    public struct PaStreamCallbackTimeInfo
    {
        public double inputBufferAdcTime;
        public double currentTime;
        public double outputBufferDacTime;
    }

    // PortAudio stream callback delegate
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate PaStreamCallbackResult PaStreamCallback(
        IntPtr input, IntPtr output, UInt32 frameCount,
        ref PaStreamCallbackTimeInfo timeInfo,
        PaStreamCallbackFlags statusFlags,
        IntPtr userData);

    // --- DllImports for PortAudio Functions ---

    [DllImport(PortAudioLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Pa_Initialize();

    [DllImport(PortAudioLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Pa_Terminate();

    [DllImport(PortAudioLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr Pa_GetErrorText(int errorCode);

    [DllImport(PortAudioLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Pa_OpenDefaultStream(
        out IntPtr stream,
        int numInputChannels,
        int numOutputChannels,
        PaSampleFormat sampleFormat,
        double sampleRate,
        uint framesPerBuffer,
        PaStreamCallback streamCallback,
        IntPtr userData
    );

    [DllImport(PortAudioLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Pa_OpenStream(
        out IntPtr stream,
        ref PaStreamParameters inputParameters,
        IntPtr outputParameters, // Use IntPtr.Zero for input-only
        double sampleRate,
        uint framesPerBuffer,
        PaStreamFlags streamFlags,
        PaStreamCallback streamCallback,
        IntPtr userData
    );

    [DllImport(PortAudioLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Pa_CloseStream(IntPtr stream);

    [DllImport(PortAudioLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Pa_StartStream(IntPtr stream);

    [DllImport(PortAudioLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Pa_StopStream(IntPtr stream);

    [DllImport(PortAudioLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Pa_IsStreamActive(IntPtr stream);

    [DllImport(PortAudioLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Pa_GetDeviceCount();

    [DllImport(PortAudioLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Pa_GetDefaultInputDevice();

    [DllImport(PortAudioLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr Pa_GetDeviceInfo(int deviceIndex);
}