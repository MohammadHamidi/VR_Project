# 🏥 Complete VR Rehab Framework Setup Guide

## 📋 Overview

This comprehensive guide will help you set up the complete VR Rehab Framework with all three mini-games:
- **🏃 Squat-Dodge Game**: Dodge drone lasers by squatting, build power meter, trigger shockwaves
- **🌉 Bridge Balance Game**: Walk across virtual bridge maintaining balance
- **🎯 Throwing Game**: Throw balls through rings with triple-ring tunnel mode

---

## 🎯 **STEP 1: Prerequisites & Project Setup**

### Required Unity Packages
1. **XR Interaction Toolkit** (2.3.0+)
2. **XR Plugin Management**
3. **Oculus XR Plugin** or **OpenXR Plugin**
4. **TextMesh Pro**
5. **DOTween** (Free from Asset Store)

### Project Settings
1. **File → Build Settings → Player Settings**
2. **XR Plug-in Management → Initialize XR on Startup ✓**
3. **XR Plug-in Management → Oculus ✓** (or OpenXR)

---

## 🏗️ **STEP 2: Core Prefabs Creation**

### 2.1 XR_Template.prefab

**Purpose**: Complete VR setup with hand tracking and interaction

**Setup Steps**:
1. **Create Empty GameObject**: `XR_Template`
2. **Add XR Origin component**
3. **Create child structure**:
   ```
   XR_Template (XR Origin)
   ├── Camera Offset
   │   ├── Main Camera (Camera, Tracked Pose Driver)
   │   ├── LeftHand Controller
   │   │   ├── Left Controller (XR Controller, XR Interactor)
   │   │   └── Hand Model
   │   └── RightHand Controller
   │       ├── Right Controller (XR Controller, XR Interactor)
   │       └── Hand Model
   └── Interaction Manager (XR Interaction Manager)
   ```

**Component Configuration**:
- **XR Origin**: Tracking Origin = Floor, Camera Y Offset = 1.36
- **Main Camera**: Tag = "MainCamera"
- **Controllers**: 
  - Left Hand = XR Controller (Left Hand)
  - Right Hand = XR Controller (Right Hand)
  - Add **XR Direct Interactor** and **XR Ray Interactor** to both

---

### 2.2 Canvas.prefab

**Purpose**: World-space UI for VR interaction

**Setup Steps**:
1. **Create UI → Canvas**: Name it `VR_Canvas`
2. **Canvas Settings**:
   - Render Mode: **World Space**
   - World Camera: Assign Main Camera
   - Sorting Layer: Default
   - Order in Layer: 0
3. **Add Components**:
   - **Canvas Scaler**: UI Scale Mode = Scale With Screen Size
   - **Graphic Raycaster**: Enable
4. **Canvas Size**: Width = 1920, Height = 1080, Scale = 0.001
5. **Add child UI elements**:
   ```
   VR_Canvas
   ├── Background Panel
   ├── Score Panel
   │   ├── Score Text
   │   ├── Lives Text
   │   └── Combo Text
   ├── Power Meter Panel
   │   ├── Power Slider
   │   └── Overcharge Indicator
   └── Status Panel
       ├── Instructions Text
       └── Progress Text
   ```

---

### 2.3 Scout Drone.prefab

**Purpose**: Fast attack drone for squat game

**Setup Steps**:
1. **Create Empty GameObject**: `Scout_Drone`
2. **Add 3D Model**: Import or use Capsule primitive
3. **Add Components**:
   - **DroneController script**
   - **Rigidbody**: Use Gravity = false, Is Kinematic = true
   - **Audio Source**: Spatial Blend = 1.0 (3D)
4. **Create child objects**:
   ```
   Scout_Drone
   ├── Model (Mesh + Material)
   ├── LaserLine (Line Renderer)
   ├── SpotLight (Light component)
   ├── TelegraphVFX (Particle System)
   └── DestroyVFX (Particle System)
   ```

**DroneController Settings**:
- Drone Type: **Scout**
- Telegraph Time: **0.6s**
- Attack Cooldown: **1.0s**
- Beam Thickness: **0.05**
- Attack Range: **15**

---

### 2.4 Heavy Drone.prefab

**Purpose**: Slow, powerful drone requiring 2 hits

**Setup Steps**:
1. **Duplicate Scout Drone prefab**
2. **Rename**: `Heavy_Drone`
3. **Scale up model**: 1.5x size
4. **Update DroneController Settings**:
   - Drone Type: **Heavy**
   - Telegraph Time: **1.2s**
   - Attack Cooldown: **2.0s**
   - Beam Thickness: **0.15**

---

### 2.5 Ball.prefab

**Purpose**: Throwable ball for throwing exercise

