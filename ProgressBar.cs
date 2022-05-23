using UnityEngine;
using UnityEngine.UI;

namespace SpaceApple.MultiRoom
{

    /// <summary>
    /// Generic progress bar, which will be used to display health
    /// </summary>
    public class ProgressBar : MonoBehaviour
    {
        public RectTransform ProgressTransform;
        public RectTransform TotalTransform;
        public Text Text;

        private float _max = 2;
        private float _current = 1;

        private float _maxWidth;

        public ProgressBarType Type;

        [Header("Initial values")]
        public float InitialMax = 10;
        public float InitialCurrent = 10;

        void Awake()
        {
            _maxWidth = TotalTransform.rect.width;

            Set(InitialCurrent, InitialMax);
        }

        /// <summary>
        /// Sets current and max values of the progress bar
        /// </summary>
        /// <param name="current"></param>
        /// <param name="max"></param>
        public void Set(float current, float max)
        {
            SetMax(max);
            SetCurrent(current);
        }

        /// <summary>
        /// Sets current value of the progress bar
        /// </summary>
        /// <param name="current"></param>
        public void SetCurrent(float current)
        {
            _current = current;

            if (Text != null)
            {
                UpdateText();
            }

            UpdateBarView();
        }

        void UpdateText()
        {
            switch (Type)
            {
                case ProgressBarType.CurrentFloatNoMax:
                    Text.text = _current.ToString();
                    break;
                case ProgressBarType.CurrentAndMaxInt:
                    Text.text = (int) _current + "/" + (int) _max;
                    break;
            }
        }

        void UpdateBarView()
        {
            ProgressTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(_maxWidth * (_current / _max), _maxWidth));
        }

        public void SetMax(float max)
        {
            _max = max;
            UpdateBarView();
        }

        public enum ProgressBarType
        {
            CurrentFloatNoMax,
            CurrentAndMaxInt
        }
    }

}