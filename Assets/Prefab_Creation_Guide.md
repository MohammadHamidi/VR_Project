# VR Rehab Framework - Prefab Creation Guide

## 📋 Overview

This guide provides step-by-step instructions for creating all the essential prefabs needed for the VR Rehab Framework. These prefabs are crucial for the framework's functionality and must be created before using the setup scripts.

---

## 🔧 Prerequisites

### Required Unity Packages
- XR Interaction Toolkit (version 2.4.0 or later)
- TextMesh Pro (for UI text)
- Oculus XR Plugin (for VR support)

### Required Assets
- Oculus Integration (for VR hands and controllers)
- DOTween (for animations)

---

## 🏗️ Core Prefabs

### 1. XR_Template.prefab - VR Environment Setup

**Purpose**: Sets up the complete VR environment with controllers, hands, and interaction systems.

#### Step-by-Step Creation:

1. **Create New Prefab**:
   - Right-click in Project window → Create → Prefab
   - Name: `XR_Template`

2. **Add XR Origin (XR Rig)**:
   - Add GameObject to prefab root
   - Name: `XR Origin (XR Rig)`
   - Add component: `XR Origin` (from XR Interaction Toolkit)
   - Configure:
     - **Camera**: Set to Main Camera child
     - **Requested Tracking Origin Mode**: `Floor`
     - **Camera Y Offset**: `1.1176`

3. **Add Main Camera**:
   - Add child GameObject to XR Origin
   - Name: `Main Camera`
   - Add components:
     - `Camera` (remove existing, add new)
     - `Audio Listener`
     - `Tracked Pose Driver` (from XR Interaction Toolkit)
   - Camera settings:
     - Clear Flags: `Solid Color`
     - Background: RGB(25, 41, 59)
     - Clipping Planes: Near=0.01, Far=1000
     - Target Eye: `Both`

4. **Add Left Controller**:
   - Add child GameObject to XR Origin
   - Name: `Left Controller`
   - Add components:
     - `XR Controller` (from XR Interaction Toolkit)
     - `XR Ray Interactor` (from XR Interaction Toolkit)
     - `XR Interaction Group` (from XR Interaction Toolkit)
     - `Line Renderer`
   - XR Controller settings:
     - **Update Tracking Type**: `Update And Before Render`
     - **Enable Input Tracking**: `true`
     - **Enable Input Actions**: `true`
     - **Model Prefab**: Assign Oculus Left Controller model
     - **Select Action**: `XRI LeftHand/Select`
     - **Activate Action**: `XRI LeftHand/Activate`
     - **UI Press Action**: `XRI LeftHand/UIPress`

5. **Add Right Controller**:
   - Duplicate Left Controller
   - Rename: `Right Controller`
   - Update XR Controller settings:
     - **Select Action**: `XRI RightHand/Select`
     - **Activate Action**: `XRI RightHand/Activate`
     - **UI Press Action**: `XRI RightHand/UIPress`
     - **Model Prefab**: Assign Oculus Right Controller model

6. **Add Interaction Manager**:
   - Add GameObject to prefab root
   - Name: `XR Interaction Manager`
   - Add component: `XR Interaction Manager`

7. **Add Locomotion System**:
   - Add GameObject to prefab root
   - Name: `LocomotionSystem`
   - Add component: `Locomotion System` (from XR Interaction Toolkit)
   - Configure:
     - **XR Origin**: Reference to XR Origin
     - **Timeout**: `10`

8. **Add Movement Provider**:
   - Add child GameObject to LocomotionSystem
   - Name: `Continuous Move`
   - Add component: `Continuous Move Provider (Action-based)`
   - Configure:
     - **System**: Reference to LocomotionSystem
     - **Move Speed**: `1`
     - **Left Hand Move Action**: `XRI LeftHand/Primary2DAxis`
     - **Right Hand Move Action**: `XRI RightHand/Primary2DAxis`

9. **Add Turn Provider**:
   - Add child GameObject to LocomotionSystem
   - Name: `Snap Turn`
   - Add component: `Snap Turn Provider (Action-based)`
   - Configure:
     - **System**: Reference to LocomotionSystem
     - **Turn Amount**: `45°`
     - **Right Hand Turn Action**: `XRI RightHand/Primary2DAxis`

10. **Add Camera Offset**:
    - Add child GameObject to XR Origin
    - Name: `Camera Offset`
    - Position: `(0, 1.1176, 0)`
    - Move Main Camera as child of Camera Offset

11. **Add XR Hands (Optional)**:
    - Add GameObject to XR Origin
    - Name: `XR Hands`
    - Add component: `OVR Hand`
    - Configure for both left and right hands

---

### 2. Canvas.prefab - UI System

**Purpose**: Main UI canvas for displaying game information and controls.

#### Step-by-Step Creation:

1. **Create New Prefab**:
   - Right-click in Project window → Create → Prefab
   - Name: `Canvas`

