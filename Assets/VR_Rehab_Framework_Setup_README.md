# VR Rehab Framework Setup Guide

## 🎯 Overview

This comprehensive guide will help you set up and run the VR Rehab Framework - a complete virtual reality orthopedic rehabilitation system with advanced analytics, patient management, and therapeutic exercises.

## 📋 Prerequisites

- Unity 2021.3 or later
- Oculus Integration package
- XR Interaction Toolkit
- TextMesh Pro
- DOTween (for animations)

## 🚀 Quick Start

### Option 1: Use CompleteVRSetup Script (Recommended)

1. **Create a new scene** in Unity
2. **Add an empty GameObject** called "VRSetup"
3. **Add the CompleteVRSetup component**
4. **Assign the required prefabs** in the inspector:
   - XR Template Prefab
   - Canvas Prefab
   - Progression System Prefab
   - Data Manager Prefab
   - Analytics Manager Prefab
5. **Set the Scene Type** (MainMenu, ThrowingExercise, BridgeExercise, etc.)
6. **Press Play** - the system will auto-setup everything!

### Option 2: Manual Setup

Follow the detailed setup instructions below for each scene type.

---

## 🎮 Scene Setup Instructions

### 1. Main Menu Scene

**Purpose**: Patient selection and exercise navigation

**Setup Steps:**
1. Create new scene: `MainMenu`
2. Add GameObject: `SceneSetup`
3. Add component: `MainMenuSetup`
4. Assign prefabs:
   - XR Template: `XR_Template.prefab`
   - Canvas: `Canvas.prefab`
5. Add GameObject: `SceneLoader`
6. Add component: `SceneLoader`

### 2. Throwing Exercise Scene

**Purpose**: Ball throwing rehabilitation exercise

**Setup Steps:**
1. Create new scene: `Throwing`
2. Add GameObject: `SceneSetup`
3. Add component: `ThrowingSceneSetup`
4. Assign prefabs:
   - XR Template: `XR_Template.prefab`
   - Canvas: `Canvas.prefab`
   - Ball: `Ball.prefab`
   - Target Ring: `Target Ring.prefab`
   - Spawn Zone: `spawnzone.prefab`
5. Configure settings:
   - Number of Rings: 5
   - Ring Spacing: 2.0
   - Ring Height: 1.5
   - Ring Distance: 3.0

### 3. Bridge Building Scene

**Purpose**: Physics-based bridge construction exercise

**Setup Steps:**
1. Create new scene: `Bridge`
2. Add GameObject: `SceneSetup`
3. Add component: `BridgeSceneSetup`
4. Assign prefabs:
   - XR Template: `XR_Template.prefab`
   - Canvas: `Canvas.prefab`
5. Create BridgeConfiguration asset:
   - Number of Planks: 8
   - Total Bridge Length: 12
   - Plank Width: 1.5
   - Create Platforms: true
6. Add ground plane (optional)

### 4. Squat Exercise Scene

**Purpose**: Lower body rehabilitation exercise

**Setup Steps:**
1. Create new scene: `Squat`
2. Add GameObject: `SceneSetup`
3. Add component: `SquatSceneSetup` (create if needed)
4. Follow similar pattern as other exercises

---

## 🔧 Core System Setup

### Required Prefabs

Create these prefabs in `Assets/Prefabs/`:

#### 1. Manager Prefabs

**ProgressionManager.prefab:**
- Add `ProgressionSystem` component

**DataManager.prefab:**
- Add `DataPersistenceManager` component

**AnalyticsManager.prefab:**
- Add `PerformanceAnalytics` component

#### 2. UI Prefab

**Canvas.prefab:**
- Canvas component (Render Mode: World Space)
- Canvas Scaler
- Graphic Raycaster

#### 3. Exercise Prefabs

**Ball.prefab:**
- Sphere mesh
- Rigidbody (Use Gravity: false)
- XR Grab Interactable
- HoverAndRelease script

**Target Ring.prefab:**
- Ring model/mesh
- Sphere collider (Trigger: true)
- TargetRing script

**spawnzone.prefab:**
- BallRespawnZone script
- Appropriate collider

---

## 🎯 System Features

