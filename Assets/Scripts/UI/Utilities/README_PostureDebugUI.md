# Posture Debug UI for VR Squat Detection

This debug UI system provides real-time visualization of body posture and squat detection parameters in VR.

## Quick Setup

### Method 1: Automatic Setup (Recommended)
1. Create an empty GameObject in your scene
2. Add the `PostureDebugSetup` component to it
3. The debug UI will be automatically created and configured

### Method 2: Manual Setup
1. Create an empty GameObject
2. Add the `PostureDebugUI` component to it
3. Configure the settings in the inspector

## Features

### Real-time Information Display
- **Status**: Current squat state (READY, SQUATTING, DODGING, COOLDOWN, SIMULATING)
- **Depth**: Squat depth in meters and percentage with visual progress bar
- **Form Validation**: Whether the current posture passes validation checks
- **Threats**: Number of nearby threats and closest distance
- **Controllers**: Hand positions and symmetry information
- **Body Pose**: Knee angle, hip position, velocity, and dwell time

### Visual Indicators
- **Depth Bar**: Color-coded progress bar (red to green)
- **Status Icons**: Visual indicators for validation, threats, and dodge status
- **Color Coding**: 
  - 🟢 Green: Valid/Good
  - 🟡 Yellow: Warning/Cooldown
  - 🔴 Red: Invalid/Error
  - ⚪ White: Neutral

### Keyboard Shortcuts
- **F1**: Toggle UI visibility
- **F2**: Show detailed debug info in console
- **F3**: Force UI update
- **L**: Simulate squat (if enabled in SquatDodge)

## Configuration

### PostureDebugUI Settings
- `showInVR`: Show UI in VR builds
- `showInEditor`: Show UI in editor
- `updateInterval`: How often to update the UI (default: 0.1s)
- `autoPosition`: Automatically position UI in front of camera
- `vrOffset`: Position offset for VR (default: 2m in front)

### PostureDebugSetup Settings
- `autoCreateOnStart`: Create UI automatically when scene starts
- `destroyAfterSetup`: Remove setup script after creating UI
- `enableKeyboardShortcuts`: Enable F1-F3 shortcuts

## Troubleshooting

### UI Not Showing
1. Check if `showInVR`/`showInEditor` is enabled
2. Press F1 to toggle visibility
3. Check console for initialization errors

### No Data Displayed
1. Ensure SquatDodge script is present in scene
2. Check if XR Camera is properly assigned
3. Press F2 to see detailed debug info

### Performance Issues
1. Increase `updateInterval` to reduce update frequency
2. Disable UI when not needed (F1)

## Integration with SquatDodge

The UI automatically connects to the SquatDodge singleton and displays:
- Current squat depth and validation status
- Threat detection information
- Controller positions and symmetry
- Body pose estimation (knee angle, hip position)
- Movement velocity and dwell times

## Customization

You can extend the UI by:
1. Adding new text elements in `CreateTextElement()`
2. Creating new visual indicators
3. Modifying the update methods to show additional data
4. Changing colors and layout in the inspector

## Notes

- The UI is designed for debugging and should be disabled in production builds
- All measurements are in meters and degrees
- The UI automatically positions itself in front of the VR camera
- Color coding helps quickly identify issues with squat detection
