using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace RecycleScrollView.Samples.Editor
{
    public sealed class RecycleScrollViewSampleWindow : EditorWindow
    {
        private static readonly Vector2 s_minimumWindowSize = new Vector2(360, 440);

        private static readonly List<string> s_layoutLabels = new List<string>()
        {
            "Vertical",
            "Horizontal",
            "Grid"
        };

        private static readonly List<string> s_effectLabels = new List<string>()
        {
            "None",
            "Carousel",
            "Highlight"
        };

        [SerializeField]
        private SampleSettings _settings = new SampleSettings();
        [SerializeField]
        private bool _autoApply = true;
        [SerializeField]
        private bool _hasPendingChanges;

        private PopupField<string> _layoutField;
        private IntegerField _dataCountField;
        private Vector2Field _viewportSizeField;
        private Vector2Field _slotSizeField;
        private Toggle _variableSlotSizeField;
        private Vector2Field _spacingField;
        private IntegerField _gridColumnsField;
        private PopupField<string> _effectField;
        private FloatField _alignmentField;
        private FloatField _effectDistanceField;
        private Vector2Field _scaleRangeField;
        private Label _alignmentHelp;
        private Label _distanceHelp;
        private Toggle _autoApplyField;
        private Toggle _performanceHudField;
        private Button _applyButton;
        private Button _openSceneButton;
        private Button _playButton;
        private Button _snapNearestButton;

        [MenuItem("Recycle Scroll View/Sample")]
        public static void Open()
        {
            GetWindow<RecycleScrollViewSampleWindow>().Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Scroll View Sample");
            minSize = s_minimumWindowSize;
            if (_settings == null)
            {
                _settings = new SampleSettings();
            }

            _settings.Normalize();
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Undo.undoRedoPerformed += OnUndoRedo;
            SampleSceneTools.SettingsChanged += SyncFromSample;
        }

        private void OnDisable()
        {
            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Undo.undoRedoPerformed -= OnUndoRedo;
            SampleSceneTools.SettingsChanged -= SyncFromSample;
        }

        public void CreateGUI()
        {
            if (_settings == null)
            {
                _settings = new SampleSettings();
            }

            _settings.Normalize();

            var root = rootVisualElement;
            root.Clear();
            root.AddToClassList("sample-window");
            root.EnableInClassList("is-light-theme", !EditorGUIUtility.isProSkin);
            LoadStyleSheet(root);

            var scroll = new UnityEngine.UIElements.ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("sample-scroll");
            root.Add(scroll);

            var content = new VisualElement();
            content.AddToClassList("sample-content");
            scroll.Add(content);

            var title = new Label("Recycle Scroll View");
            title.AddToClassList("sample-title");
            content.Add(title);

            var description = new Label("Choose a layout and configure the sample.");
            description.AddToClassList("sample-description");
            content.Add(description);

            _layoutField = new PopupField<string>("Layout", s_layoutLabels, (int)_settings.Layout)
            {
                name = "layoutField"
            };
            AddField(content, _layoutField);

            _dataCountField = new IntegerField("Data Items")
            {
                name = "dataCountField",
                isDelayed = true,
                tooltip = "0 to 1,000,000 items"
            };
            AddField(content, _dataCountField);

            _viewportSizeField = CreateVectorField(content, "Viewport Size", "viewportSizeField");
            _slotSizeField = CreateVectorField(content, "Slot Size", "slotSizeField");

            _variableSlotSizeField = new Toggle("Variable Slot Size")
            {
                name = "variableSlotSizeField",
                tooltip = "Repeat 0.75, 1, and 1.5 times Slot Size along the scroll axis. Available when Effect is None."
            };
            AddField(content, _variableSlotSizeField);

            _spacingField = CreateVectorField(content, "Spacing", "spacingField");

            _gridColumnsField = new IntegerField("Grid Columns")
            {
                name = "gridColumnsField",
                isDelayed = true
            };
            AddField(content, _gridColumnsField);

            var effectTitle = new Label("Scroll Effect");
            effectTitle.AddToClassList("sample-section-title");
            content.Add(effectTitle);

            _effectField = new PopupField<string>("Effect", s_effectLabels, (int)_settings.Effect)
            {
                name = "effectField"
            };
            AddField(content, _effectField);

            _alignmentField = new FloatField("Alignment")
            {
                name = "alignmentField",
                isDelayed = true,
                tooltip = "0 = start, 0.5 = center, 1 = end of the viewport."
            };
            AddField(content, _alignmentField);

            _alignmentHelp = new Label("Alignment: 0 = start, 0.5 = center, 1 = end of the viewport.");
            _alignmentHelp.AddToClassList("sample-help");
            content.Add(_alignmentHelp);

            _effectDistanceField = new FloatField("Distance")
            {
                name = "effectDistanceField",
                isDelayed = true,
                tooltip = "Effect distance in unscaled content units."
            };
            AddField(content, _effectDistanceField);

            _scaleRangeField = CreateVectorField(content, "Scale Range", "scaleRangeField");
            _scaleRangeField.tooltip = "X = minimum scale; Y = maximum scale.";

            _distanceHelp = new Label("Distance uses unscaled content units. Scale Range: X = min, Y = max.");
            _distanceHelp.AddToClassList("sample-help");
            content.Add(_distanceHelp);

            _snapNearestButton = new Button(SnapToNearest)
            {
                name = "snapNearestButton",
                text = "Snap Nearest",
                tooltip = "Snap the nearest item to the alignment position while playing in Carousel mode."
            };
            _snapNearestButton.AddToClassList("sample-button");
            _snapNearestButton.AddToClassList("sample-carousel-button");
            content.Add(_snapNearestButton);

            _performanceHudField = new Toggle("Performance HUD")
            {
                name = "performanceHudField",
                tooltip = "Show in-game item/slot counts and average app FPS/frame time."
            };
            AddField(content, _performanceHudField);

            var applyRow = new VisualElement();
            applyRow.AddToClassList("sample-apply-row");
            content.Add(applyRow);

            _autoApplyField = new Toggle("Auto Apply")
            {
                name = "autoApplyField",
                tooltip = "Apply confirmed inputs to the current sample."
            };
            _autoApplyField.AddToClassList("sample-auto-apply");
            applyRow.Add(_autoApplyField);

            _applyButton = new Button(() =>
            {
                ApplySettings();
            })
            {
                name = "applyButton",
                text = "Apply"
            };
            _applyButton.AddToClassList("sample-button");
            _applyButton.AddToClassList("sample-apply-button");
            applyRow.Add(_applyButton);

            var sceneActions = new VisualElement();
            sceneActions.AddToClassList("sample-scene-actions");
            content.Add(sceneActions);

            _openSceneButton = new Button(OpenSampleScene)
            {
                name = "openSceneButton",
                text = "Open Sample Scene"
            };
            _openSceneButton.AddToClassList("sample-button");
            _openSceneButton.AddToClassList("sample-open-scene-button");
            sceneActions.Add(_openSceneButton);

            _playButton = new Button(TogglePlayMode)
            {
                name = "playButton"
            };
            _playButton.AddToClassList("sample-button");
            sceneActions.Add(_playButton);

            _layoutField.RegisterValueChangedCallback(_ =>
            {
                OnSettingsChanged();
            });

            _dataCountField.RegisterValueChangedCallback(_ =>
            {
                OnSettingsChanged();
            });

            _viewportSizeField.RegisterValueChangedCallback(_ =>
            {
                OnSettingsChanged();
            });

            _slotSizeField.RegisterValueChangedCallback(_ =>
            {
                OnSettingsChanged();
            });

            _variableSlotSizeField.RegisterValueChangedCallback(_ =>
            {
                OnSettingsChanged();
            });

            _spacingField.RegisterValueChangedCallback(_ =>
            {
                OnSettingsChanged();
            });

            _gridColumnsField.RegisterValueChangedCallback(_ =>
            {
                OnSettingsChanged();
            });

            _effectField.RegisterValueChangedCallback(_ =>
            {
                OnSettingsChanged();
            });

            _alignmentField.RegisterValueChangedCallback(_ =>
            {
                OnSettingsChanged();
            });

            _effectDistanceField.RegisterValueChangedCallback(_ =>
            {
                OnSettingsChanged();
            });

            _scaleRangeField.RegisterValueChangedCallback(_ =>
            {
                OnSettingsChanged();
            });

            _performanceHudField.RegisterValueChangedCallback(_ =>
            {
                OnSettingsChanged();
            });

            _autoApplyField.RegisterValueChangedCallback(evt =>
            {
                OnAutoApplyChanged(evt.newValue);
            });

            WriteFields();
            SyncFromSample();
        }

        private void LoadStyleSheet(VisualElement root)
        {
            var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            var directory = Path.GetDirectoryName(scriptPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{directory}/RecycleScrollViewSampleWindow.uss");
            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }
        }

        private static void AddField(VisualElement parent, VisualElement field)
        {
            field.AddToClassList("sample-field");
            parent.Add(field);
        }

        private static Vector2Field CreateVectorField(VisualElement parent, string label, string name)
        {
            var field = new Vector2Field(label)
            {
                name = name
            };
            field.Query<FloatField>().ForEach(component =>
            {
                component.isDelayed = true;
            });
            AddField(parent, field);
            return field;
        }

        private void OnSettingsChanged()
        {
            _settings.Layout = (SampleLayout)s_layoutLabels.IndexOf(_layoutField.value);
            _settings.DataCount = _dataCountField.value;
            _settings.ViewportSize = _viewportSizeField.value;
            _settings.SlotSize = _slotSizeField.value;
            _settings.VariableSlotSize = _variableSlotSizeField.value;
            _settings.Spacing = _spacingField.value;
            _settings.GridColumns = _gridColumnsField.value;

            _settings.Effect = (SampleEffect)s_effectLabels.IndexOf(_effectField.value);
            _settings.Alignment = _alignmentField.value;
            _settings.EffectDistance = _effectDistanceField.value;
            _settings.HighlightScaleRange = _scaleRangeField.value;
            _settings.ShowPerformanceHud = _performanceHudField.value;

            _settings.Normalize();

            _hasPendingChanges = true;
            WriteFields();

            if (_autoApply && CanApply())
            {
                ApplySettings();
            }
            else
            {
                UpdateButtons();
            }
        }

        private void OnAutoApplyChanged(bool enabled)
        {
            _autoApply = enabled;
            if (enabled && _hasPendingChanges && CanApply())
            {
                ApplySettings();
            }
            else
            {
                UpdateButtons();
            }
        }

        private bool CanApply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode != EditorApplication.isPlaying)
            {
                return false;
            }

            if (!SampleSceneTools.IsSampleSceneActive)
            {
                return false;
            }

            return SampleSceneTools.FindSample() != null;
        }

        private bool ApplySettings()
        {
            if (!CanApply())
            {
                UpdateButtons();
                return false;
            }

            _settings.Normalize();
            var sample = SampleSceneTools.Apply(_settings.Clone());
            if (sample == null)
            {
                UpdateButtons();
                return false;
            }

            _settings = sample.Settings.Clone();
            _hasPendingChanges = false;
            WriteFields();
            UpdateButtons();
            return true;
        }

        private void OpenSampleScene()
        {
            if (SampleSceneTools.OpenSampleScene())
            {
                SyncFromSample();
            }
            else
            {
                UpdateButtons();
            }
        }

        private void TogglePlayMode()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode || !CanApply())
            {
                return;
            }

            if (_autoApply && _hasPendingChanges && !ApplySettings())
            {
                return;
            }

            EditorApplication.isPlaying = true;
            UpdateButtons();
        }

        private void SnapToNearest()
        {
            var sample = SampleSceneTools.FindSample();
            if (!EditorApplication.isPlaying || !CanApply() || sample == null || sample.Settings.Effect != SampleEffect.Carousel)
            {
                return;
            }

            sample.SnapToNearest();
            UpdateButtons();
        }

        private void WriteFields()
        {
            if (_layoutField == null)
            {
                return;
            }

            _layoutField.SetValueWithoutNotify(s_layoutLabels[(int)_settings.Layout]);
            _dataCountField.SetValueWithoutNotify(_settings.DataCount);
            _viewportSizeField.SetValueWithoutNotify(_settings.ViewportSize);
            _slotSizeField.SetValueWithoutNotify(_settings.SlotSize);
            _variableSlotSizeField.SetValueWithoutNotify(_settings.VariableSlotSize);
            _spacingField.SetValueWithoutNotify(_settings.Spacing);
            _gridColumnsField.SetValueWithoutNotify(_settings.GridColumns);

            _effectField.SetValueWithoutNotify(s_effectLabels[(int)_settings.Effect]);
            _alignmentField.SetValueWithoutNotify(_settings.Alignment);
            _effectDistanceField.SetValueWithoutNotify(_settings.EffectDistance);
            _scaleRangeField.SetValueWithoutNotify(_settings.HighlightScaleRange);
            _performanceHudField.SetValueWithoutNotify(_settings.ShowPerformanceHud);

            _autoApplyField.SetValueWithoutNotify(_autoApply);

            bool isGrid = _settings.Layout == SampleLayout.Grid;
            _gridColumnsField.EnableInClassList("is-hidden", !isGrid);
            _gridColumnsField.SetEnabled(isGrid);

            bool hasEffect = _settings.Effect != SampleEffect.None;
            _variableSlotSizeField.SetEnabled(!hasEffect);

            bool isHighlight = _settings.Effect == SampleEffect.Highlight;
            _alignmentField.EnableInClassList("is-hidden", !hasEffect);
            _alignmentField.SetEnabled(hasEffect);
            _alignmentHelp.EnableInClassList("is-hidden", !hasEffect);
            _effectDistanceField.EnableInClassList("is-hidden", !isHighlight);
            _effectDistanceField.SetEnabled(isHighlight);
            _scaleRangeField.EnableInClassList("is-hidden", !isHighlight);
            _scaleRangeField.SetEnabled(isHighlight);
            _distanceHelp.EnableInClassList("is-hidden", !isHighlight);
        }

        private void SyncFromSample()
        {
            var sample = SampleSceneTools.FindSample();
            if (SampleSceneTools.IsSampleSceneActive && sample != null && !_hasPendingChanges)
            {
                if (sample.Settings == null)
                {
                    _settings = new SampleSettings();
                }
                else
                {
                    _settings = sample.Settings.Clone();
                }

                _settings.Normalize();
                _hasPendingChanges = false;
                WriteFields();
            }

            UpdateButtons();
        }

        private void OnActiveSceneChanged(Scene previousScene, Scene newScene)
        {
            SyncFromSample();
        }

        private void OnUndoRedo()
        {
            SyncFromSample();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.EnteredPlayMode)
            {
                SyncFromSample();
            }

            UpdateButtons();
        }

        private void UpdateButtons()
        {
            if (_applyButton == null)
            {
                return;
            }

            bool isSampleScene = SampleSceneTools.IsSampleSceneActive;
            var sample = SampleSceneTools.FindSample();
            bool hasSample = sample != null;
            bool isPlaying = EditorApplication.isPlaying;
            bool isChangingPlayMode = EditorApplication.isPlayingOrWillChangePlaymode != isPlaying;
            bool canApply = isSampleScene && hasSample && !isChangingPlayMode;

            _applyButton.SetEnabled(canApply);
            _openSceneButton.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode && !isPlaying);

            if (isPlaying)
            {
                _playButton.text = "Stop";
            }
            else
            {
                _playButton.text = "Play";
            }

            _playButton.SetEnabled(!isChangingPlayMode && (isPlaying || isSampleScene && hasSample));

            bool canSnap = false;
            if (isPlaying && !isChangingPlayMode && isSampleScene && hasSample)
            {
                canSnap = sample.Settings.Effect == SampleEffect.Carousel;
            }

            _snapNearestButton.SetEnabled(canSnap);
        }
    }
}
