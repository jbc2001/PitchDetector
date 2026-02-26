// PitchDetector.cs
// Copyright (c) 2026 James Collins (jbc2001)
// Licensed under the MIT License. See LICENSE file in PitchDetector folder.
using Godot;
using System;
using System.Linq;

[GlobalClass]
[Icon("res://addons/PitchDetector/icon.svg")]

public partial class PitchDetector : Node {
    public static PitchDetector Instance { get; private set; }

    [Export]
    public int BufferSize = 2048;   // Size of audio buffer for pitch detection
    [Export] 
    public string AudioBusName = "Record";
    [Export]
    public float minFreq = 30f;
    [Export]
    public float maxFreq = 1000f;
    [Export]
    public float noiseThreshold = 0f;
    [Export]
    public bool disableFreqComparison = false;
    [Export]
    public bool InputConfiguredExternally = false; // Set to true if user will configure audio input instead of using default
    [Signal]
    public delegate void PitchChangedEventHandler(PitchInfo pitch);

    private AudioEffectCapture capture; // Audio capture effect from the "Record" bus
    private AudioStreamPlayer micPlayer; // Player to route microphone input
    public PitchInfo CurrentPitch { get; private set; }  // Current detected pitch information

    // Initialize microphone input and audio bus
    public override void _EnterTree() {
		
        if (Instance != null) {
            QueueFree(); // prevent duplicates
            return;
        }
        Instance = this;

        // Set up audio bus and microphone input
        int busIndex = AudioServer.GetBusIndex(AudioBusName);
        if (busIndex == -1) {
            GD.PrintErr($"Audio bus '{AudioBusName}' not found.");
            CurrentPitch = new PitchInfo();
            Instance = null;
            return;
        }
        if (!InputConfiguredExternally) {
            var devices = AudioServer.GetInputDeviceList();
            // Log available input devices
            foreach (var device in devices) {
                GD.Print($"Input Device: {device}");
            }
            //handle no input devices
            if (devices.Length == 0) {
                GD.PrintErr("No input devices available.");
                CurrentPitch = new PitchInfo();
                Instance = null;
                return;
            }
        }

        // Create and configure AudioStreamPlayer for microphone input
        micPlayer = new AudioStreamPlayer();
        micPlayer.Stream = new AudioStreamMicrophone();
        micPlayer.Bus = AudioBusName;
        AddChild(micPlayer);
        micPlayer.Play();

        // Set the input device to the first available device
        capture = (AudioEffectCapture)AudioServer.GetBusEffect(busIndex, 0);

        CurrentPitch = new PitchInfo();
        CurrentPitch.Frequency = 0f;
        CurrentPitch.Note = "--";
        CurrentPitch.CentsOffset = 0f;


    }

    // Clean up resources on exit
    public override void _ExitTree() {
        if (micPlayer != null) {
            micPlayer.Stop();
            micPlayer.QueueFree(); // Remove from scene tree
            micPlayer = null;
        }

        capture = null; // release reference
        CurrentPitch = new PitchInfo(); // reset
        if (Instance == this) Instance = null;
    }


    // Process audio input and perform pitch detection each frame
    public override void _Process(double delta) {
        // Ensure audio capture is initialized
        if (capture == null) return;

        // Check if enough frames are available for processing
        var framesAvailable = capture.GetFramesAvailable();
        if (framesAvailable < BufferSize) {
            return;
        }

        // Retrieve audio buffer and process samples
        var buffer = capture.GetBuffer(BufferSize);
        if (buffer.Length == 0) {
            return;
        }

        // Convert stereo buffer to mono and apply Hanning window
        float[] samples = buffer.Select(v => (v.X + v.Y) * 0.5f).ToArray();
        for (int i = 0; i < samples.Length; i++) {
            samples[i] *= 0.5f * (1 - MathF.Cos(2 * MathF.PI * i / (samples.Length - 1)));
        }

        // Perform pitch detection
        var pitchInfo = GetPitch(samples, (float)AudioServer.GetMixRate(), minFreq, maxFreq, noiseThreshold);

        //set current pitch and emit signal if changed
        if(pitchInfo.Frequency != CurrentPitch.Frequency || disableFreqComparison) {
            CurrentPitch = pitchInfo;
            EmitSignal(SignalName.PitchChanged, CurrentPitch);
        }
    }
    /// <summary>
    /// Sets the minimum frequency value that will be considered for detection.
    /// </summary>
    /// <param name="frequency">Must be a non-negative value.</param>
    public void SetMinFrequency(float frequency) {
        if (frequency <= 0f || frequency >= maxFreq) {
            GD.PrintErr($"Invalid minFreq: {frequency}. Must be > 0 and < maxFreq ({maxFreq}).");
            return;
        }
        minFreq = frequency;
    }
    /// <summary>
    /// Sets the maximum frequency value that will be considered for detection.
    /// </summary>
    /// <param name="frequency">Must be a non-negative value.</param>
    public void SetMaxFrequency(float frequency) {
        if (frequency <= minFreq) {
            GD.PrintErr($"Invalid maxFreq: {frequency}. Must be > minFreq ({minFreq}).");
            return;
        }
        maxFreq = frequency;
    }
    /// <summary>
    /// Sets the noise threshold value that will be considered for detection.
    /// When detecting, if energy of audio signal is below the threshold, PitchInfo will be set to empty.
    /// </summary>
    /// <param name="threshold">Must be a non-negative value.</param>
    public void SetNoiseThreshold(float threshold)
    {
        noiseThreshold = threshold;
    }
    /// <summary>
    /// Sets whether the comparison between last frequency and current frequency will be ignored in detection.
    /// </summary>
    /// <param name="disable"></param>
    public void SetDisableFrequencyComparison(bool disable)
    {
        disableFreqComparison = disable;
    }
    /// <summary>
    /// Sets the audio bus name used for microphone input.
    /// </summary>
    /// <param name="busName"></param>
    public void SetAudioBusName(string busName) {
        int busIndex = AudioServer.GetBusIndex(busName);
        if (busIndex == -1) {
            GD.PrintErr($"Audio bus '{busName}' not found.");
            return;
        }
        AudioBusName = busName;
        if (micPlayer != null) {
            micPlayer.Bus = AudioBusName;
        }
        capture = (AudioEffectCapture)AudioServer.GetBusEffect(busIndex, 0);
    }
    /// <summary>
    /// Sets the buffer size used for pitch detection.
    /// </summary>
    /// <param name="size">The buffer size in samples.</param>
    public void SetBufferSize(int size) {
        BufferSize = size;
    }