2. **Add Canvas Component**:
   - Add component: `Canvas`
   - Configure:
     - **Render Mode**: `World Space`
     - **Width**: `2`
     - **Height**: `1`
     - **Plane Distance**: `100`
     - **Event Camera**: Leave empty (will be set at runtime)

3. **Add Canvas Scaler**:
   - Add component: `Canvas Scaler`
   - Configure:
     - **UI Scale Mode**: `Scale With Screen Size`
     - **Reference Resolution**: `800x600`
     - **Screen Match Mode**: `Match Width Or Height`
     - **Match**: `0.5`

4. **Add Graphic Raycaster**:
   - Add component: `Graphic Raycaster`

5. **Add Tracked Device Graphic Raycaster**:
   - Add component: `Tracked Device Graphic Raycaster` (from XR Interaction Toolkit)
   - Configure:
     - **Block Outside ScrollViews**: `true`

6. **Create UI Panel**:
   - Add child GameObject: `MainPanel`
   - Add component: `Image`
   - Configure:
     - **Color**: RGBA(32, 32, 32, 230) - semi-transparent dark
     - **Raycast Target**: `true`

7. **Add RectTransform settings**:
   - **Anchors**: Min(0.5, 0.5), Max(0.5, 0.5)
   - **Pivot**: (0.5, 0.5)
   - **Size**: (600, 400)
   - **Position**: (0, 0.2, 0) - slightly above center

---

### 3. Ball.prefab - Throwing Ball

**Purpose**: The ball that players throw at targets in the throwing exercise.

#### Step-by-Step Creation:

1. **Create New Prefab**:
   - Right-click in Project window → Create → Prefab
   - Name: `Ball`

2. **Add Sphere Primitive**:
   - Create Sphere GameObject
   - Scale: `(0.3, 0.3, 0.3)`
   - Position: `(0, 1.34, 0)`

3. **Add Required Components**:
   - `Rigidbody`
     - **Mass**: `1`
     - **Drag**: `0`
     - **Angular Drag**: `0.05`
     - **Use Gravity**: `false` (will be enabled on throw)
     - **Constraints**: None
   - `Sphere Collider`
     - **Radius**: `0.5`
     - **Is Trigger**: `false`
   - `XR Grab Interactable` (from XR Interaction Toolkit)
     - **Interaction Layer Mask**: Everything
     - **Colliders**: Add Sphere Collider
   - `HoverAndRelease` (custom script)

4. **Add Visual Components**:
   - `Mesh Renderer`
     - Assign a suitable material
   - `Mesh Filter`
     - Mesh: `Sphere`

5. **Configure HoverAndRelease Script**:
   - **Hover Amplitude**: `0.1`
   - **Hover Duration**: `1`
   - **Rotation Duration**: `5`

6. **Set Tag**:
   - Tag: `Throwable`

---

### 4. Target Ring.prefab - Throwing Target

**Purpose**: The ring targets that players aim for in the throwing exercise.

#### Step-by-Step Creation:

1. **Create New Prefab**:
   - Right-click in Project window → Create → Prefab
   - Name: `Target Ring`

2. **Import Ring Model**:
   - Use the `Ring.fbx` model from your project
   - Or create a custom ring using torus primitive

3. **Add Required Components**:
   - `TargetRing` (custom script)
     - Configure hit VFX and SFX references if available
   - `Sphere Collider`
     - **Is Trigger**: `true`
     - **Radius**: `1.0`
     - **Center**: `(0, 0.33, 0.16)` - adjust based on ring geometry

4. **Add Visual Components**:
   - `Mesh Renderer`
     - Assign ring material
   - `Mesh Filter`
     - Assign ring mesh

5. **Position and Scale**:
   - Position: `(0, 1.62, 1.3)` - example position
   - Scale: `(1, 1, 1)`

---

### 5. spawnzone.prefab - Ball Spawn Zone

**Purpose**: Defines the area where balls spawn and respawn after being thrown.

#### Step-by-Step Creation:

1. **Create New Prefab**:
   - Right-click in Project window → Create → Prefab
   - Name: `spawnzone`

2. **Add Base Object**:
   - Create Cube primitive
   - Name: `SpawnZone`
   - Scale: `(2, 0.1, 2)`
   - Position: `(0, 0, 0)`

3. **Add Required Components**:
   - `Box Collider`
     - **Size**: `(2, 0.1, 2)`
     - **Is Trigger**: `true`
   - `BallRespawnZone` (custom script)

4. **Add Visual Feedback (Optional)**:
   - `Mesh Renderer` with semi-transparent material
   - Color: RGBA(0, 255, 0, 100) - green tint

5. **Add Spawn Point**:
   - Add child GameObject: `SpawnPoint`
   - Position: `(0, 1, 0)` - above the zone
   - This will be referenced by the ball spawner

---

## 🏗️ Manager Prefabs

### 6. ProgressionManager.prefab

**Purpose**: Manages player progression through exercises and difficulty levels.

#### Step-by-Step Creation:

1. **Create New Prefab**:
   - Right-click in Project window → Create → Prefab
   - Name: `ProgressionManager`

