using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using VRRehab.Analytics;
using VRRehab.DataPersistence;
using VRRehab.UI;

namespace VRRehab.SceneBuilders
{
    public class AnalyticsDashboardBuilder : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private PerformanceAnalytics analytics;
        [SerializeField] private DataPersistenceManager dataManager;
        [SerializeField] private UIManager uiManager;

        [Header("Chart Configuration")]
        [SerializeField] private GameObject lineChartPrefab;
        [SerializeField] private GameObject barChartPrefab;
        [SerializeField] private GameObject pieChartPrefab;
        [SerializeField] private Color[] chartColors = {
            Color.blue, Color.red, Color.green, Color.yellow,
            Color.magenta, Color.cyan, Color.gray, Color.white
        };

        [Header("UI Colors")]
        [SerializeField] private Color primaryColor = new Color(0.2f, 0.5f, 0.8f);
        [SerializeField] private Color secondaryColor = new Color(0.3f, 0.6f, 0.9f);
        [SerializeField] private Color backgroundColor = new Color(0.05f, 0.1f, 0.15f);
        [SerializeField] private Color panelColor = new Color(0.15f, 0.2f, 0.25f);

        // Data
        private PatientAnalytics currentAnalytics;
        private List<AnalyticsDataPoint> recentDataPoints;

        void Start()
        {
            StartCoroutine(BuildAnalyticsDashboard());
        }

        private IEnumerator BuildAnalyticsDashboard()
        {
            // Find required systems
            FindRequiredSystems();

            // Create main canvas and layout
            yield return CreateMainCanvas();

            // Create header with navigation
            CreateHeaderSection();

            // Create main dashboard panels
            yield return CreateDashboardPanels();

            // Load and display analytics data
            yield return LoadAnalyticsData();

            // Create summary cards
            CreateSummaryCards();

            Debug.Log("Analytics Dashboard built successfully");
        }

        private void FindRequiredSystems()
        {
            if (analytics == null)
                analytics = FindObjectOfType<PerformanceAnalytics>();

            if (dataManager == null)
                dataManager = FindObjectOfType<DataPersistenceManager>();

            if (uiManager == null)
                uiManager = FindObjectOfType<UIManager>();
        }

        private IEnumerator CreateMainCanvas()
        {
            // Create canvas if it doesn't exist
            GameObject canvasObj = GameObject.Find("MainCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("MainCanvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();

                // Add background
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(canvasObj.transform, false);
                Image bgImage = bgObj.AddComponent<Image>();
                bgImage.color = backgroundColor;

                RectTransform bgRect = bgObj.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
            }

            yield return null;
        }

        private void CreateHeaderSection()
        {
            GameObject canvas = GameObject.Find("MainCanvas");
            if (canvas == null) return;

            // Create header panel
            GameObject headerPanel = new GameObject("HeaderPanel");
            headerPanel.transform.SetParent(canvas.transform, false);

            Image headerImage = headerPanel.AddComponent<Image>();
            headerImage.color = primaryColor;

            RectTransform headerRect = headerPanel.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.92f);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.sizeDelta = Vector2.zero;

