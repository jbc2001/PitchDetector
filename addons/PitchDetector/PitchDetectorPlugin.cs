// PitchDetector.cs
// Copyright (c) 2026 James Collins (jbc2001)
// Licensed under the MIT License. See LICENSE file in PitchDetector folder.
#if TOOLS
using Godot;

[Tool]
public partial class PitchDetectorPlugin : EditorPlugin {
    private EditorDock dock;
    public override void _EnterTree() {
        Script pitchDetector = GD.Load<Script>("res://addons/PitchDetector/PitchDetector.cs");
        Texture2D icon = GD.Load<Texture2D>("res://addons/PitchDetector/icon.svg");
        // Register a custom node type in the editor
        AddCustomType("PitchDetector", "Node", pitchDetector, icon
        );

        // Create a panel container
        var panel = new VBoxContainer();
        panel.Name = "PitchDetector Settings";

        // Audio bus label + input
        var label1 = new Label();
        label1.Text = "Audio Bus:";
        panel.AddChild(label1);

        var busField = new LineEdit();
        busField.Text = "Record";
        busField.TextChanged += (string newText) => {
            PitchDetector.Instance?.SetAudioBusName(newText);
        };
        panel.AddChild(busField);

        // Buffer size label + input
        var label2 = new Label();
        label2.Text = "Buffer Size:";
        panel.AddChild(label2);

        var bufferField = new SpinBox();
        bufferField.MinValue = 256;
        bufferField.MaxValue = 8192;
        bufferField.Value = 2048;
        bufferField.ValueChanged += (double val) => {
            PitchDetector.Instance?.SetBufferSize((int)val);
        };
        panel.AddChild(bufferField);

        //Min Frequency
        var label3 = new Label();
        label3.Text = "Min Frequency (Hz):";
        panel.AddChild(label3);

        var minFreqField = new SpinBox();
        minFreqField.MinValue = 1;
        minFreqField.MaxValue = 2000;
        minFreqField.Step = 1;
        minFreqField.Value = PitchDetector.Instance?.minFreq ?? 40f;
        minFreqField.ValueChanged += (double val) => {
            PitchDetector.Instance?.SetMinFrequency((float)val);
        };
        panel.AddChild(minFreqField);

        // Max Frequency
        var label4 = new Label();
        label4.Text = "Max Frequency (Hz):";
        panel.AddChild(label4);

        var maxFreqField = new SpinBox();
        maxFreqField.MinValue = 1;
        maxFreqField.MaxValue = 5000;
        maxFreqField.Step = 1;
        maxFreqField.Value = PitchDetector.Instance?.maxFreq ?? 700f;
        maxFreqField.ValueChanged += (double val) => {
            PitchDetector.Instance?.SetMaxFrequency((float)val);
        };
        panel.AddChild(maxFreqField);

        var label5 = new Label();
        label5.Text = "Noise threshold:";
        panel.AddChild(label5);

        var noiseThresholdField = new SpinBox();
        noiseThresholdField.MinValue = 0;
        noiseThresholdField.MaxValue = 10;
        noiseThresholdField.Step = 0.05;
        noiseThresholdField.Value = PitchDetector.Instance?.noiseThreshold ?? 0f;
        noiseThresholdField.ValueChanged += (double val) => {
            PitchDetector.Instance?.SetNoiseThreshold((float)val);
        };
        panel.AddChild(noiseThresholdField);

        // Wrap the panel in an EditorDock
        dock = new EditorDock();
        dock.Title = "PitchDetector Settings";
        dock.AddChild(panel);
        dock.DefaultSlot = EditorDock.DockSlot.Bottom;

        // Add the dock
        AddDock(dock);
    }

    public override void _ExitTree() {
        // Remove the custom node type when plugin is disabled
        RemoveCustomType("PitchDetector");
        if (dock != null) {
            RemoveDock(dock);
            dock.QueueFree();
            dock = null;
        }
    }
}
#endif
