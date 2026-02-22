# PitchDetector for Godot .NET

## Overview

**PitchDetector** is a real-time **monophonic pitch detection plugin** for Godot .NET projects.  
It is ideal for rhythm games, music apps, and instrument learning tools.

The plugin provides:

- Real-time pitch detection from microphone or line audio input.
- Current note, frequency, and cents offset.
- Configurable buffer size and frequency range.
- Editor dock for easy setup and live adjustments.

---

## Installation

1. Either download the plugin from the godot asset store https://godotengine.org/asset-library/asset or from here directly as a .zip
2. Copy the `PitchDetector` folder into your project’s `addons/` directory.
3. Enable the plugin in **Project → Project Settings → Plugins**.
4. Add the `PitchDetector` node to your project as an **Autoload** (singleton):  
   - Open **Project → Project Settings → Globals → Autoload**.  
   - Select the 'PitchDetector' script (`PitchDetector.cs`) and give it a name.  
   - Then click **Add**.  
   - Ensure **Enable** is checked.
5. Configure settings in the editor dock (Audio Bus, Buffer Size, Min/Max Frequency).

---

## Usage

### Accessing pitch information
```csharp
var pitchDetector = PitchDetector.Instance;
if (pitchDetector != null && pitchDetector.CurrentPitch.IsValid)
{
    GD.Print($"Note: {pitchDetector.CurrentPitch.Note}");
    GD.Print($"Frequency: {pitchDetector.CurrentPitch.Frequency} Hz");
    GD.Print($"Cents Offset: {pitchDetector.CurrentPitch.CentsOffset}");
    GD.Print($"Energy: {pitchDetector.CurrentPitch.Energy}");
}
```
---

### Listening for pitch changes
```csharp
pitchDetector.PitchChanged += OnPitchChanged;

void OnPitchChanged(PitchInfo pitch)
{
    GD.Print($"Detected note: {pitch.Note} ({pitch.Frequency:F2} Hz)");
}
```
---
### Adjusting settings
```csharp
PitchDetector.Instance?.SetAudioBusName("Record");
PitchDetector.Instance?.SetBufferSize(2048);
PitchDetector.Instance?.SetMinFrequency(40f);
PitchDetector.Instance?.SetMaxFrequency(700f);
PitchDetector.Instance?.SetNoiseThreshold(0.0f);
PitchDetector.Instance?.SetDisableFrequencyComparison(false);
```
---

### Editor Dock
The **PitchDetector** plugin provides an interface for adjusting audio settings from inside the Godot editor bottom dock.
- **Audio Bus:** *The audio bus to capture input from.*
- **Buffer Size:** *The number of samples taken per pitch detection window.*
- **Min Frequency:** *The minimum frequency the plugin will attempt to detect (Hz).*
- **Max Frequency:** *The maximum frequency the plugin will attempt to detect (Hz).*
- **Noise Threshold:** *The minimum energy required to send a valid pitch.*
---
## License
This project is licensed under the **MIT License** see the [LICENSE](LICENSE) file for details.