2. **Add Empty GameObject**:
   - Add component: `ProgressionSystem`

3. **Configure ProgressionSystem**:
   - **Min Success Rate**: `0.7`
   - **Min Attempts**: `3`
   - **Mastery Threshold**: `0.85`
   - **Enable Adaptive Difficulty**: `true`
   - **Adaptation Speed**: `0.3`

---

### 7. DataManager.prefab

**Purpose**: Handles patient data persistence and profile management.

#### Step-by-Step Creation:

1. **Create New Prefab**:
   - Right-click in Project window → Create → Prefab
   - Name: `DataManager`

2. **Add Empty GameObject**:
   - Add component: `DataPersistenceManager`

3. **Configure DataPersistenceManager**:
   - **Save File Name**: `patient_profiles.json`
   - **Auto Save**: `true`
   - **Auto Save Interval**: `30`
   - **Create Backup**: `true`
   - **Max Backups**: `5`
   - **Data Directory**: `PatientData`

---

### 8. AnalyticsManager.prefab

**Purpose**: Tracks player performance and generates analytics reports.

#### Step-by-Step Creation:

1. **Create New Prefab**:
   - Right-click in Project window → Create → Prefab
   - Name: `AnalyticsManager`

2. **Add Empty GameObject**:
   - Add component: `PerformanceAnalytics`

3. **Configure PerformanceAnalytics**:
   - **Enable Real-time Tracking**: `true`
   - **Data Collection Interval**: `1`
   - **Max Data Points**: `1000`
   - **Trend Window Size**: `10`
   - **Enable Predictive Analytics**: `true`

---

## 🎨 Materials and Textures

### Required Materials:

1. **Ball Material**:
   - Create new Material: `Ball_Material`
   - Shader: `Standard`
   - Albedo Color: Bright color (red, blue, or green)
   - Metallic: `0`
   - Smoothness: `0.5`

2. **Ring Material**:
   - Create new Material: `Ring_Material`
   - Shader: `Standard`
   - Albedo Color: Contrasting color (yellow or white)
   - Metallic: `0.8`
   - Smoothness: `0.8`

3. **UI Panel Material**:
   - Create new Material: `UI_Panel_Material`
   - Shader: `UI/Default`
   - Color: Semi-transparent dark

---

## 🧪 Testing Prefabs

### Test Checklist:

1. **XR_Template.prefab**:
   - [ ] Controllers appear in VR
   - [ ] Ray interactors work
   - [ ] Movement and turning function
   - [ ] No console errors

2. **Canvas.prefab**:
   - [ ] UI appears in world space
   - [ ] Raycasting works with VR controllers
   - [ ] Text renders correctly

3. **Ball.prefab**:
   - [ ] Can be grabbed with VR controllers
   - [ ] Physics work when thrown
   - [ ] Respawns correctly
   - [ ] Hover animation plays

4. **Target Ring.prefab**:
   - [ ] Collision detection works
   - [ ] Hit feedback triggers
   - [ ] Visual feedback appears

5. **spawnzone.prefab**:
   - [ ] Ball respawns when entering zone
   - [ ] Trigger collider works
   - [ ] Spawn point positioned correctly

---

## 🔧 Advanced Configuration

### Customizing for Different Exercises:

1. **Multiple Ball Types**:
   - Create variations with different masses, sizes, colors
   - Different hover/rotation speeds

2. **Advanced Targets**:
   - Moving targets with animation
   - Multi-ring targets with scoring zones
   - Interactive targets with special effects

3. **Environment Integration**:
   - Add custom materials for different themes
   - Multiple ring arrangements
   - Dynamic spawn zone positioning

---

## 🚀 Usage Instructions

After creating all prefabs:

1. **Place in Assets/Prefabs/** directory
2. **Assign to Setup Scripts**:
   - Open each setup script (CompleteVRSetup, etc.)
   - Assign prefabs to the corresponding fields in the inspector
3. **Test in Unity**:
   - Create new scene
   - Add setup script
   - Press Play to test
4. **Build and Deploy**:
   - Add scenes to Build Settings
   - Test on VR headset

---

## 🆘 Troubleshooting

### Common Issues:

1. **Prefabs not appearing**:
   - Check prefab assignments in inspector
   - Verify prefabs are in correct directory
   - Ensure no missing components

2. **VR not working**:
   - Verify XR packages are installed
   - Check Oculus settings in Project Settings
   - Ensure VR headset is connected

3. **Physics not working**:
   - Check Rigidbody settings
   - Verify colliders are configured
   - Ensure layers are correct

4. **UI not responding**:
   - Check Canvas render mode
   - Verify raycasters are attached
   - Ensure XR Interaction Manager is present

---

## 📝 Notes

- All prefabs should be created in the `Assets/Prefabs/` directory
- Test each prefab individually before combining
- Keep prefab references organized and documented
- Version control your prefabs for easy rollback

**With these prefabs created, you'll have a complete foundation for your VR Rehab Framework!** 🎯