    // gets pitch information from audio samples
    private static PitchInfo GetPitch(float[] samples, float sampleRate, float minFreq, float maxFreq, float noiseThreshold) {
        var output = new PitchInfo();
        int size = samples.Length;
        int maxLag = size / 2;
        float[] autocorr = new float[maxLag];

        // calculate signal energy for normalization
        float energy = 0f;
        for (int i = 0; i < size; i++)
            energy += samples[i] * samples[i];

        output.Energy = energy;

        if (energy <= noiseThreshold) {
            output.Frequency = 0f;
            output.Note = "--";
            output.CentsOffset = 0f;
            return output;
        }

        // perform autocorrelation on samples
        for (int lag = 0; lag < maxLag; lag++) {
            float sum = 0f;
            for (int i = 0; i < size - lag; i++)
                sum += samples[i] * samples[i + lag];

            autocorr[lag] = sum / energy; // normalize
        }



        int minLag = Math.Max(1, (int)(sampleRate / maxFreq));
        int maxValidLag = Math.Min((int)(sampleRate / minFreq), maxLag - 1);

        // find strongest peak in valid lag range
        int bestLag = 0;
        float bestValue = 0f;

        for (int i = minLag + 1; i < maxValidLag - 1; i++) {
            if (autocorr[i] > bestValue &&
                autocorr[i] > autocorr[i - 1] &&
                autocorr[i] > autocorr[i + 1]) {

                bestValue = autocorr[i];
                bestLag = i;
            }
        }

        // return invalid pitch if no peak found
        if (bestLag == 0) {
            output.Frequency = 0f;
            output.Note = "--";
            output.CentsOffset = 0f;
            return output;
        }

        // refine peak using parabolic interpolation
        float y0 = autocorr[bestLag - 1];
        float y1 = autocorr[bestLag];
        float y2 = autocorr[bestLag + 1];

        float shift = 0f;
        float denom = y0 - 2f * y1 + y2;
        if (denom != 0f) {
            shift = 0.5f * (y0 - y2) / denom;
        }
        float refinedLag = bestLag + shift;

        // calculate frequency from refined lag
        float frequency = sampleRate / refinedLag;
        string note = NoteName(frequency);

        // calculate cents offset from nearest note
        float centsOffset = 0f;
        if (frequency > 0f) {
            double a4 = 440.0;
            double semitones = 12 * Math.Log(frequency / a4, 2);
            double roundedSemitones = Math.Round(semitones);
            centsOffset = (float)(100 * (semitones - roundedSemitones));
        }

        // return detected pitch information
        output.Frequency = frequency;
        output.Note = note;
        output.CentsOffset = centsOffset;
        return output;
    }


    // Convert frequency to note name
    private static string NoteName(float frequency) {
        //return invalid note for non-positive frequencies
        if (frequency <= 0) {
            return "--";
        }

        // Calculate note name from frequency
        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        double a4 = 440.0;  // Reference frequency for A4

        // Calculate number of semitones from A4
        double semitones = 12 * Math.Log(frequency / a4, 2);

        // Determine note index and octave
        int noteIndex = (int)Math.Round(semitones) + 9 + 12 * 4;
        int octave = Math.Clamp(noteIndex / 12, 0, 9);
        int note = noteIndex % 12;

        //return note in Letter+Octave format "E2"
        return $"{noteNames[(note + 12) % 12]}{octave}";
    }
}

[GlobalClass]
public partial class PitchInfo : Resource {
    [Export] public float Frequency { get; set; }
    [Export] public string Note { get; set; } = "--";
    [Export] public float CentsOffset { get; set; }
    [Export] public float Energy { get; set; }

    public bool IsValid => Frequency > 0 && Note != "--";
    public override string ToString() => $"{Note} ({Frequency:F2} Hz, {CentsOffset:+0.##;-0.##} cents, {Energy:F4} energy)";

}