### Core Systems
- ✅ **VR Integration**: Full Oculus Quest support
- ✅ **Patient Management**: Profile creation and tracking
- ✅ **Progress Tracking**: Exercise performance monitoring
- ✅ **Analytics**: Advanced performance analysis
- ✅ **Data Persistence**: JSON-based save system
- ✅ **UI Management**: Comprehensive interface system

### Exercise Types
- 🎯 **Ball Throwing**: Target accuracy training
- 🌉 **Bridge Building**: Physics-based construction
- 🏃 **Squat Dodge**: Lower body coordination
- 🎮 **Custom Exercises**: Extensible framework

### Advanced Features
- 📊 **Real-time Analytics**: Live performance tracking
- 🎨 **Theme System**: Customizable UI themes
- ♿ **Accessibility**: High contrast, large print, voice over
- 🔄 **Adaptive Difficulty**: Performance-based progression
- 💾 **Backup System**: Automatic data backups
- 📱 **Cross-platform**: PC VR and standalone support

---

## 🏗️ Architecture Overview

```
VR Rehab Framework
├── Core Systems
│   ├── VR Environment (XR_Template)
│   ├── UI System (Canvas + UIManager)
│   ├── Data Persistence (JSON files)
│   └── Analytics (Performance tracking)
├── Exercise Modules
│   ├── Throwing (BallSpawner + TargetRing)
│   ├── Bridge (SOLIDBridgeBuilder)
│   └── Squat (Custom implementation)
├── Management
│   ├── ProgressionSystem (Level advancement)
│   ├── SceneLoader (Navigation)
│   └── PrefabInstantiator (Auto-setup)
└── Utilities
    ├── SceneSetup scripts
    ├── Configuration assets
    └── Helper components
```

---

## 🚀 Getting Started

### Step 1: Scene Creation
1. Open Unity and create a new 3D project
2. Import the VR Rehab Framework assets
3. Create scenes for each exercise type

### Step 2: Basic Setup
1. Add `CompleteVRSetup` component to each scene
2. Assign required prefabs
3. Set appropriate scene type
4. Test in Play mode

### Step 3: Customization
1. Modify exercise parameters
2. Create custom UI themes
3. Add new exercise types
4. Configure analytics settings

### Step 4: Build and Deploy
1. Add scenes to Build Settings
2. Set MainMenu as first scene
3. Build for Oculus Quest/Quest 2
4. Test on device

---

## 🐛 Troubleshooting

### Common Issues

**VR Not Initializing:**
- Check XR Template prefab assignment
- Verify Oculus Integration is installed
- Ensure correct build settings

**UI Not Appearing:**
- Check Canvas prefab assignment
- Verify World Space render mode
- Check canvas position/rotation

**Exercises Not Working:**
- Verify exercise prefabs are assigned
- Check component dependencies
- Review console for error messages

**Data Not Saving:**
- Check persistent data path permissions
- Verify JSON serialization
- Check file system access

### Debug Tools

Use the `SceneSetupInstructions` component to verify setup:
```csharp
[ContextMenu("Verify Scene Setup")]
public void VerifySceneSetup()
```

---

## 📚 Advanced Usage

### Custom Exercise Creation
1. Create new exercise script inheriting from base classes
2. Implement required interfaces (IBridgeComponent, etc.)
3. Add to SceneType enum
4. Configure in CompleteVRSetup

### Analytics Integration
1. Use `PerformanceAnalytics.RecordDataPoint()` for custom metrics
2. Implement `OnMetricUpdated` events
3. Create custom trend analysis

### UI Customization
1. Create new `UITheme` assets
2. Implement custom notification types
3. Add accessibility features

---

## 📞 Support

For issues or questions:
1. Check the console for error messages
2. Verify all prefabs are assigned
3. Review the setup instructions above
4. Use the debug tools provided

---

## 🎉 Success Checklist

- [ ] VR headset recognized
- [ ] Controllers working
- [ ] UI visible and interactive
- [ ] Exercises load correctly
- [ ] Data saves between sessions
- [ ] Analytics tracking performance
- [ ] Scene transitions smooth
- [ ] Patient profiles manageable

**Congratulations! Your VR Rehab Framework is now ready for clinical use!** 🚀