**Setup Steps**:
1. **Create Sphere primitive**: Scale to (0.3, 0.3, 0.3)
2. **Add Components**:
   - **Rigidbody**: Mass = 1, Use Gravity = false initially
   - **XR Grab Interactable**: Throw on Detach = true
   - **HoverAndRelease script** (existing)
   - **GrabRespawner script** (existing)
3. **Materials**: Apply ball material with good grip texture
4. **Tag**: "Throwable"

---

### 2.6 Target Ring.prefab

**Purpose**: Ring targets for throwing (both normal and tunnel mode)

**Setup Steps**:
1. **Import Ring.fbx** or create torus primitive
2. **Add Components**:
   - **TargetRing script** (existing)
   - **Trigger Collider**: Sphere or mesh collider
3. **Create center detection**:
   ```
   Target_Ring
   ├── Ring Model (Mesh + Material)
   ├── Ring Center (Empty GameObject)
   │   ├── Center Collider (Sphere, Trigger, radius = 0.07)
   │   └── RingCenter script
   ├── Hit VFX (Particle System)
   └── Audio Source
   ```

---

### 2.7 Power Meter UI.prefab

**Purpose**: Visual power meter for squat game

**Setup Steps**:
1. **Create UI Panel**: Name `PowerMeterPanel`
2. **Add child elements**:
   ```
   PowerMeterPanel
   ├── Background Image
   ├── Power Slider (UI Slider)
   │   ├── Background
   │   ├── Fill Area
   │   │   └── Fill (Image - Blue to Gold gradient)
   │   └── Handle Slide Area (disabled)
   ├── Overcharge Text
   └── Overcharge VFX (Particle System)
   ```
3. **Slider Settings**:
   - Min Value: 0, Max Value: 100
   - Fill Image: Gradient from Blue → Gold
   - No handle (Interactable = false)

---

## 🎮 **STEP 3: Scene Setup Instructions**

### 3.1 Squat Game Scene

**Scene Name**: `SquatDodgeScene`

**Required GameObjects**:

#### Core Managers
```
SquatGameSetup
├── XR_Template (Prefab instance)
├── VR_Canvas (Prefab instance)
├── SquatDodge (SquatDodge script)
├── PowerMeter (PowerMeter + UI reference)
├── ShockwaveEmitter (ShockwaveEmitter script)
├── DroneSpawner (DroneSpawner script)
├── CombatScoring (CombatScoring script)
└── DataManager (DataPersistenceManager)
```

#### Environment
```
Environment
├── Ground Plane (10x10 plane, Physics material)
├── Lighting (Directional Light)
└── Spawn Zones (Empty GameObjects for drone spawning)
```

**Setup Steps**:
1. **Create new scene**: `Assets/Scenes/SquatDodgeScene.unity`
2. **Delete default camera** (XR Template has its own)
3. **Add Core Managers**:
   - Drag **XR_Template.prefab** to scene
   - Drag **VR_Canvas.prefab** to scene
4. **Create SquatGameManager**:
   ```csharp
   // Create empty GameObject named "SquatGameManager"
   // Add these scripts:
   - SquatDodge
   - PowerMeter  
   - ShockwaveEmitter
   - DroneSpawner
   - CombatScoring
   ```
5. **Configure DroneSpawner**:
   - Scout Drone Prefab: Assign Scout_Drone.prefab
   - Heavy Drone Prefab: Assign Heavy_Drone.prefab
   - Player Transform: Assign XR Origin transform
6. **Link UI References**:
   - PowerMeter → Power Slider from Canvas
   - CombatScoring → Score/Lives/Combo texts from Canvas

---

### 3.2 Bridge Balance Scene

**Scene Name**: `BridgeBalanceScene`

**Required GameObjects**:

#### Core Setup
```
BridgeGameSetup
├── XR_Template (Prefab instance)
├── VR_Canvas (Prefab instance)  
├── BridgeBuilder (SOLIDBridgeBuilder script)
├── BalanceChecker (BalanceChecker script)
├── BridgeUIController (Existing script)
└── DataManager (DataPersistenceManager)
```

#### Environment
```
Environment
├── Water/Ground plane (Large plane below bridge)
├── Scenery (Optional environmental objects)
└── Lighting
```

**Setup Steps**:
1. **Create new scene**: `Assets/Scenes/BridgeBalanceScene.unity`
2. **Add XR and UI prefabs**
3. **Create BridgeBuilder GameObject**:
   - Add **SOLIDBridgeBuilder script**
   - Configure bridge parameters:
     ```
     Number of Planks: 10
     Total Bridge Length: 20
     Plank Width: 0.4
     Create Platforms: true
     Platform Length/Width: 2
     Auto Position Player: true
     ```
