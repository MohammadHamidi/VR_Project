// Assets/Editor/ThrowingLevelDataEditor.cs
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor.SceneManagement;
using System.Linq;

[CustomEditor(typeof(ThrowingLevelData))]
public class ThrowingLevelDataEditor : Editor
{
    // Constants for 2D Inspector visualization
    private const float WorldViewRange = 20f;
    private const float WorldHeightRange = 12f;
    private const float GridStep = 2.5f;

    // Visual constants for drawing
    private const float BallDotSize = 18f;
    private const float RespawnDotSize = 16f;
    private const float RingDotSize = 14f;
    private const float AxisLabelOffset = 0.75f;
    private const float RingLabelOffset = 0.5f;

    // Human throwing range constants (in meters)
    private static readonly ThrowingRange[] ThrowingRanges = new ThrowingRange[]
    {
        new ThrowingRange("Beginner (Sitting)", 1.2f, 2.5f, new Color(0.2f, 0.8f, 0.2f, 0.15f), new Color(0.2f, 0.8f, 0.2f, 0.8f)),
        new ThrowingRange("Easy (Sitting)", 1.2f, 4.0f, new Color(0.5f, 0.8f, 0.2f, 0.12f), new Color(0.5f, 0.8f, 0.2f, 0.7f)),
        new ThrowingRange("Medium (Standing)", 1.7f, 6.5f, new Color(0.8f, 0.8f, 0.2f, 0.1f), new Color(0.8f, 0.8f, 0.2f, 0.6f)),
        new ThrowingRange("Hard (Standing)", 1.7f, 9.0f, new Color(0.8f, 0.5f, 0.2f, 0.08f), new Color(0.8f, 0.5f, 0.2f, 0.5f)),
        new ThrowingRange("Expert (Standing)", 1.7f, 12.5f, new Color(0.8f, 0.2f, 0.2f, 0.06f), new Color(0.8f, 0.2f, 0.2f, 0.4f))
    };

    private struct ThrowingRange
    {
        public string name;
        public float humanHeight;
        public float maxRange;
        public Color fillColor;
        public Color outlineColor;

        public ThrowingRange(string name, float humanHeight, float maxRange, Color fillColor, Color outlineColor)
        {
            this.name = name;
            this.humanHeight = humanHeight;
            this.maxRange = maxRange;
            this.fillColor = fillColor;
            this.outlineColor = outlineColor;
        }
    }

    // Delegate for drawing content
    delegate void DrawAreaContent(Rect areaRect);

    // GUI Styles - Cached for performance
    private GUIStyle miniLabelStyle;
    private GUIStyle ringLabelStyle;
    private GUIStyle difficultyLabelStyle;
    private GUIStyle headerStyle;
    private GUIStyle infoBoxStyle;
    private bool stylesInitialized = false;

    // Foldout states
    private bool showThrowingRanges = true;
    private bool showValidation = true;
    private bool showGeneration = true;

    // Interactive visualization state
    private float zoomLevel = 1.0f;
    private Vector2 panOffset = Vector2.zero;
    private bool isDragging = false;
    private bool isPanning = false;
    private int draggedElement = -1; // -1: none, -2: ball spawn, -3: respawn zone, 0+: ring index
    private Vector2 dragStartPos;
    private Vector3 originalElementPos;
    private bool isHovering = false;
    private int hoveredElement = -1;
    private const float ElementInteractionRadius = 25f;
    
    // Visualization size controls
    private float visualizationHeight = 400f; // Increased default size
    private bool showVisualizationControls = true;

    void OnEnable()
    {
        InitializeStyles();
    }

    void InitializeStyles()
    {
        if (stylesInitialized && miniLabelStyle?.normal.textColor != null)
            return;

        miniLabelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        ringLabelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.LowerLeft,
            normal = { textColor = Color.green },
            fontSize = 9
        };

        difficultyLabelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            fontSize = 8,
            fontStyle = FontStyle.Bold
        };

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            normal = { textColor = new Color(0.8f, 0.9f, 1f) }
        };

        infoBoxStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 8),
            margin = new RectOffset(4, 4, 4, 4)
        };

        stylesInitialized = true;
    }

    public override void OnInspectorGUI()
    {
        if (!stylesInitialized) InitializeStyles();

        ThrowingLevelData levelData = target as ThrowingLevelData;
        if (levelData == null)
        {
            base.OnInspectorGUI();
            return;
        }

        EditorGUI.BeginChangeCheck();

        // Custom header
        DrawCustomHeader();

        // Draw default inspector
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        // === THROWING DIFFICULTY ANALYSIS ===
        showThrowingRanges = EditorGUILayout.Foldout(showThrowingRanges, "🎯 Throwing Difficulty Analysis", true, headerStyle);
        if (showThrowingRanges)
        {
            DrawThrowingDifficultyAnalysis(levelData);
        }

        EditorGUILayout.Space(10);

        // === VALIDATION SECTION ===
        showValidation = EditorGUILayout.Foldout(showValidation, "✓ Level Validation", true, headerStyle);
        if (showValidation)
        {
            DrawValidationSection(levelData);
        }

        EditorGUILayout.Space(10);

        // === 2D VISUALIZATIONS ===
        EditorGUILayout.LabelField("📊 Level Layout Visualization", headerStyle);
        
        // Visualization size controls
        showVisualizationControls = EditorGUILayout.Foldout(showVisualizationControls, "🎛️ Visualization Controls", true);
        if (showVisualizationControls)
        {
            EditorGUILayout.BeginVertical(infoBoxStyle);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Visualization Size:", GUILayout.Width(120));
            float newHeight = EditorGUILayout.Slider(visualizationHeight, 200f, 800f);
            if (newHeight != visualizationHeight)
            {
                visualizationHeight = newHeight;
                EditorUtility.SetDirty(target);
            }
            EditorGUILayout.LabelField($"{visualizationHeight:F0}px", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Small (250px)", EditorStyles.miniButtonLeft))
            {
                visualizationHeight = 250f;
                EditorUtility.SetDirty(target);
            }
            if (GUILayout.Button("Medium (400px)", EditorStyles.miniButtonMid))
            {
                visualizationHeight = 400f;
                EditorUtility.SetDirty(target);
            }
            if (GUILayout.Button("Large (600px)", EditorStyles.miniButtonMid))
            {
                visualizationHeight = 600f;
                EditorUtility.SetDirty(target);
            }
            if (GUILayout.Button("X-Large (800px)", EditorStyles.miniButtonRight))
            {
                visualizationHeight = 800f;
                EditorUtility.SetDirty(target);
            }
            EditorGUILayout.EndHorizontal();
            
            // Add keyboard shortcuts info
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("⌨️ Keyboard Shortcuts:", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("• Shift +/- : Resize visualization", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("• Ctrl+F : Fit to content", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("• Ctrl+M : Maximize size", EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space(5);

        // Top-Down View (X-Z) with throwing ranges
        EditorGUILayout.LabelField("Top-Down View (X-Z Plane) • Throwing Ranges Overlay", EditorStyles.boldLabel);
        DrawVisualizationArea(rect => {
            DrawThrowingRangesXZ(rect, levelData);
            DrawGridAndAxesXZ(rect);
            DrawElementsXZ(rect, levelData);
            DrawDifficultyLabelsXZ(rect, levelData);
        }, visualizationHeight);

        EditorGUILayout.Space(10);

        // Side View (X-Y) with height indicators
        EditorGUILayout.LabelField("Side View (X-Y Plane) • Human Height References", EditorStyles.boldLabel);
        DrawVisualizationArea(rect => {
            DrawGridAndAxesXY(rect);
            DrawHumanHeightIndicators(rect);
            DrawElementsXY(rect, levelData);
        }, visualizationHeight);

        EditorGUILayout.Space(10);

        // === ENVIRONMENT GENERATION SECTION ===
        showGeneration = EditorGUILayout.Foldout(showGeneration, "🌍 Environment Generation", true, headerStyle);
        if (showGeneration)
        {
            DrawEnvironmentGenerationSection(levelData);
        }

        EditorGUILayout.Space(10);

        // Action buttons with improved styling
        DrawActionButtons(levelData);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(target);
        }
    }

    void OnSceneGUI()
    {
        ThrowingLevelData levelData = target as ThrowingLevelData;
        if (levelData == null) return;

        if (!stylesInitialized) InitializeStyles();

        // Draw core level elements
        DrawSceneViewElements(levelData);

        // Draw throwing range indicators in Scene view
        DrawSceneViewThrowingRanges(levelData);

        // Draw environment generation bounds if enabled
        if (levelData.autoGenerateEnvironment)
        {
            DrawEnvironmentGenerationBounds(levelData);
        }

        if (Event.current.type == EventType.Repaint)
        {
            SceneView.RepaintAll();
        }
    }

    // === PRIVATE METHODS ===

    private void DrawCustomHeader()
    {
        EditorGUILayout.Space(5);
        Rect headerRect = GUILayoutUtility.GetRect(new GUIContent(""), GUIStyle.none, GUILayout.Height(40));
        
        // Background
        EditorGUI.DrawRect(headerRect, new Color(0.15f, 0.25f, 0.35f, 0.8f));
        
        // Title
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            normal = { textColor = Color.white },
            alignment = TextAnchor.MiddleCenter
        };
        
        GUI.Label(headerRect, "🎯 Throwing Level Designer", titleStyle);
        EditorGUILayout.Space(5);
    }

    private void DrawThrowingDifficultyAnalysis(ThrowingLevelData levelData)
    {
        EditorGUILayout.BeginVertical(infoBoxStyle);
        
        // Calculate distances and difficulties for each ring
        Vector3 ballSpawn = levelData.ballSpawnPosition;
        
        if (levelData.ringPositions != null && levelData.ringPositions.Count > 0)
        {
            EditorGUILayout.LabelField("🎯 Ring Analysis (Based on Real Human Capabilities)", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);
            
            for (int i = 0; i < levelData.ringPositions.Count; i++)
            {
                Vector3 ringPos = levelData.ringPositions[i];
                float distance = Vector3.Distance(ballSpawn, ringPos);
                float heightDiff = ringPos.y - ballSpawn.y;
                
                string difficulty = GetThrowingDifficulty(distance);
                Color difficultyColor = GetDifficultyColor(difficulty);
                
                EditorGUILayout.BeginHorizontal();
                
                // Ring info
                EditorGUILayout.LabelField($"Ring {i + 1}:", GUILayout.Width(50));
                EditorGUILayout.LabelField($"{distance:F1}m", GUILayout.Width(45));
                
                // Difficulty indicator
                GUIStyle diffStyle = new GUIStyle(EditorStyles.miniLabel);
                diffStyle.normal.textColor = difficultyColor;
                diffStyle.fontStyle = FontStyle.Bold;
                EditorGUILayout.LabelField(difficulty, diffStyle, GUILayout.Width(80));
                
                // Height info
                string heightInfo = heightDiff > 0 ? $"↑{heightDiff:F1}m" : heightDiff < 0 ? $"↓{Mathf.Abs(heightDiff):F1}m" : "Level";
                EditorGUILayout.LabelField(heightInfo, GUILayout.Width(50));
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(5);
            
            // Overall level assessment
            float avgDistance = levelData.ringPositions.Average(ring => Vector3.Distance(ballSpawn, ring));
            string overallDifficulty = GetThrowingDifficulty(avgDistance);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Overall Level:", EditorStyles.boldLabel);
            GUIStyle overallStyle = new GUIStyle(EditorStyles.boldLabel);
            overallStyle.normal.textColor = GetDifficultyColor(overallDifficulty);
            EditorGUILayout.LabelField($"{overallDifficulty} (Avg: {avgDistance:F1}m)", overallStyle);
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("Add ring positions to see difficulty analysis", MessageType.Info);
        }
        
        EditorGUILayout.Space(3);
        
        // Legend
        EditorGUILayout.LabelField("Difficulty Reference:", EditorStyles.miniLabel);
        foreach (var range in ThrowingRanges)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Color indicator
            Rect colorRect = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12), GUILayout.Height(12));
            EditorGUI.DrawRect(colorRect, range.outlineColor);
            
            EditorGUILayout.LabelField($"{range.name}: ≤{range.maxRange:F1}m", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
    }

    private string GetThrowingDifficulty(float distance)
    {
        if (distance <= 2.5f) return "Beginner";
        if (distance <= 4.0f) return "Easy";
        if (distance <= 6.5f) return "Medium";
        if (distance <= 9.0f) return "Hard";
        return "Expert";
    }

    private Color GetDifficultyColor(string difficulty)
    {
        switch (difficulty)
        {
            case "Beginner": return new Color(0.2f, 0.8f, 0.2f);
            case "Easy": return new Color(0.5f, 0.8f, 0.2f);
            case "Medium": return new Color(0.8f, 0.8f, 0.2f);
            case "Hard": return new Color(0.8f, 0.5f, 0.2f);
            case "Expert": return new Color(0.8f, 0.2f, 0.2f);
            default: return Color.white;
        }
    }

    private void DrawValidationSection(ThrowingLevelData levelData)
    {
        EditorGUILayout.BeginVertical(infoBoxStyle);
        
        List<string> issues = levelData.ValidateLevelData();
        
        if (issues.Count > 0)
        {
            EditorGUILayout.LabelField("⚠️ Issues Found:", EditorStyles.boldLabel);
            foreach (string issue in issues)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("•", GUILayout.Width(15));
                EditorGUILayout.LabelField(issue, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("✅", GUILayout.Width(20));
            EditorGUILayout.LabelField("All validation checks passed", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
    }

    private void DrawEnvironmentGenerationSection(ThrowingLevelData levelData)
    {
        EditorGUILayout.BeginVertical(infoBoxStyle);
        
        if (!levelData.autoGenerateEnvironment)
        {
            EditorGUILayout.HelpBox(
                "🌍 Environment generation is disabled. Enable 'Auto Generate Environment' to create procedural surroundings.",
                MessageType.Info
            );
            EditorGUILayout.EndVertical();
            return;
        }

        // Calculate and display bounds information
        Bounds cubicalBounds = levelData.GetCubicalBoundary();
        float cubicalSize = cubicalBounds.size.x;
        float outerSize = cubicalSize + (levelData.generationAreaOffsetXZ * 2f);
        
        // Count gameplay elements included in boundary
        int elementCount = 1; // Ball spawn
        elementCount += levelData.ringPositions?.Count ?? 0; // Rings
        elementCount += 1; // Respawn zone

        EditorGUILayout.LabelField("🌍 Generation Configuration", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);
        
        // Info grid
        EditorGUILayout.BeginVertical();
        
        DrawInfoRow("Boundary Coverage:", $"Ball Spawn + {levelData.ringPositions?.Count ?? 0} Rings + Respawn Zone ({elementCount} points)");
        DrawInfoRow("Level Boundary (EXCLUSION):", $"{cubicalSize:F1}×{cubicalSize:F1}×{cubicalSize:F1} units");
        DrawInfoRow("Ground Plane:", $"{cubicalSize * 2f:F1}×{cubicalSize * 2f:F1} units (2× cubical size)");
        DrawInfoRow("Outer Generation Area:", $"{outerSize:F1}×{outerSize:F1}×{outerSize:F1} units");
        DrawInfoRow("Generation Ring Width:", $"{levelData.generationAreaOffsetXZ:F1} units around boundary");
        DrawInfoRow("Individual Exclusion Radius:", $"{levelData.generationExclusionRadius:F1} units around key points");
        DrawInfoRow("Generation Density:", $"{levelData.generationDensity:P0}");
        
        EditorGUILayout.EndVertical();

        if (levelData.generationAreaOffsetXZ <= 1f)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "⚠️ Generation Area Offset is very small. Consider increasing it for better environment coverage.",
                MessageType.Warning
            );
        }

        EditorGUILayout.Space(8);

        // Generation button with better styling
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            fixedHeight = 35
        };
        
        if (GUILayout.Button("🌍 Generate Scene Environment", buttonStyle))
        {
            EnvironmentGeneratorEditorUtil.GenerateEnvironment(levelData);
        }

        EditorGUILayout.Space(5);
        
        EditorGUILayout.HelpBox(
            "💡 Generation creates a cubical boundary encompassing ALL gameplay elements. " +
            "This boundary becomes an EXCLUSION zone where NO environment objects are placed. " +
            "Environment objects are generated in an outer ring AROUND this boundary. " +
            "Individual exclusion zones provide additional protection around key gameplay elements.",
            MessageType.Info
        );
        
        EditorGUILayout.EndVertical();
    }

    private void DrawInfoRow(string label, string value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(200));
        EditorGUILayout.LabelField(value, EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawVisualizationArea(DrawAreaContent drawContent, float height)
    {
        Rect visualizationRect = GUILayoutUtility.GetRect(new GUIContent(""), GUIStyle.none, 
            GUILayout.Height(height), GUILayout.ExpandWidth(true));

        // Use more of the available space while maintaining reasonable proportions
        float maxSize = Mathf.Max(visualizationRect.width * 0.95f, height); // Use 95% of width or height, whichever is larger
        float size = Mathf.Min(maxSize, Mathf.Max(visualizationRect.width, height));
        
        // Center the visualization area
        visualizationRect.x += (visualizationRect.width - size) * 0.5f;
        visualizationRect.width = size;
        visualizationRect.height = size;

        // Professional background with subtle gradient
        Color bgColor1 = new Color(0.12f, 0.12f, 0.15f);
        Color bgColor2 = new Color(0.18f, 0.18f, 0.22f);
        EditorGUI.DrawRect(visualizationRect, bgColor1);
        
        // Add border with enhanced styling
        Rect borderRect = new Rect(visualizationRect.x - 2, visualizationRect.y - 2, 
                                   visualizationRect.width + 4, visualizationRect.height + 4);
        EditorGUI.DrawRect(borderRect, new Color(0.3f, 0.4f, 0.5f, 0.6f));
        
        // Inner border for depth
        Rect innerBorderRect = new Rect(visualizationRect.x - 1, visualizationRect.y - 1, 
                                       visualizationRect.width + 2, visualizationRect.height + 2);
        EditorGUI.DrawRect(innerBorderRect, new Color(0.5f, 0.6f, 0.7f, 0.3f));

        // Handle mouse input for zoom and pan
        HandleVisualizationInput(visualizationRect);

        Handles.BeginGUI();
        GUI.BeginClip(visualizationRect);
        
        try
        {
            Rect clippedRect = new Rect(Vector2.zero, visualizationRect.size);
            drawContent.Invoke(clippedRect);
            
            // Draw interaction UI overlays
            DrawInteractionOverlays(clippedRect);
        }
        finally
        {
            GUI.EndClip();
        }
        
        Handles.EndGUI();

        // Draw zoom/pan controls
        DrawVisualizationControls(visualizationRect);
    }

    private void HandleVisualizationInput(Rect visualizationRect)
    {
        Event e = Event.current;
        Vector2 mousePos = e.mousePosition;
        
        // Check if mouse is over visualization area
        bool mouseInRect = visualizationRect.Contains(mousePos);
        if (!mouseInRect) return;

        // Convert to local coordinates
        Vector2 localMousePos = mousePos - visualizationRect.position;

        switch (e.type)
        {
            case EventType.ScrollWheel:
                HandleZoom(e, localMousePos, visualizationRect);
                break;
                
            case EventType.MouseDown:
                HandleMouseDown(e, localMousePos, visualizationRect);
                break;
                
            case EventType.MouseDrag:
                HandleMouseDrag(e, localMousePos, visualizationRect);
                break;
                
            case EventType.MouseUp:
                HandleMouseUp(e);
                break;
                
            case EventType.MouseMove:
                HandleMouseMove(e, localMousePos, visualizationRect);
                break;
        }
    }

    private void HandleZoom(Event e, Vector2 localMousePos, Rect visualizationRect)
    {
        float zoomSensitivity = 0.1f;
        float oldZoom = zoomLevel;
        
        // Zoom toward mouse position
        zoomLevel = Mathf.Clamp(zoomLevel - e.delta.y * zoomSensitivity, 0.1f, 5.0f);
        
        // Adjust pan to zoom toward mouse
        Vector2 center = new Vector2(visualizationRect.width / 2, visualizationRect.height / 2);
        Vector2 mouseOffset = localMousePos - center;
        float zoomChange = zoomLevel / oldZoom - 1.0f;
        panOffset -= mouseOffset * zoomChange;
        
        e.Use();
        EditorUtility.SetDirty(target);
    }

    private void HandleMouseDown(Event e, Vector2 localMousePos, Rect visualizationRect)
    {
        if (e.button == 0) // Left mouse button
        {
            draggedElement = GetElementAtPosition(localMousePos, visualizationRect);
            
            if (draggedElement != -1)
            {
                isDragging = true;
                dragStartPos = localMousePos;
                originalElementPos = GetElementWorldPosition(draggedElement);
                e.Use();
            }
        }
        else if (e.button == 2) // Middle mouse button for panning
        {
            isPanning = true;
            dragStartPos = localMousePos;
            e.Use();
        }
    }

    private void HandleMouseDrag(Event e, Vector2 localMousePos, Rect visualizationRect)
    {
        if (isPanning && e.button == 2)
        {
            Vector2 delta = localMousePos - dragStartPos;
            panOffset += delta;
            dragStartPos = localMousePos;
            e.Use();
            EditorUtility.SetDirty(target);
        }
        else if (isDragging && e.button == 0 && draggedElement != -1)
        {
            HandleElementDrag(localMousePos, visualizationRect);
            e.Use();
            EditorUtility.SetDirty(target);
        }
    }

    private void HandleMouseUp(Event e)
    {
        if (e.button == 0)
        {
            isDragging = false;
            draggedElement = -1;
        }
        else if (e.button == 2)
        {
            isPanning = false;
        }
        e.Use();
    }

    private void HandleMouseMove(Event e, Vector2 localMousePos, Rect visualizationRect)
    {
        int newHoveredElement = GetElementAtPosition(localMousePos, visualizationRect);
        if (newHoveredElement != hoveredElement)
        {
            hoveredElement = newHoveredElement;
            isHovering = hoveredElement != -1;
            SceneView.RepaintAll();
        }
    }

    private Vector2 WorldXZToGuiPos(Vector3 worldPos, Rect guiRect)
    {
        float scale = (guiRect.width / (WorldViewRange * 2)) * zoomLevel;
        Vector2 center = new Vector2(guiRect.width / 2, guiRect.height / 2) + panOffset;
        
        return new Vector2(
            center.x + worldPos.x * scale,
            center.y - worldPos.z * scale
        );
    }

    private Vector2 WorldXYToGuiPos(Vector3 worldPos, Rect guiRect)
    {
        float scaleX = (guiRect.width / (WorldViewRange * 2)) * zoomLevel;
        float scaleY = (guiRect.height / (WorldHeightRange * 2)) * zoomLevel;
        Vector2 center = new Vector2(guiRect.width / 2, guiRect.height / 2) + panOffset;
        
        return new Vector2(
            center.x + worldPos.x * scaleX,
            center.y - worldPos.y * scaleY
        );
    }

    private Vector3 GuiPosToWorldXZ(Vector2 guiPos, Rect guiRect)
    {
        float scale = (guiRect.width / (WorldViewRange * 2)) * zoomLevel;
        Vector2 center = new Vector2(guiRect.width / 2, guiRect.height / 2) + panOffset;
        
        return new Vector3(
            (guiPos.x - center.x) / scale,
            0f,
            -(guiPos.y - center.y) / scale
        );
    }

    private Vector3 GuiPosToWorldXY(Vector2 guiPos, Rect guiRect)
    {
        float scaleX = (guiRect.width / (WorldViewRange * 2)) * zoomLevel;
        float scaleY = (guiRect.height / (WorldHeightRange * 2)) * zoomLevel;
        Vector2 center = new Vector2(guiRect.width / 2, guiRect.height / 2) + panOffset;
        
        return new Vector3(
            (guiPos.x - center.x) / scaleX,
            -(guiPos.y - center.y) / scaleY,
            0f
        );
    }

    private void DrawGridAndAxesXZ(Rect guiRect)
    {
        if (miniLabelStyle == null) InitializeStyles();

        // Enhanced grid
        Handles.color = new Color(0.3f, 0.35f, 0.4f, 0.3f);
        for (float x = -WorldViewRange; x <= WorldViewRange; x += GridStep)
        {
            Handles.DrawLine(
                WorldXZToGuiPos(new Vector3(x, 0, -WorldViewRange), guiRect),
                WorldXZToGuiPos(new Vector3(x, 0, WorldViewRange), guiRect)
            );
        }
        for (float z = -WorldViewRange; z <= WorldViewRange; z += GridStep)
        {
            Handles.DrawLine(
                WorldXZToGuiPos(new Vector3(-WorldViewRange, 0, z), guiRect),
                WorldXZToGuiPos(new Vector3(WorldViewRange, 0, z), guiRect)
            );
        }

        // Main axes with better visibility
        Handles.color = new Color(0.6f, 0.7f, 0.8f, 0.8f);
        Handles.DrawLine(
            WorldXZToGuiPos(new Vector3(-WorldViewRange, 0, 0), guiRect),
            WorldXZToGuiPos(new Vector3(WorldViewRange, 0, 0), guiRect)
        );
        Handles.DrawLine(
            WorldXZToGuiPos(new Vector3(0, 0, -WorldViewRange), guiRect),
            WorldXZToGuiPos(new Vector3(0, 0, WorldViewRange), guiRect)
        );

        // Enhanced origin
        Handles.color = new Color(1f, 1f, 1f, 0.9f);
        Handles.DrawSolidDisc(WorldXZToGuiPos(Vector3.zero, guiRect), Vector3.forward, 4f);
        Handles.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        Handles.DrawWireDisc(WorldXZToGuiPos(Vector3.zero, guiRect), Vector3.forward, 4f);

        // Professional axis labels
        if (miniLabelStyle != null)
        {
            GUIStyle axisStyle = new GUIStyle(miniLabelStyle);
            axisStyle.normal.textColor = new Color(0.8f, 0.9f, 1f);
            axisStyle.fontStyle = FontStyle.Bold;
            
            Handles.Label(WorldXZToGuiPos(new Vector3(WorldViewRange - AxisLabelOffset, 0, 0), guiRect), "X", axisStyle);
            Handles.Label(WorldXZToGuiPos(new Vector3(0, 0, WorldViewRange - AxisLabelOffset), guiRect), "Z", axisStyle);
        }
    }

    private void DrawGridAndAxesXY(Rect guiRect)
    {
        if (miniLabelStyle == null) InitializeStyles();

        // Enhanced grid
        Handles.color = new Color(0.3f, 0.35f, 0.4f, 0.3f);
        for (float x = -WorldViewRange; x <= WorldViewRange; x += GridStep)
        {
            Handles.DrawLine(
                WorldXYToGuiPos(new Vector3(x, -WorldHeightRange, 0), guiRect),
                WorldXYToGuiPos(new Vector3(x, WorldHeightRange, 0), guiRect)
            );
        }
        for (float y = -WorldHeightRange; y <= WorldHeightRange; y += GridStep)
        {
            Handles.DrawLine(
                WorldXYToGuiPos(new Vector3(-WorldViewRange, y, 0), guiRect),
                WorldXYToGuiPos(new Vector3(WorldViewRange, y, 0), guiRect)
            );
        }

        // Main axes
        Handles.color = new Color(0.6f, 0.7f, 0.8f, 0.8f);
        Handles.DrawLine(
            WorldXYToGuiPos(new Vector3(-WorldViewRange, 0, 0), guiRect),
            WorldXYToGuiPos(new Vector3(WorldViewRange, 0, 0), guiRect)
        );
        Handles.DrawLine(
            WorldXYToGuiPos(new Vector3(0, -WorldHeightRange, 0), guiRect),
            WorldXYToGuiPos(new Vector3(0, WorldHeightRange, 0), guiRect)
        );

        // Enhanced origin
        Handles.color = new Color(1f, 1f, 1f, 0.9f);
        Handles.DrawSolidDisc(WorldXYToGuiPos(Vector3.zero, guiRect), Vector3.forward, 4f);
        Handles.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        Handles.DrawWireDisc(WorldXYToGuiPos(Vector3.zero, guiRect), Vector3.forward, 4f);

        // Professional axis labels
        if (miniLabelStyle != null)
        {
            GUIStyle axisStyle = new GUIStyle(miniLabelStyle);
            axisStyle.normal.textColor = new Color(0.8f, 0.9f, 1f);
            axisStyle.fontStyle = FontStyle.Bold;
            
            Handles.Label(WorldXYToGuiPos(new Vector3(WorldViewRange - AxisLabelOffset, 0, 0), guiRect), "X", axisStyle);
            Handles.Label(WorldXYToGuiPos(new Vector3(0, WorldHeightRange - AxisLabelOffset, 0), guiRect), "Y", axisStyle);
        }
    }

    private void DrawThrowingRangesXZ(Rect guiRect, ThrowingLevelData levelData)
    {
        Vector3 ballSpawn = levelData.ballSpawnPosition;
        Vector2 ballSpawnGui = WorldXZToGuiPos(ballSpawn, guiRect);
        
        // Draw throwing range circles from ball spawn position
        foreach (var range in ThrowingRanges)
        {
            float rangeScale = guiRect.width / (WorldViewRange * 2);
            float rangeRadius = range.maxRange * rangeScale;
            
            if (rangeRadius > 5f) // Only draw if visible
            {
                // Fill circle
                Handles.color = range.fillColor;
                Handles.DrawSolidDisc(ballSpawnGui, Vector3.forward, rangeRadius);
                
                // Outline circle
                Handles.color = range.outlineColor;
                Handles.DrawWireDisc(ballSpawnGui, Vector3.forward, rangeRadius);
            }
        }
    }

    private void DrawDifficultyLabelsXZ(Rect guiRect, ThrowingLevelData levelData)
    {
        if (difficultyLabelStyle == null) return;
        
        Vector3 ballSpawn = levelData.ballSpawnPosition;
        Vector2 ballSpawnGui = WorldXZToGuiPos(ballSpawn, guiRect);
        float rangeScale = guiRect.width / (WorldViewRange * 2);
        
        // Draw range labels at the edge of each circle
        for (int i = ThrowingRanges.Length - 1; i >= 0; i--) // Draw from largest to smallest
        {
            var range = ThrowingRanges[i];
            float rangeRadius = range.maxRange * rangeScale;
            
            if (rangeRadius > 20f) // Only draw labels for visible ranges
            {
                Vector2 labelPos = ballSpawnGui + new Vector2(rangeRadius * 0.7f, -rangeRadius * 0.7f);
                
                Handles.color = range.outlineColor;
                string label = $"{range.name}\n{range.maxRange:F1}m";
                Handles.Label(labelPos, label, difficultyLabelStyle);
            }
        }
    }

    private void DrawHumanHeightIndicators(Rect guiRect)
    {
        if (miniLabelStyle == null) return;
        
        // Draw sitting height line (1.2m)
        float sittingHeight = 1.2f;
        Vector2 sittingLineStart = WorldXYToGuiPos(new Vector3(-WorldViewRange, sittingHeight, 0), guiRect);
        Vector2 sittingLineEnd = WorldXYToGuiPos(new Vector3(WorldViewRange, sittingHeight, 0), guiRect);
        
        Handles.color = new Color(0.3f, 0.7f, 1f, 0.6f);
        Handles.DrawDottedLine(sittingLineStart, sittingLineEnd, 3f);
        Handles.Label(sittingLineEnd + Vector2.left * 40, "Sitting (1.2m)", miniLabelStyle);
        
        // Draw standing height line (1.7m)
        float standingHeight = 1.7f;
        Vector2 standingLineStart = WorldXYToGuiPos(new Vector3(-WorldViewRange, standingHeight, 0), guiRect);
        Vector2 standingLineEnd = WorldXYToGuiPos(new Vector3(WorldViewRange, standingHeight, 0), guiRect);
        
        Handles.color = new Color(1f, 0.7f, 0.3f, 0.6f);
        Handles.DrawDottedLine(standingLineStart, standingLineEnd, 3f);
        Handles.Label(standingLineEnd + Vector2.left * 40, "Standing (1.7m)", miniLabelStyle);
    }

    private void DrawElementsXZ(Rect guiRect, ThrowingLevelData levelData)
    {
        if (ringLabelStyle == null) InitializeStyles();

        // Enhanced Ball Spawn with glow effect
        Vector2 ballSpawnGui = WorldXZToGuiPos(levelData.ballSpawnPosition, guiRect);
        
        // Check if ball spawn is selected or hovered
        bool ballSpawnInteractive = (hoveredElement == -2) || (draggedElement == -2);
        float ballGlowMultiplier = ballSpawnInteractive ? 2.5f : 1.8f;
        Color ballGlowColor = ballSpawnInteractive ? new Color(1f, 1f, 0f, 0.3f) : new Color(0.2f, 0.4f, 1f, 0.2f);
        
        // Glow effect
        Handles.color = ballGlowColor;
        Handles.DrawSolidDisc(ballSpawnGui, Vector3.forward, BallDotSize * ballGlowMultiplier);
        
        // Main ball
        Handles.color = new Color(0.3f, 0.6f, 1f, 0.9f);
        Handles.DrawSolidDisc(ballSpawnGui, Vector3.forward, BallDotSize / 2);
        
        // Outline
        Color ballOutlineColor = ballSpawnInteractive ? Color.yellow : new Color(0.8f, 0.9f, 1f, 1f);
        Handles.color = ballOutlineColor;
        Handles.DrawWireDisc(ballSpawnGui, Vector3.forward, BallDotSize / 2);
        if (ballSpawnInteractive)
        {
            Handles.DrawWireDisc(ballSpawnGui, Vector3.forward, BallDotSize / 2 + 3);
        }

        // Enhanced Respawn Zone
        Vector2 respawnGui = WorldXZToGuiPos(levelData.respawnZonePosition, guiRect);
        
        // Check if respawn zone is selected or hovered
        bool respawnInteractive = (hoveredElement == -3) || (draggedElement == -3);
        float respawnGlowMultiplier = respawnInteractive ? 2.0f : 1.5f;
        Color respawnGlowColor = respawnInteractive ? new Color(1f, 1f, 0f, 0.2f) : new Color(1f, 0.2f, 0.2f, 0.15f);
        
        // Danger glow
        Handles.color = respawnGlowColor;
        Handles.DrawSolidDisc(respawnGui, Vector3.forward, RespawnDotSize * respawnGlowMultiplier);
        
        // Main respawn zone
        Handles.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        Handles.DrawSolidDisc(respawnGui, Vector3.forward, RespawnDotSize / 2);
        
        // Outline
        Color respawnOutlineColor = respawnInteractive ? Color.yellow : new Color(1f, 0.6f, 0.6f, 1f);
        Handles.color = respawnOutlineColor;
        Handles.DrawWireDisc(respawnGui, Vector3.forward, RespawnDotSize / 2);
        if (respawnInteractive)
        {
            Handles.DrawWireDisc(respawnGui, Vector3.forward, RespawnDotSize / 2 + 3);
        }

        // Enhanced Rings and connections
        if (levelData.ringPositions != null)
        {
            for (int i = 0; i < levelData.ringPositions.Count; i++)
            {
                Vector2 ringGui = WorldXZToGuiPos(levelData.ringPositions[i], guiRect);
                
                // Ring difficulty color
                float distance = Vector3.Distance(levelData.ballSpawnPosition, levelData.ringPositions[i]);
                Color ringColor = GetDifficultyColor(GetThrowingDifficulty(distance));

                // Check if ring is selected or hovered
                bool ringInteractive = (hoveredElement == i) || (draggedElement == i);
                float ringGlowMultiplier = ringInteractive ? 2.0f : 1.4f;
                Color ringGlowColor = ringInteractive ? new Color(1f, 1f, 0f, 0.3f) : new Color(ringColor.r, ringColor.g, ringColor.b, 0.2f);

                // Ring glow
                Handles.color = ringGlowColor;
                Handles.DrawSolidDisc(ringGui, Vector3.forward, RingDotSize * ringGlowMultiplier);
                
                // Main ring
                Handles.color = new Color(ringColor.r, ringColor.g, ringColor.b, 0.9f);
                Handles.DrawSolidDisc(ringGui, Vector3.forward, RingDotSize / 2);
                
                // Ring outline
                Color ringOutlineColor = ringInteractive ? Color.yellow : Color.white;
                Handles.color = ringOutlineColor;
                Handles.DrawWireDisc(ringGui, Vector3.forward, RingDotSize / 2);
                if (ringInteractive)
                {
                    Handles.DrawWireDisc(ringGui, Vector3.forward, RingDotSize / 2 + 3);
                }
                
                // Enhanced ring label
                if (ringLabelStyle != null)
                {
                    GUIStyle enhancedRingStyle = new GUIStyle(ringLabelStyle);
                    enhancedRingStyle.normal.textColor = ringInteractive ? Color.yellow : Color.white;
                    enhancedRingStyle.fontStyle = FontStyle.Bold;
                    
                    string labelText = $"{i + 1}\n{distance:F1}m";
                    Handles.Label(ringGui + Vector2.one * (RingLabelOffset * RingDotSize), labelText, enhancedRingStyle);
                }

                // Enhanced connection lines
                if (i == 0)
                {
                    Handles.color = new Color(0.4f, 0.8f, 1f, 0.8f);
                    Handles.DrawLine(ballSpawnGui, ringGui);
                    
                    // Arrow indicator
                    Vector2 direction = (ringGui - ballSpawnGui).normalized;
                    Vector2 arrowPos = ballSpawnGui + direction * (BallDotSize + 5);
                    DrawArrow(arrowPos, direction, 8f);
                }
                else
                {
                    Handles.color = new Color(0.8f, 0.8f, 0.4f, 0.7f);
                    Vector2 prevRingGui = WorldXZToGuiPos(levelData.ringPositions[i - 1], guiRect);
                    Handles.DrawLine(prevRingGui, ringGui);
                    
                    // Arrow indicator
                    Vector2 direction = (ringGui - prevRingGui).normalized;
                    Vector2 arrowPos = prevRingGui + direction * (RingDotSize + 5);
                    DrawArrow(arrowPos, direction, 6f);
                }
            }
        }
    }

    private void DrawArrow(Vector2 position, Vector2 direction, float size)
    {
        Vector2 arrowHead1 = position + (Vector2)(Quaternion.Euler(0, 0, 150) * direction) * size;
        Vector2 arrowHead2 = position + (Vector2)(Quaternion.Euler(0, 0, -150) * direction) * size;
        
        Handles.DrawLine(position, arrowHead1);
        Handles.DrawLine(position, arrowHead2);
    }

    // === INTERACTIVE VISUALIZATION METHODS ===

    private int GetElementAtPosition(Vector2 localMousePos, Rect visualizationRect)
    {
        ThrowingLevelData levelData = target as ThrowingLevelData;
        if (levelData == null) return -1;

        // Check rings first (higher priority for selection)
        if (levelData.ringPositions != null)
        {
            for (int i = 0; i < levelData.ringPositions.Count; i++)
            {
                Vector2 elementPos = WorldXZToGuiPos(levelData.ringPositions[i], visualizationRect);
                if (Vector2.Distance(localMousePos, elementPos) <= ElementInteractionRadius)
                {
                    return i; // Ring index
                }
            }
        }

        // Check ball spawn
        Vector2 ballPos = WorldXZToGuiPos(levelData.ballSpawnPosition, visualizationRect);
        if (Vector2.Distance(localMousePos, ballPos) <= ElementInteractionRadius)
        {
            return -2; // Ball spawn
        }

        // Check respawn zone
        Vector2 respawnPos = WorldXZToGuiPos(levelData.respawnZonePosition, visualizationRect);
        if (Vector2.Distance(localMousePos, respawnPos) <= ElementInteractionRadius)
        {
            return -3; // Respawn zone
        }

        return -1; // No element
    }

    private Vector3 GetElementWorldPosition(int elementIndex)
    {
        ThrowingLevelData levelData = target as ThrowingLevelData;
        if (levelData == null) return Vector3.zero;

        switch (elementIndex)
        {
            case -2: return levelData.ballSpawnPosition;
            case -3: return levelData.respawnZonePosition;
            default:
                if (elementIndex >= 0 && levelData.ringPositions != null && elementIndex < levelData.ringPositions.Count)
                    return levelData.ringPositions[elementIndex];
                return Vector3.zero;
        }
    }

    private void SetElementWorldPosition(int elementIndex, Vector3 newPosition)
    {
        ThrowingLevelData levelData = target as ThrowingLevelData;
        if (levelData == null) return;

        Undo.RecordObject(levelData, "Move Level Element");

        switch (elementIndex)
        {
            case -2:
                levelData.ballSpawnPosition = newPosition;
                break;
            case -3:
                levelData.respawnZonePosition = newPosition;
                break;
            default:
                if (elementIndex >= 0 && levelData.ringPositions != null && elementIndex < levelData.ringPositions.Count)
                    levelData.ringPositions[elementIndex] = newPosition;
                break;
        }

        EditorUtility.SetDirty(levelData);
    }

    private void HandleElementDrag(Vector2 localMousePos, Rect visualizationRect)
    {
        ThrowingLevelData levelData = target as ThrowingLevelData;
        if (levelData == null || draggedElement == -1) return;

        // Convert mouse position to world coordinates
        Vector3 newWorldPos = GuiPosToWorldXZ(localMousePos, visualizationRect);
        
        // Preserve original Y coordinate
        Vector3 originalPos = GetElementWorldPosition(draggedElement);
        newWorldPos.y = originalPos.y;

        SetElementWorldPosition(draggedElement, newWorldPos);
    }

    private void DrawInteractionOverlays(Rect visualizationRect)
    {
        ThrowingLevelData levelData = target as ThrowingLevelData;
        if (levelData == null) return;

        // Draw hover highlights
        if (isHovering && hoveredElement != -1)
        {
            Vector2 elementPos = Vector2.zero;
            string elementName = "";

            switch (hoveredElement)
            {
                case -2:
                    elementPos = WorldXZToGuiPos(levelData.ballSpawnPosition, visualizationRect);
                    elementName = "Ball Spawn";
                    break;
                case -3:
                    elementPos = WorldXZToGuiPos(levelData.respawnZonePosition, visualizationRect);
                    elementName = "Respawn Zone";
                    break;
                default:
                    if (hoveredElement >= 0 && levelData.ringPositions != null && hoveredElement < levelData.ringPositions.Count)
                    {
                        elementPos = WorldXZToGuiPos(levelData.ringPositions[hoveredElement], visualizationRect);
                        elementName = $"Ring {hoveredElement + 1}";
                    }
                    break;
            }

            if (elementName != "")
            {
                // Draw hover circle
                Handles.color = new Color(1f, 1f, 0f, 0.5f);
                Handles.DrawWireDisc(elementPos, Vector3.forward, ElementInteractionRadius);

                // Draw tooltip
                GUIStyle tooltipStyle = new GUIStyle(EditorStyles.helpBox);
                tooltipStyle.normal.textColor = Color.white;
                tooltipStyle.fontSize = 10;
                
                Vector2 tooltipPos = elementPos + new Vector2(ElementInteractionRadius + 5, -10);
                Handles.Label(tooltipPos, elementName, tooltipStyle);
            }
        }

        // Draw drag preview
        if (isDragging && draggedElement != -1)
        {
            Vector2 elementPos = Vector2.zero;
            
            switch (draggedElement)
            {
                case -2: elementPos = WorldXZToGuiPos(levelData.ballSpawnPosition, visualizationRect); break;
                case -3: elementPos = WorldXZToGuiPos(levelData.respawnZonePosition, visualizationRect); break;
                default:
                    if (draggedElement >= 0 && levelData.ringPositions != null && draggedElement < levelData.ringPositions.Count)
                        elementPos = WorldXZToGuiPos(levelData.ringPositions[draggedElement], visualizationRect);
                    break;
            }

            // Draw drag indicator
            Handles.color = new Color(0f, 1f, 1f, 0.8f);
            Handles.DrawWireDisc(elementPos, Vector3.forward, ElementInteractionRadius * 1.2f);
            
            // Draw movement lines
            Handles.color = new Color(0f, 1f, 1f, 0.3f);
            Handles.DrawDottedLine(WorldXZToGuiPos(originalElementPos, visualizationRect), elementPos, 3f);
        }
    }

    private void DrawVisualizationControls(Rect visualizationRect)
    {
        // Draw size indicator in top-left
        Rect sizeRect = new Rect(visualizationRect.x + 10, visualizationRect.y + 10, 150, 20);
        EditorGUI.DrawRect(sizeRect, new Color(0f, 0f, 0f, 0.8f));
        
        GUIStyle sizeStyle = new GUIStyle(EditorStyles.miniLabel);
        sizeStyle.normal.textColor = Color.white;
        sizeStyle.fontStyle = FontStyle.Bold;
        GUI.Label(sizeRect, $"📐 Size: {visualizationRect.width:F0}×{visualizationRect.height:F0}px", sizeStyle);

        // Draw zoom level indicator below size
        Rect zoomRect = new Rect(visualizationRect.x + 10, visualizationRect.y + 35, 150, 20);
        EditorGUI.DrawRect(zoomRect, new Color(0f, 0f, 0f, 0.8f));
        
        GUIStyle zoomStyle = new GUIStyle(EditorStyles.miniLabel);
        zoomStyle.normal.textColor = Color.white;
        GUI.Label(zoomRect, $"🔍 Zoom: {zoomLevel:F1}x", zoomStyle);

        // Draw control instructions
        Rect instructionsRect = new Rect(visualizationRect.x + 10, visualizationRect.yMax - 100, 220, 90);
        EditorGUI.DrawRect(instructionsRect, new Color(0f, 0f, 0f, 0.8f));
        
        GUIStyle instructionsStyle = new GUIStyle(EditorStyles.miniLabel);
        instructionsStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        instructionsStyle.wordWrap = true;
        instructionsStyle.fontSize = 10;
        GUI.Label(instructionsRect, "🖱️ Drag elements to move\n🔄 Middle mouse to pan\n⚫ Scroll to zoom\n⌨️ +/- keys to zoom\n⌨️ Shift +/- resize view\n⌨️ Ctrl+R reset • Ctrl+F fit • Ctrl+M max", instructionsStyle);

        // Control buttons in top-right
        float buttonWidth = 70f;
        float buttonHeight = 22f;
        float buttonSpacing = 5f;
        
        // Reset view button
        Rect resetRect = new Rect(visualizationRect.xMax - buttonWidth - 10, visualizationRect.y + 10, buttonWidth, buttonHeight);
        if (GUI.Button(resetRect, "Reset View", EditorStyles.miniButton))
        {
            ResetVisualizationView();
        }
        
        // Fit to content button
        Rect fitRect = new Rect(visualizationRect.xMax - buttonWidth - 10, visualizationRect.y + 10 + buttonHeight + buttonSpacing, buttonWidth, buttonHeight);
        if (GUI.Button(fitRect, "Fit Content", EditorStyles.miniButton))
        {
            FitVisualizationToContent();
        }
        
        // Maximize button
        Rect maxRect = new Rect(visualizationRect.xMax - buttonWidth - 10, visualizationRect.y + 10 + (buttonHeight + buttonSpacing) * 2, buttonWidth, buttonHeight);
        if (GUI.Button(maxRect, "Maximize", EditorStyles.miniButton))
        {
            visualizationHeight = 800f;
            EditorUtility.SetDirty(target);
        }

        // Handle keyboard shortcuts
        HandleKeyboardShortcuts();
    }

    private void HandleKeyboardShortcuts()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            switch (e.keyCode)
            {
                case KeyCode.R:
                    if (e.control || e.command)
                    {
                        ResetVisualizationView();
                        e.Use();
                    }
                    break;
                case KeyCode.F:
                    if (e.control || e.command)
                    {
                        FitVisualizationToContent();
                        e.Use();
                    }
                    break;
                case KeyCode.Plus:
                case KeyCode.KeypadPlus:
                    if (e.shift) // Shift + Plus = increase visualization size
                    {
                        visualizationHeight = Mathf.Clamp(visualizationHeight + 50f, 200f, 800f);
                        EditorUtility.SetDirty(target);
                    }
                    else // Just Plus = zoom
                    {
                        zoomLevel = Mathf.Clamp(zoomLevel + 0.2f, 0.1f, 5.0f);
                        EditorUtility.SetDirty(target);
                    }
                    e.Use();
                    break;
                case KeyCode.Minus:
                case KeyCode.KeypadMinus:
                    if (e.shift) // Shift + Minus = decrease visualization size
                    {
                        visualizationHeight = Mathf.Clamp(visualizationHeight - 50f, 200f, 800f);
                        EditorUtility.SetDirty(target);
                    }
                    else // Just Minus = zoom out
                    {
                        zoomLevel = Mathf.Clamp(zoomLevel - 0.2f, 0.1f, 5.0f);
                        EditorUtility.SetDirty(target);
                    }
                    e.Use();
                    break;
                case KeyCode.M:
                    if (e.control || e.command) // Ctrl/Cmd + M = maximize
                    {
                        visualizationHeight = 800f;
                        EditorUtility.SetDirty(target);
                        e.Use();
                    }
                    break;
            }
        }
    }

    private void ResetVisualizationView()
    {
        zoomLevel = 1.0f;
        panOffset = Vector2.zero;
        EditorUtility.SetDirty(target);
    }
    
    private void FitVisualizationToContent()
    {
        ThrowingLevelData levelData = target as ThrowingLevelData;
        if (levelData == null) return;
        
        // Calculate bounds of all elements
        Bounds contentBounds = new Bounds(levelData.ballSpawnPosition, Vector3.zero);
        contentBounds.Encapsulate(levelData.respawnZonePosition);
        
        if (levelData.ringPositions != null)
        {
            foreach (var ringPos in levelData.ringPositions)
            {
                contentBounds.Encapsulate(ringPos);
            }
        }
        
        // Add some padding
        contentBounds.Expand(2f);
        
        // Calculate optimal zoom to fit content
        float maxDimension = Mathf.Max(contentBounds.size.x, contentBounds.size.z);
        float targetZoom = (WorldViewRange * 1.5f) / maxDimension;
        
        zoomLevel = Mathf.Clamp(targetZoom, 0.1f, 5.0f);
        
        // Center on content
        Vector3 contentCenter = contentBounds.center;
        panOffset = new Vector2(-contentCenter.x * 10f, contentCenter.z * 10f);
        
        EditorUtility.SetDirty(target);
    }

    private void DrawElementsXY(Rect guiRect, ThrowingLevelData levelData)
    {
        if (ringLabelStyle == null) InitializeStyles();

        // Same enhanced styling as XZ view but for XY plane
        Vector2 ballSpawnGui = WorldXYToGuiPos(levelData.ballSpawnPosition, guiRect);
        
        // Check if ball spawn is selected or hovered
        bool ballSpawnInteractive = (hoveredElement == -2) || (draggedElement == -2);
        float ballGlowMultiplier = ballSpawnInteractive ? 2.5f : 1.8f;
        Color ballGlowColor = ballSpawnInteractive ? new Color(1f, 1f, 0f, 0.3f) : new Color(0.2f, 0.4f, 1f, 0.2f);
        
        // Enhanced Ball Spawn
        Handles.color = ballGlowColor;
        Handles.DrawSolidDisc(ballSpawnGui, Vector3.forward, BallDotSize * ballGlowMultiplier);
        Handles.color = new Color(0.3f, 0.6f, 1f, 0.9f);
        Handles.DrawSolidDisc(ballSpawnGui, Vector3.forward, BallDotSize / 2);
        
        Color ballOutlineColor = ballSpawnInteractive ? Color.yellow : new Color(0.8f, 0.9f, 1f, 1f);
        Handles.color = ballOutlineColor;
        Handles.DrawWireDisc(ballSpawnGui, Vector3.forward, BallDotSize / 2);
        if (ballSpawnInteractive)
        {
            Handles.DrawWireDisc(ballSpawnGui, Vector3.forward, BallDotSize / 2 + 3);
        }

        // Enhanced Respawn Zone
        Vector2 respawnGui = WorldXYToGuiPos(levelData.respawnZonePosition, guiRect);
        
        bool respawnInteractive = (hoveredElement == -3) || (draggedElement == -3);
        float respawnGlowMultiplier = respawnInteractive ? 2.0f : 1.5f;
        Color respawnGlowColor = respawnInteractive ? new Color(1f, 1f, 0f, 0.2f) : new Color(1f, 0.2f, 0.2f, 0.15f);
        
        Handles.color = respawnGlowColor;
        Handles.DrawSolidDisc(respawnGui, Vector3.forward, RespawnDotSize * respawnGlowMultiplier);
        Handles.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        Handles.DrawSolidDisc(respawnGui, Vector3.forward, RespawnDotSize / 2);
        
        Color respawnOutlineColor = respawnInteractive ? Color.yellow : new Color(1f, 0.6f, 0.6f, 1f);
        Handles.color = respawnOutlineColor;
        Handles.DrawWireDisc(respawnGui, Vector3.forward, RespawnDotSize / 2);
        if (respawnInteractive)
        {
            Handles.DrawWireDisc(respawnGui, Vector3.forward, RespawnDotSize / 2 + 3);
        }

        // Enhanced Rings
        if (levelData.ringPositions != null)
        {
            for (int i = 0; i < levelData.ringPositions.Count; i++)
            {
                Vector2 ringGui = WorldXYToGuiPos(levelData.ringPositions[i], guiRect);
                
                float distance = Vector3.Distance(levelData.ballSpawnPosition, levelData.ringPositions[i]);
                Color ringColor = GetDifficultyColor(GetThrowingDifficulty(distance));

                bool ringInteractive = (hoveredElement == i) || (draggedElement == i);
                float ringGlowMultiplier = ringInteractive ? 2.0f : 1.4f;
                Color ringGlowColor = ringInteractive ? new Color(1f, 1f, 0f, 0.3f) : new Color(ringColor.r, ringColor.g, ringColor.b, 0.2f);

                Handles.color = ringGlowColor;
                Handles.DrawSolidDisc(ringGui, Vector3.forward, RingDotSize * ringGlowMultiplier);
                Handles.color = new Color(ringColor.r, ringColor.g, ringColor.b, 0.9f);
                Handles.DrawSolidDisc(ringGui, Vector3.forward, RingDotSize / 2);
                
                Color ringOutlineColor = ringInteractive ? Color.yellow : Color.white;
                Handles.color = ringOutlineColor;
                Handles.DrawWireDisc(ringGui, Vector3.forward, RingDotSize / 2);
                if (ringInteractive)
                {
                    Handles.DrawWireDisc(ringGui, Vector3.forward, RingDotSize / 2 + 3);
                }
                
                if (ringLabelStyle != null)
                {
                    GUIStyle enhancedRingStyle = new GUIStyle(ringLabelStyle);
                    enhancedRingStyle.normal.textColor = ringInteractive ? Color.yellow : Color.white;
                    enhancedRingStyle.fontStyle = FontStyle.Bold;
                    
                    string labelText = $"{i + 1}";
                    Handles.Label(ringGui + Vector2.one * (RingLabelOffset * RingDotSize), labelText, enhancedRingStyle);
                }

                // Connection lines
                if (i == 0)
                {
                    Handles.color = new Color(0.4f, 0.8f, 1f, 0.8f);
                    Handles.DrawLine(ballSpawnGui, ringGui);
                }
                else
                {
                    Handles.color = new Color(0.8f, 0.8f, 0.4f, 0.7f);
                    Vector2 prevRingGui = WorldXYToGuiPos(levelData.ringPositions[i - 1], guiRect);
                    Handles.DrawLine(prevRingGui, ringGui);
                }
            }
        }
    }

    private void DrawSceneViewElements(ThrowingLevelData levelData)
    {
        // Enhanced Ball Spawn
        Handles.color = new Color(0.3f, 0.6f, 1f, 0.8f);
        float ballHandleSize = HandleUtility.GetHandleSize(levelData.ballSpawnPosition) * 0.15f;
        Handles.SphereHandleCap(0, levelData.ballSpawnPosition, Quaternion.identity, ballHandleSize, EventType.Repaint);
        
        // Ball spawn glow
        Handles.color = new Color(0.2f, 0.4f, 1f, 0.3f);
        Handles.SphereHandleCap(0, levelData.ballSpawnPosition, Quaternion.identity, ballHandleSize * 2f, EventType.Repaint);
        
        // Professional label
        GUIStyle ballStyle = new GUIStyle(EditorStyles.whiteMiniLabel);
        ballStyle.fontStyle = FontStyle.Bold;
        ballStyle.normal.textColor = new Color(0.8f, 0.9f, 1f);
        Handles.Label(levelData.ballSpawnPosition + Vector3.up * ballHandleSize * 2.5f, "🎯 Ball Spawn", ballStyle);

        // Enhanced Respawn Zone
        Handles.color = new Color(1f, 0.3f, 0.3f, 0.7f);
        float respawnHandleSize = HandleUtility.GetHandleSize(levelData.respawnZonePosition) * 0.3f;
        Handles.DrawWireCube(levelData.respawnZonePosition, Vector3.one * respawnHandleSize);
        
        // Respawn zone glow
        Handles.color = new Color(1f, 0.2f, 0.2f, 0.2f);
        Handles.CubeHandleCap(0, levelData.respawnZonePosition, Quaternion.identity, respawnHandleSize * 1.5f, EventType.Repaint);
        
        GUIStyle respawnStyle = new GUIStyle(EditorStyles.whiteMiniLabel);
        respawnStyle.fontStyle = FontStyle.Bold;
        respawnStyle.normal.textColor = new Color(1f, 0.7f, 0.7f);
        Handles.Label(levelData.respawnZonePosition + Vector3.up * respawnHandleSize, "⚠️ Respawn Zone", respawnStyle);

        // Enhanced Rings and connections
        if (levelData.ringPositions != null)
        {
            for (int i = 0; i < levelData.ringPositions.Count; i++)
            {
                Vector3 ringPos = levelData.ringPositions[i];
                float distance = Vector3.Distance(levelData.ballSpawnPosition, ringPos);
                Color ringColor = GetDifficultyColor(GetThrowingDifficulty(distance));
                
                float ringHandleSize = HandleUtility.GetHandleSize(ringPos) * 0.12f;
                
                // Ring with difficulty color
                Handles.color = ringColor;
                Handles.SphereHandleCap(0, ringPos, Quaternion.identity, ringHandleSize, EventType.Repaint);
                
                // Ring glow
                Handles.color = new Color(ringColor.r, ringColor.g, ringColor.b, 0.3f);
                Handles.SphereHandleCap(0, ringPos, Quaternion.identity, ringHandleSize * 2f, EventType.Repaint);
                
                // Professional ring label
                GUIStyle ringStyle = new GUIStyle(EditorStyles.whiteMiniLabel);
                ringStyle.fontStyle = FontStyle.Bold;
                ringStyle.normal.textColor = Color.white;
                
                string difficulty = GetThrowingDifficulty(distance);
                string ringLabel = $"🎯 Ring {i + 1}\n{distance:F1}m • {difficulty}";
                Handles.Label(ringPos + Vector3.up * ringHandleSize * 3f, ringLabel, ringStyle);

                // Enhanced connection lines
                if (i == 0)
                {
                    Handles.color = new Color(0.4f, 0.8f, 1f, 0.6f);
                    Handles.DrawDottedLine(levelData.ballSpawnPosition, ringPos, 8.0f);
                }
                else if (i > 0)
                {
                    Handles.color = new Color(0.8f, 0.8f, 0.4f, 0.5f);
                    Handles.DrawDottedLine(levelData.ringPositions[i - 1], ringPos, 5.0f);
                }
            }
        }
    }

    private void DrawSceneViewThrowingRanges(ThrowingLevelData levelData)
    {
        Vector3 ballSpawn = levelData.ballSpawnPosition;
        
        // Draw throwing range circles in Scene view
        foreach (var range in ThrowingRanges)
        {
            Handles.color = range.fillColor;
            Handles.DrawSolidDisc(ballSpawn, Vector3.up, range.maxRange);
            
            Handles.color = range.outlineColor;
            Handles.DrawWireDisc(ballSpawn, Vector3.up, range.maxRange);
            
            // Label at the edge of each range
            Vector3 labelPos = ballSpawn + new Vector3(range.maxRange * 0.7f, range.humanHeight, range.maxRange * 0.7f);
            Handles.Label(labelPos, $"{range.name}\n{range.maxRange:F1}m", EditorStyles.whiteMiniLabel);
        }
        
        // Draw human height indicators at ball spawn
        Vector3 sittingPos = ballSpawn + new Vector3(0, 1.2f, 0);
        Vector3 standingPos = ballSpawn + new Vector3(0, 1.7f, 0);
        
        // Sitting indicator
        Handles.color = new Color(0.3f, 0.7f, 1f, 0.8f);
        Handles.DrawWireCube(sittingPos, new Vector3(0.5f, 0.1f, 0.5f));
        Handles.Label(sittingPos + Vector3.up * 0.2f, "Sitting Height", EditorStyles.whiteMiniLabel);
        
        // Standing indicator  
        Handles.color = new Color(1f, 0.7f, 0.3f, 0.8f);
        Handles.DrawWireCube(standingPos, new Vector3(0.5f, 0.1f, 0.5f));
        Handles.Label(standingPos + Vector3.up * 0.2f, "Standing Height", EditorStyles.whiteMiniLabel);
        
        // Connect the heights with a line
        Handles.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        Handles.DrawLine(sittingPos, standingPos);
    }

    private void DrawEnvironmentGenerationBounds(ThrowingLevelData levelData)
    {
        Vector3 cubicalBoundsMin, cubicalBoundsMax, effectiveGenerationMin, effectiveGenerationMax;
        EnvironmentGeneratorEditorUtil.CalculateGenerationBounds(levelData, 
            out cubicalBoundsMin, out cubicalBoundsMax, 
            out effectiveGenerationMin, out effectiveGenerationMax);

        // Enhanced Level Boundary (EXCLUSION ZONE)
        Handles.color = new Color(1f, 0.3f, 0.3f, 0.4f);
        Vector3 cubicalCenter = (cubicalBoundsMin + cubicalBoundsMax) / 2f;
        Vector3 cubicalSize = cubicalBoundsMax - cubicalBoundsMin;
        Handles.DrawWireCube(cubicalCenter, cubicalSize);
        
        // Add corner markers for better visibility
        Handles.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        Vector3[] corners = {
            new Vector3(cubicalBoundsMin.x, cubicalBoundsMin.y, cubicalBoundsMin.z),
            new Vector3(cubicalBoundsMax.x, cubicalBoundsMin.y, cubicalBoundsMin.z),
            new Vector3(cubicalBoundsMin.x, cubicalBoundsMax.y, cubicalBoundsMin.z),
            new Vector3(cubicalBoundsMax.x, cubicalBoundsMax.y, cubicalBoundsMin.z),
            new Vector3(cubicalBoundsMin.x, cubicalBoundsMin.y, cubicalBoundsMax.z),
            new Vector3(cubicalBoundsMax.x, cubicalBoundsMin.y, cubicalBoundsMax.z),
            new Vector3(cubicalBoundsMin.x, cubicalBoundsMax.y, cubicalBoundsMax.z),
            new Vector3(cubicalBoundsMax.x, cubicalBoundsMax.y, cubicalBoundsMax.z)
        };
        
        foreach (var corner in corners)
        {
            Handles.CubeHandleCap(0, corner, Quaternion.identity, 0.2f, EventType.Repaint);
        }
        
        // Professional label
        GUIStyle boundaryStyle = new GUIStyle(EditorStyles.boldLabel);
        boundaryStyle.normal.textColor = new Color(1f, 0.6f, 0.6f);
        Handles.Label(cubicalBoundsMax + Vector3.up * 1f, "🚫 Level Boundary (EXCLUSION)", boundaryStyle);

        // Enhanced Outer generation area
        if (effectiveGenerationMin.x < effectiveGenerationMax.x && effectiveGenerationMin.z < effectiveGenerationMax.z)
        {
            Handles.color = new Color(0.2f, 1f, 0.4f, 0.3f);
            Vector3 outerCenter = (effectiveGenerationMin + effectiveGenerationMax) / 2f;
            Vector3 outerSize = effectiveGenerationMax - effectiveGenerationMin;
            Handles.DrawWireCube(outerCenter, outerSize);
            
            // Outer boundary label
            GUIStyle outerStyle = new GUIStyle(EditorStyles.boldLabel);
            outerStyle.normal.textColor = new Color(0.4f, 1f, 0.6f);
            Handles.Label(effectiveGenerationMax + Vector3.up * 1f, "🌍 Outer Generation Area", outerStyle);
        }
        else
        {
            Handles.color = new Color(1f, 0f, 0f, 0.7f);
            Vector3 warningPos = cubicalCenter + Vector3.up * (cubicalSize.y / 2f + 2f);
            GUIStyle warningStyle = new GUIStyle(EditorStyles.boldLabel);
            warningStyle.normal.textColor = Color.red;
            Handles.Label(warningPos, "⚠️ Invalid Generation Area", warningStyle);
        }

        // Enhanced exclusion zones
        Handles.color = new Color(1f, 0.5f, 0f, 0.25f);
        float exclusionRadius = levelData.generationExclusionRadius;
        
        List<Vector3> exclusionPositions = levelData.GetExclusionPositions();
        foreach (Vector3 pos in exclusionPositions)
        {
            // Draw semi-transparent sphere using multiple wire discs
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f * Mathf.Deg2Rad;
                Vector3 normal = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0);
                Handles.color = new Color(1f, 0.5f, 0f, 0.15f);
                Handles.DrawWireDisc(pos, normal, exclusionRadius);
            }
            
            // Main exclusion discs
            Handles.color = new Color(1f, 0.5f, 0f, 0.4f);
            Handles.DrawWireDisc(pos, Vector3.up, exclusionRadius);
            Handles.DrawWireDisc(pos, Vector3.right, exclusionRadius);
            Handles.DrawWireDisc(pos, Vector3.forward, exclusionRadius);
        }

        // Exclusion zones label
        if (exclusionPositions.Count > 0)
        {
            Handles.color = new Color(1f, 0.7f, 0.3f);
            Vector3 labelPos = levelData.ballSpawnPosition + Vector3.up * (exclusionRadius + 2f);
            GUIStyle exclusionStyle = new GUIStyle(EditorStyles.boldLabel);
            exclusionStyle.normal.textColor = new Color(1f, 0.7f, 0.3f);
            Handles.Label(labelPos, "🔄 Individual Exclusion Zones", exclusionStyle);
        }
    }

    private void DrawActionButtons(ThrowingLevelData levelData)
    {
        EditorGUILayout.BeginHorizontal();
        
        // Focus Scene View button with icon
        if (GUILayout.Button("🔍 Focus Scene View", GUILayout.Height(30), GUILayout.MinWidth(140)))
        {
            FocusSceneView(levelData);
        }
        
        // Quick validation button
        if (GUILayout.Button("✓ Quick Validate", GUILayout.Height(30), GUILayout.MinWidth(120)))
        {
            var issues = levelData.ValidateLevelData();
            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Validation", "✓ Level validation passed successfully!", "OK");
            }
            else
            {
                string issueText = "Issues found:\n• " + string.Join("\n• ", issues);
                EditorUtility.DisplayDialog("Validation Issues", issueText, "OK");
            }
        }
        
        EditorGUILayout.EndHorizontal();
    }

    private void FocusSceneView(ThrowingLevelData levelData)
    {
        if (SceneView.lastActiveSceneView == null) return;

        Bounds boundsToFrame = levelData.GetCubicalBoundary();
        
        // Ensure all key points are included
        boundsToFrame.Encapsulate(levelData.ballSpawnPosition);
        boundsToFrame.Encapsulate(levelData.respawnZonePosition);
        
        if (levelData.ringPositions != null)
        {
            foreach (var pos in levelData.ringPositions)
            {
                boundsToFrame.Encapsulate(pos);
            }
        }

        // Expand to include throwing ranges and exclusion zones
        boundsToFrame.Expand(Mathf.Max(ThrowingRanges[ThrowingRanges.Length - 1].maxRange, levelData.generationExclusionRadius) * 2f);
        
        SceneView.lastActiveSceneView.Frame(boundsToFrame, false);
        SceneView.lastActiveSceneView.Repaint();
    }
}