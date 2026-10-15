using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace RecycleScrollView.Samples.Editor
{
    [CustomEditor(typeof(RecycleScrollViewSample), true)]
    public sealed class RecycleScrollViewSampleInspector : UnityEditor.Editor
    {
        private PropertyField _variableSizeField;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            _variableSizeField = null;

            var property = serializedObject.GetIterator();
            if (property.NextVisible(true))
            {
                do
                {
                    if (property.name == "_settings")
                    {
                        var settingsFoldout = new Foldout
                        {
                            text = property.displayName,
                            value = property.isExpanded
                        };
                        settingsFoldout.RegisterValueChangedCallback(evt =>
                        {
                            if (evt.target == settingsFoldout)
                            {
                                serializedObject.FindProperty("_settings").isExpanded = evt.newValue;
                            }
                        });
                        root.Add(settingsFoldout);

                        var setting = property.Copy();
                        var end = setting.GetEndProperty();
                        if (setting.NextVisible(true))
                        {
                            do
                            {
                                if (SerializedProperty.EqualContents(setting, end))
                                {
                                    break;
                                }

                                var field = new PropertyField(setting.Copy());
                                settingsFoldout.Add(field);
                                if (setting.name == "_variableSlotSize")
                                {
                                    field.name = "variableSlotSizeField";
                                    _variableSizeField = field;
                                }
                            }
                            while (setting.NextVisible(false));
                        }
                    }
                    else
                    {
                        var field = new PropertyField(property.Copy());
                        field.SetEnabled(property.name != "m_Script");
                        root.Add(field);
                    }
                }
                while (property.NextVisible(false));
            }

            UpdateVariableSizeState();

            root.TrackPropertyValue(serializedObject.FindProperty("_settings._effect"), _ =>
            {
                UpdateVariableSizeState();
            });

            root.RegisterCallback<SerializedPropertyChangeEvent>(_ =>
            {
                UpdateVariableSizeState();
                SampleSceneTools.QueueSettingsRefresh();
            });

            return root;
        }

        private void UpdateVariableSizeState()
        {
            var effect = serializedObject.FindProperty("_settings._effect");
            if (_variableSizeField != null)
            {
                _variableSizeField.SetEnabled(effect.enumValueIndex == (int)SampleEffect.None);
            }
        }
    }
}