4. **Create BalanceChecker GameObject**:
   - Add **BalanceChecker script**
   - Assign XR Camera reference
   - Set balance thresholds:
     ```
     Max Offset X: 0.25
     Max Offset Z: 0.3
     Failure Delay: 0.5s
     ```
5. **Link UI elements** to BridgeUIController

---

### 3.3 Throwing Game Scene

**Scene Name**: `ThrowingScene`

**Required GameObjects**:

#### Core Setup
```
ThrowingGameSetup
├── XR_Template (Prefab instance)
├── VR_Canvas (Prefab instance)
├── LevelGenerator (LevelGenerator script)
├── TripleRingController (TripleRingController script)
├── BallSpawner (BallSpawner script)
└── DataManager (DataPersistenceManager)
```

#### Game Objects
```
ThrowingElements
├── Ball Spawn Point (Empty GameObject at waist height)
├── Rings Parent (Empty GameObject)
├── Respawn Zone (Trigger collider below play area)
└── Level Environment (Optional decorative objects)
```

**Setup Steps**:
1. **Create new scene**: `Assets/Scenes/ThrowingScene.unity`
2. **Add core prefabs**
3. **Create LevelGenerator**:
   - Add **LevelGenerator script**
   - Create/assign **ThrowingLevelData assets** in Inspector
4. **Create TripleRingController**:
   - Add **TripleRingController script**
   - Assign Ring Prefab: Target_Ring.prefab
   - Configure difficulties (Easy/Medium/Hard)
5. **Setup BallSpawner**:
   - Create spawn point at (0, 1.0, 0.6) relative to player
   - Assign Ball.prefab
   - Set respawn delay: 1.0s

---

## 🎯 **STEP 4: ScriptableObject Assets**

### 4.1 ThrowingLevelData Assets

**Create multiple level configurations**:

1. **Right-click in Project** → Create → Game → Throwing Level Data
2. **Create these assets**:
   - `Level0.asset` (Easy - 5 rings)
   - `Level1.asset` (Medium - 7 rings) 
   - `Level2.asset` (Hard - 10 rings)

**Level0 Configuration**:
```
Ball Spawn Position: (0, 1.0, 0.6)
Ring Positions: 
- (0, 1.5, 2.0)
- (0, 1.5, 3.0)  
- (0, 1.5, 4.0)
- (0, 1.5, 5.0)
- (0, 1.5, 6.0)
Stage Time: 60s
Enable Timer: true
```

---

## 🎨 **STEP 5: Materials & Visual Assets**

### 5.1 Essential Materials

Create these materials in `Assets/Materials/`:

1. **Drone_Scout_Material**:
   - Shader: Standard
   - Albedo: Light blue metallic
   - Metallic: 0.8, Smoothness: 0.6

2. **Drone_Heavy_Material**:
   - Shader: Standard  
   - Albedo: Dark red metallic
   - Metallic: 0.9, Smoothness: 0.4

3. **Laser_Telegraph_Material**:
   - Shader: Sprites/Default
   - Color: Red with 50% alpha
   - Rendering Mode: Transparent

4. **Laser_Fire_Material**:
   - Shader: Sprites/Default
   - Color: Bright red
   - Emission: Red

5. **Ring_Normal_Material**:
   - Shader: Standard
   - Albedo: White
   - Emission: Light blue

6. **Ring_Active_Material**:
   - Shader: Standard
   - Albedo: White
   - Emission: Green

---

## 🔊 **STEP 6: Audio Setup**

### 6.1 Required Audio Files

Place audio files in `Assets/Audio/`:

**Squat Game**:
- `drone_telegraph.wav` - Warning beep
- `laser_fire.wav` - Laser sound
- `drone_destroy.wav` - Explosion
- `perfect_squat.wav` - Success chime
- `shockwave.wav` - Shockwave blast
- `overcharge_start.wav` - Power-up sound

**Throwing Game**:
- `ball_grab.wav` - Ball pickup
- `ring_hit.wav` - Ring collision
- `perfect_tunnel.wav` - Success fanfare

**Bridge Game**:
- `balance_warning.wav` - Off-balance alert
- `water_splash.wav` - Fall sound

---

## ⚙️ **STEP 7: Configuration & Testing**

### 7.1 Game Manager Integration

Create a **Main Menu Scene** with navigation:

```csharp
// MainMenuController.cs
public class MainMenuController : MonoBehaviour 
{
    public void LoadSquatGame() 
    {
        SceneManager.LoadScene("SquatDodgeScene");
    }
    
    public void LoadBridgeGame() 
    {
        SceneManager.LoadScene("BridgeBalanceScene");  
    }
    
    public void LoadThrowingGame()
    {
        SceneManager.LoadScene("ThrowingScene");
    }
}
```

### 7.2 Build Settings

1. **Add all scenes** to Build Settings:
   - MainMenu (index 0)
   - SquatDodgeScene (index 1)
   - BridgeBalanceScene (index 2)
   - ThrowingScene (index 3)

