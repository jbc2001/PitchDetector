// PitchDetector.cs
// Copyright (c) 2026 James Collins (jbc2001)
// Licensed under the MIT License. See LICENSE file in PitchDetector folder.

// A simple Godot Label that displays the detected pitch from PitchDetector
using Godot;
using System;

public partial class TunerDisplay : Label
{
    // Reference to the PitchDetector singleton (ensure it's added to Autoloads)
    private PitchDetector audioIn;
    
    public override void _Ready()
    {
        // Get the PitchDetector instance
        audioIn = PitchDetector.Instance;
        if (audioIn == null) {
            GD.PrintErr("PitchDetector instance not found. Ensure it is added as an Autoload.");
            return;
        }
        // Subscribe to pitch change events
        audioIn.PitchChanged += OnPitchChanged;
    }

    // Unsubscribe from events when the node is removed from the scene tree
    public override void _ExitTree()
    {
        if (audioIn != null) {
            audioIn.PitchChanged -= OnPitchChanged;
        }
    }

    // Update the pitch text when a new pitch is detected
    private void OnPitchChanged(PitchInfo pitch) {
        //ensure pitch is valid before displaying
        if (pitch.IsValid) {
            Text = $"{pitch.Note} {pitch.CentsOffset:+0;-0} cents";
        }
        else {
            Text = "No pitch detected";
        }
    }
}