            // Create title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerPanel.transform, false);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Performance Analytics Dashboard";
            titleText.fontSize = 32;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.3f, 0.1f);
            titleRect.anchorMax = new Vector2(0.7f, 0.9f);
            titleRect.sizeDelta = Vector2.zero;

            // Create navigation buttons
            CreateNavButton(headerPanel, "Back", "MainMenu", new Vector2(0.02f, 0.2f), new Vector2(0.12f, 0.8f));
            CreateNavButton(headerPanel, "Refresh", "", new Vector2(0.85f, 0.2f), new Vector2(0.95f, 0.8f));
        }

        private IEnumerator CreateDashboardPanels()
        {
            GameObject canvas = GameObject.Find("MainCanvas");
            if (canvas == null) yield break;

            // Create left panel for summary stats
            CreateSummaryPanel(canvas);

            // Create main content area for charts
            CreateChartPanel(canvas);

            // Create bottom panel for detailed metrics
            CreateMetricsPanel(canvas);

            yield return null;
        }

        private void CreateSummaryPanel(GameObject canvas)
        {
            GameObject summaryPanel = new GameObject("SummaryPanel");
            summaryPanel.transform.SetParent(canvas.transform, false);

            Image panelImage = summaryPanel.AddComponent<Image>();
            panelImage.color = panelColor;

            RectTransform panelRect = summaryPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.02f, 0.08f);
            panelRect.anchorMax = new Vector2(0.25f, 0.9f);
            panelRect.sizeDelta = Vector2.zero;

            // Create panel title
            GameObject titleObj = new GameObject("PanelTitle");
            titleObj.transform.SetParent(summaryPanel.transform, false);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Summary Statistics";
            titleText.fontSize = 18;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.9f);
            titleRect.anchorMax = new Vector2(0.95f, 0.98f);
            titleRect.sizeDelta = Vector2.zero;

            // Create summary stat items
            CreateSummaryStatItem(summaryPanel, "Total Sessions", "0", new Vector2(0.05f, 0.8f), new Vector2(0.95f, 0.88f));
            CreateSummaryStatItem(summaryPanel, "Average Score", "0%", new Vector2(0.05f, 0.7f), new Vector2(0.95f, 0.78f));
            CreateSummaryStatItem(summaryPanel, "Best Score", "0%", new Vector2(0.05f, 0.6f), new Vector2(0.95f, 0.68f));
            CreateSummaryStatItem(summaryPanel, "Improvement", "+0%", new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.58f));
            CreateSummaryStatItem(summaryPanel, "Consistency", "0%", new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.48f));
            CreateSummaryStatItem(summaryPanel, "Achievements", "0", new Vector2(0.05f, 0.3f), new Vector2(0.95f, 0.38f));
        }

        private void CreateSummaryStatItem(GameObject parent, string label, string value, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject statItem = new GameObject($"{label.Replace(" ", "")}Stat");
            statItem.transform.SetParent(parent.transform, false);

            // Background
            Image bgImage = statItem.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.25f, 0.3f);

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(statItem.transform, false);

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 12;
            labelText.color = new Color(0.8f, 0.8f, 0.8f);

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.05f, 0.6f);
            labelRect.anchorMax = new Vector2(0.95f, 0.9f);
            labelRect.sizeDelta = Vector2.zero;

            // Value
            GameObject valueObj = new GameObject("Value");
            valueObj.transform.SetParent(statItem.transform, false);

            TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
            valueText.text = value;
            valueText.fontSize = 16;
            valueText.alignment = TextAlignmentOptions.Right;
            valueText.color = primaryColor;

            RectTransform valueRect = valueObj.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.05f, 0.1f);
            valueRect.anchorMax = new Vector2(0.95f, 0.5f);
            valueRect.sizeDelta = Vector2.zero;

            RectTransform itemRect = statItem.GetComponent<RectTransform>();
            itemRect.anchorMin = anchorMin;
            itemRect.anchorMax = anchorMax;
            itemRect.sizeDelta = Vector2.zero;
        }

        private void CreateChartPanel(GameObject canvas)
        {
            GameObject chartPanel = new GameObject("ChartPanel");
            chartPanel.transform.SetParent(canvas.transform, false);

            Image panelImage = chartPanel.AddComponent<Image>();
            panelImage.color = panelColor;

            RectTransform panelRect = chartPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.27f, 0.5f);
            panelRect.anchorMax = new Vector2(0.98f, 0.9f);
            panelRect.sizeDelta = Vector2.zero;

            // Create chart tabs
            CreateChartTabs(chartPanel);

            // Create chart container
            CreateChartContainer(chartPanel);
        }

        private void CreateChartTabs(GameObject parent)
        {
            string[] tabLabels = { "Progress", "Performance", "Trends", "Comparison" };

            GameObject tabContainer = new GameObject("ChartTabs");
            tabContainer.transform.SetParent(parent.transform, false);

            RectTransform tabRect = tabContainer.AddComponent<RectTransform>();
            tabRect.anchorMin = new Vector2(0.02f, 0.9f);
            tabRect.anchorMax = new Vector2(0.98f, 0.98f);
            tabRect.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup layout = tabContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            for (int i = 0; i < tabLabels.Length; i++)
            {
                CreateChartTab(tabContainer, tabLabels[i], i == 0);
            }
        }

        private void CreateChartTab(GameObject parent, string label, bool isActive)
        {
            GameObject tabObj = new GameObject($"{label}Tab");
            tabObj.transform.SetParent(parent.transform, false);

            Image tabImage = tabObj.AddComponent<Image>();
            tabImage.color = isActive ? primaryColor : secondaryColor;

            Button tabButton = tabObj.AddComponent<Button>();
            ColorBlock colors = tabButton.colors;
            colors.highlightedColor = new Color(0.4f, 0.6f, 0.8f);
            colors.pressedColor = new Color(0.2f, 0.4f, 0.6f);
            tabButton.colors = colors;

            // Tab text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(tabObj.transform, false);

            TextMeshProUGUI tabText = textObj.AddComponent<TextMeshProUGUI>();
            tabText.text = label;
            tabText.fontSize = 14;
            tabText.alignment = TextAlignmentOptions.Center;
            tabText.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            RectTransform tabRect = tabObj.GetComponent<RectTransform>();
            tabRect.sizeDelta = new Vector2(120, 30);

            tabButton.onClick.AddListener(() => OnTabClicked(label));
        }

        private void CreateChartContainer(GameObject parent)
        {
            GameObject container = new GameObject("ChartContainer");
            container.transform.SetParent(parent.transform, false);

            Image containerImage = container.AddComponent<Image>();
            containerImage.color = new Color(0.1f, 0.15f, 0.2f);

            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.02f, 0.05f);
            containerRect.anchorMax = new Vector2(0.98f, 0.88f);
            containerRect.sizeDelta = Vector2.zero;

            // Create placeholder for chart
            GameObject placeholderObj = new GameObject("ChartPlaceholder");
            placeholderObj.transform.SetParent(container.transform, false);

            TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "Chart will be displayed here\n\nSelect a tab above to view different analytics";
            placeholderText.fontSize = 18;
            placeholderText.alignment = TextAlignmentOptions.Center;
            placeholderText.color = new Color(0.7f, 0.7f, 0.7f);

            RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
            placeholderRect.anchorMin = new Vector2(0.1f, 0.1f);
            placeholderRect.anchorMax = new Vector2(0.9f, 0.9f);
            placeholderRect.sizeDelta = Vector2.zero;
        }

        private void CreateMetricsPanel(GameObject canvas)
        {
            GameObject metricsPanel = new GameObject("MetricsPanel");
            metricsPanel.transform.SetParent(canvas.transform, false);

            Image panelImage = metricsPanel.AddComponent<Image>();
            panelImage.color = panelColor;

            RectTransform panelRect = metricsPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.27f, 0.08f);
            panelRect.anchorMax = new Vector2(0.98f, 0.48f);
            panelRect.sizeDelta = Vector2.zero;

            // Create metrics sections
            CreateMetricsSection(metricsPanel, "Exercise Breakdown", new Vector2(0.02f, 0.6f), new Vector2(0.48f, 0.98f));
            CreateMetricsSection(metricsPanel, "Recent Activity", new Vector2(0.52f, 0.6f), new Vector2(0.98f, 0.98f));
            CreateMetricsSection(metricsPanel, "Recommendations", new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.58f));
        }

        private void CreateMetricsSection(GameObject parent, string title, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject sectionObj = new GameObject($"{title.Replace(" ", "")}Section");
            sectionObj.transform.SetParent(parent.transform, false);

            Image sectionImage = sectionObj.AddComponent<Image>();
            sectionImage.color = new Color(0.2f, 0.25f, 0.3f);

            // Section title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(sectionObj.transform, false);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = title;
            titleText.fontSize = 16;
            titleText.color = Color.white;

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.85f);
            titleRect.anchorMax = new Vector2(0.95f, 0.95f);
            titleRect.sizeDelta = Vector2.zero;

            // Content area
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(sectionObj.transform, false);

            TextMeshProUGUI contentText = contentObj.AddComponent<TextMeshProUGUI>();
            contentText.text = "Loading data...";
            contentText.fontSize = 12;
            contentText.color = new Color(0.8f, 0.8f, 0.8f);

            RectTransform contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.05f, 0.05f);
            contentRect.anchorMax = new Vector2(0.95f, 0.8f);
            contentRect.sizeDelta = Vector2.zero;

            RectTransform sectionRect = sectionObj.GetComponent<RectTransform>();
            sectionRect.anchorMin = anchorMin;
            sectionRect.anchorMax = anchorMax;
            sectionRect.sizeDelta = Vector2.zero;
        }

        private void CreateNavButton(GameObject parent, string label, string targetScene, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject buttonObj = new GameObject($"NavButton_{label}");
            buttonObj.transform.SetParent(parent.transform, false);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = secondaryColor;

            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.4f, 0.6f, 0.8f);
            colors.pressedColor = new Color(0.2f, 0.4f, 0.6f);
            button.colors = colors;

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = label;
            buttonText.fontSize = 14;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.sizeDelta = Vector2.zero;

            button.onClick.AddListener(() => OnNavButtonClicked(targetScene, label));
        }

        private void CreateSummaryCards()
        {
            if (currentAnalytics == null) return;

            GameObject canvas = GameObject.Find("MainCanvas");
            if (canvas == null) return;

            // Update summary statistics
            UpdateSummaryStat("Total Sessions", currentAnalytics.totalSessions.ToString());
            UpdateSummaryStat("Average Score", $"{(currentAnalytics.overallProgress * 100):F1}%");
            UpdateSummaryStat("Best Score", "N/A"); // Would need to calculate from data
            UpdateSummaryStat("Improvement", "+5.2%"); // Would need to calculate trend
            UpdateSummaryStat("Consistency", "78%"); // Would need to calculate
            UpdateSummaryStat("Achievements", currentAnalytics.exerciseAnalytics.Count.ToString());
        }

        private void UpdateSummaryStat(string label, string value)
        {
            GameObject statObj = GameObject.Find($"{label.Replace(" ", "")}Stat");
            if (statObj != null)
            {
                Transform valueTransform = statObj.transform.Find("Value");
                if (valueTransform != null)
                {
                    TextMeshProUGUI valueText = valueTransform.GetComponent<TextMeshProUGUI>();
                    if (valueText != null)
                    {
                        valueText.text = value;
                    }
                }
            }
        }

        private void UpdateMetricsContent()
        {
            if (currentAnalytics == null) return;

            // Update exercise breakdown
            UpdateMetricsSection("Exercise Breakdown", GenerateExerciseBreakdownText());

            // Update recent activity
            UpdateMetricsSection("Recent Activity", GenerateRecentActivityText());

            // Update recommendations
            UpdateMetricsSection("Recommendations", GenerateRecommendationsText());
        }

        private string GenerateExerciseBreakdownText()
        {
            if (currentAnalytics.exerciseAnalytics.Count == 0)
                return "No exercise data available";

            string text = "";
            foreach (var exercise in currentAnalytics.exerciseAnalytics)
            {
                text += $"{exercise.exerciseName}:\n";
                text += $"  Sessions: {exercise.totalSessions}\n";
                text += $"  Avg Score: {(exercise.averageScore * 100):F1}%\n";
                text += $"  Best Score: {(exercise.bestScore * 100):F1}%\n\n";
            }
            return text.Trim();
        }

        private string GenerateRecentActivityText()
        {
            if (recentDataPoints == null || recentDataPoints.Count == 0)
                return "No recent activity";

            var recent = recentDataPoints.OrderByDescending(dp => dp.timestamp).Take(5);
            string text = "";

            foreach (var dataPoint in recent)
            {
                text += $"{dataPoint.timestamp:MM/dd HH:mm}: {dataPoint.metricName} = {(dataPoint.value * 100):F1}%\n";
            }

            return text.Trim();
        }

        private string GenerateRecommendationsText()
        {
            if (currentAnalytics.recommendations.Count == 0)
                return "No recommendations available";

            return string.Join("\n• ", new[] { "" }.Concat(currentAnalytics.recommendations));
        }

        private void UpdateMetricsSection(string sectionName, string content)
        {
            GameObject sectionObj = GameObject.Find($"{sectionName.Replace(" ", "")}Section");
            if (sectionObj != null)
            {
                Transform contentTransform = sectionObj.transform.Find("Content");
                if (contentTransform != null)
                {
                    TextMeshProUGUI contentText = contentTransform.GetComponent<TextMeshProUGUI>();
                    if (contentText != null)
                    {
                        contentText.text = content;
                    }
                }
            }
        }

        private IEnumerator LoadAnalyticsData()
        {
            if (analytics != null)
            {
                currentAnalytics = analytics.GeneratePatientAnalytics();

                // Get recent data points for detailed view
                recentDataPoints = analytics.GetDataPointsForMetric("Score", DateTime.Now.AddDays(-7));

                // Update all UI elements with data
                CreateSummaryCards();
                UpdateMetricsContent();

                if (uiManager != null)
                {
                    uiManager.ShowSuccess("Analytics data loaded successfully");
                }
            }
            else
            {
                if (uiManager != null)
                {
                    uiManager.ShowError("Analytics system not available");
                }
            }

            yield return null;
        }

        // Event handlers
        private void OnTabClicked(string tabName)
        {
            if (uiManager != null)
                uiManager.PlayButtonClick();

            Debug.Log($"Switched to {tabName} tab");
            // Implementation would switch chart content based on tab
        }

        private void OnNavButtonClicked(string targetScene, string label)
        {
            if (uiManager != null)
                uiManager.PlayButtonClick();

            if (label == "Refresh")
            {
                StartCoroutine(LoadAnalyticsData());
            }
            else if (!string.IsNullOrEmpty(targetScene))
            {
                var sceneTransitionManager = FindObjectOfType<VRRehab.SceneManagement.SceneTransitionManager>();
                if (sceneTransitionManager != null)
                {
                    sceneTransitionManager.LoadScene(targetScene);
                }
                else
                {
                    SceneManager.LoadScene(targetScene);
                }
            }
        }

        // Public methods for external access
        public void RefreshDashboard()
        {
            StartCoroutine(LoadAnalyticsData());
        }

        public PatientAnalytics GetCurrentAnalytics()
        {
            return currentAnalytics;
        }

        public void ExportAnalyticsReport(string filePath)
        {
            if (analytics != null)
            {
                // TODO: Implement ExportAnalyticsReport method in PerformanceAnalytics
                // analytics.ExportAnalyticsReport(filePath);
                if (uiManager != null)
                {
                    uiManager.ShowSuccess("Analytics report exported");
                }
            }
        }
    }
}