2. **Configure Build**:
   - Platform: Android (for Oculus Quest)
   - Texture Compression: ASTC
   - Scripting Backend: IL2CPP
   - Target Architectures: ARM64

---

## 🧪 **STEP 8: Testing Checklist**

### 8.1 Squat Game Testing
- [ ] **VR Setup**: Controllers track properly, hand presence works
- [ ] **Squat Detection**: Standing height calibrates, squats register
- [ ] **Drone Behavior**: Drones spawn, telegraph lasers, fire accurately
- [ ] **Power Meter**: Fills with good squats, triggers overcharge
- [ ] **Shockwave**: Activates during overcharge squats, destroys drones
- [ ] **Scoring**: Points awarded correctly, combo system works
- [ ] **UI**: All text updates, power meter animates

### 8.2 Bridge Game Testing  
- [ ] **Bridge Generation**: Bridge builds automatically with physics
- [ ] **Balance Detection**: Off-balance triggers warnings and failure
- [ ] **Progress Tracking**: Position on bridge tracked accurately
- [ ] **UI Feedback**: Balance meter and progress display correctly
- [ ] **Physics**: Bridge planks react to player weight
- [ ] **Failure/Success**: Falling/completion triggers appropriately

### 8.3 Throwing Game Testing
- [ ] **Ball Physics**: Grab, throw, and respawn work correctly
- [ ] **Ring Detection**: Normal rings register hits properly
- [ ] **Tunnel Mode**: Triple-ring sequence detection works
- [ ] **Level Progression**: Advances through difficulties
- [ ] **Scoring**: Points awarded for accuracy
- [ ] **UI**: Progress and feedback display correctly

---

## 🚀 **STEP 9: Performance Optimization**

### 9.1 VR Performance Settings
```csharp
// Add to main camera or XR Origin
Application.targetFrameRate = 72; // For Quest 1
// Application.targetFrameRate = 90; // For Quest 2/3

// Graphics settings
QualitySettings.vSyncCount = 0;
QualitySettings.shadowResolution = ShadowResolution.Low;
```

### 9.2 Object Pooling
- **Drones**: Use object pooling for drone spawning
- **VFX**: Pool particle systems
- **Audio**: Pool audio sources for sound effects

---

## 🎯 **STEP 10: Final Deployment**

### 10.1 Build & Deploy
1. **Connect Oculus Quest** via USB
2. **Enable Developer Mode** on Quest
3. **Build and Run** from Unity
4. **Test all three games** in VR

### 10.2 User Instructions
Create these instructions for users:

**Setup**:
1. Put on VR headset
2. Follow calibration prompts
3. Use hand controllers to interact

**Squat Game**:
- Stand upright for calibration
- Squat to dodge red laser beams
- Perfect squats fill power meter
- During overcharge, squats create shockwaves

**Bridge Game**:
- Stay centered on bridge planks
- Walk slowly and maintain balance
- Avoid falling into water

**Throwing Game**:
- Grab balls from spawn point
- Throw through rings for points
- Try tunnel mode for perfect throws

---

## 🆘 **Troubleshooting**

### Common Issues:

**VR Not Working**:
- Check XR Plugin Management settings
- Verify Oculus app is running
- Restart Unity and Quest headset

**Scripts Missing**:
- Check all custom scripts are in correct folders
- Verify namespace imports
- Rebuild player if needed

**Performance Issues**:
- Lower graphics quality
- Reduce particle counts
- Disable shadows if needed

**Audio Not Playing**:
- Check Audio Source 3D settings
- Verify audio files are imported correctly
- Check audio mixer settings

---

## ✅ **Complete Setup Verification**

**Your VR Rehab Framework is ready when**:
- [ ] All three scenes load without errors
- [ ] VR tracking works in all scenes
- [ ] All game mechanics function correctly
- [ ] UI displays properly in VR
- [ ] Audio plays correctly
- [ ] Performance is smooth (72+ FPS)
- [ ] All prefabs are created and assigned
- [ ] Build deploys to headset successfully

**🎉 Congratulations! Your complete VR Rehab Framework is now ready for therapeutic use!**

---

## 📚 **Advanced Customization**

### Difficulty Scaling
- Modify drone spawn rates in DroneSpawner
- Adjust balance sensitivity in BalanceChecker
- Change ring sizes in TripleRingController

### Analytics Integration
- Expand PerformanceAnalytics for detailed tracking
- Add patient progress reports
- Implement session data export

### Additional Exercises
- Use existing framework to create new mini-games
- Extend with arm exercises, coordination tasks
- Add biometric integration for heart rate, etc.

---

*This framework provides a complete, therapeutic-grade VR rehabilitation system ready for clinical use.*
