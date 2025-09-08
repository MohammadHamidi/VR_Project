// Assets/Editor/CirclePopulatorEditor.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Tools.Spawning;

[CustomEditor(typeof(CirclePopulator))]
public class CirclePopulatorEditor : Editor
{
    private CirclePopulator _pop;
    private SerializedProperty _prefabs, _count, _radius, _ringThickness;
    private SerializedProperty _randomYaw;

    // scale props
    private SerializedProperty _scaleDistribution, _uniformScale, _perAxisMin, _perAxisMax, _scaleVariants;
    private SerializedProperty _gaussianClamp, _gaussianMean, _gaussianStdDev, _gaussianPerAxis;
    private SerializedProperty _usePerlinJitter, _perlinFrequency, _perlinAmplitude;

    private SerializedProperty _alignToGround, _groundMask, _groundRayStartHeight, _groundRayLength;
    private SerializedProperty _spawnOnStart, _useSeed, _seed, _parentOverride;

    private void OnEnable()
    {
        _pop = (CirclePopulator)target;

        _prefabs = serializedObject.FindProperty("prefabs");
        _count = serializedObject.FindProperty("count");
        _radius = serializedObject.FindProperty("radius");
        _ringThickness = serializedObject.FindProperty("ringThickness");
        _randomYaw = serializedObject.FindProperty("randomYaw");

        _scaleDistribution = serializedObject.FindProperty("scaleDistribution");
        _uniformScale = serializedObject.FindProperty("uniformScale");
        _perAxisMin = serializedObject.FindProperty("perAxisMin");
        _perAxisMax = serializedObject.FindProperty("perAxisMax");
        _scaleVariants = serializedObject.FindProperty("scaleVariants");
        _gaussianClamp = serializedObject.FindProperty("gaussianClamp");
        _gaussianMean = serializedObject.FindProperty("gaussianMean");
        _gaussianStdDev = serializedObject.FindProperty("gaussianStdDev");
        _gaussianPerAxis = serializedObject.FindProperty("gaussianPerAxis");
        _usePerlinJitter = serializedObject.FindProperty("usePerlinJitter");
        _perlinFrequency = serializedObject.FindProperty("perlinFrequency");
        _perlinAmplitude = serializedObject.FindProperty("perlinAmplitude");

        _alignToGround = serializedObject.FindProperty("alignToGround");
        _groundMask = serializedObject.FindProperty("groundMask");
        _groundRayStartHeight = serializedObject.FindProperty("groundRayStartHeight");
        _groundRayLength = serializedObject.FindProperty("groundRayLength");

        _spawnOnStart = serializedObject.FindProperty("spawnOnStart");
        _useSeed = serializedObject.FindProperty("useSeed");
        _seed = serializedObject.FindProperty("seed");
        _parentOverride = serializedObject.FindProperty("parentOverride");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_prefabs, true);
        EditorGUILayout.PropertyField(_count);
        EditorGUILayout.PropertyField(_radius);
        EditorGUILayout.PropertyField(_ringThickness);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_randomYaw);

        // ==== Scale UI ====
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scale Randomization", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_scaleDistribution);

        var dist = (ScaleDistribution)_scaleDistribution.enumValueIndex;
        switch (dist)
        {
            case ScaleDistribution.UniformRange:
                EditorGUILayout.PropertyField(_uniformScale, new GUIContent("Uniform Scale (min,max)"));
                break;
            case ScaleDistribution.PerAxisRange:
                EditorGUILayout.PropertyField(_perAxisMin);
                EditorGUILayout.PropertyField(_perAxisMax);
                break;
            case ScaleDistribution.VariantList:
                EditorGUILayout.PropertyField(_scaleVariants, true);
                break;
            case ScaleDistribution.Gaussian:
                EditorGUILayout.PropertyField(_gaussianMean);
                EditorGUILayout.PropertyField(_gaussianStdDev);
                EditorGUILayout.PropertyField(_gaussianClamp, new GUIContent("Clamp Min/Max"));
                EditorGUILayout.PropertyField(_gaussianPerAxis);
                break;
        }

        EditorGUILayout.PropertyField(_usePerlinJitter);
        if (_usePerlinJitter.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_perlinFrequency);
            EditorGUILayout.PropertyField(_perlinAmplitude);
            EditorGUI.indentLevel--;
        }

        // Ground align
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_alignToGround);
        if (_alignToGround.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_groundMask);
            EditorGUILayout.PropertyField(_groundRayStartHeight);
            EditorGUILayout.PropertyField(_groundRayLength);
            EditorGUI.indentLevel--;
        }

        // Runtime + seed + parenting
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_spawnOnStart);
        EditorGUILayout.PropertyField(_useSeed);
        if (_useSeed.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_seed);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.PropertyField(_parentOverride);

        EditorGUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Spawn Now"))
            {
                _pop.ClearSpawned();
                _pop.Spawn();
                SceneView.RepaintAll();
            }
            if (GUILayout.Button("Clear Spawned"))
            {
                _pop.ClearSpawned();
                SceneView.RepaintAll();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    // Scene radius handle centered at world origin
    private void OnSceneGUI()
    {
        if (_pop == null) return;

        EditorGUI.BeginChangeCheck();
        float newRadius = Handles.RadiusHandle(Quaternion.identity, Vector3.zero, _pop.radius);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_pop, "Change Circle Radius");
            _pop.radius = Mathf.Max(0.01f, newRadius);
        }
    }
}
#endif
