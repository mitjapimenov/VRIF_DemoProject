using TMPro;
using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// World space scoreboard : score, combo multiplier with its time left, and the game over screen
    /// </summary>
    public class BoomHud : MonoBehaviour {

        public BoomScoreManager ScoreManager;

        [Header("Score : ")]
        public TMP_Text ScoreValue;

        [Tooltip("How fast the shown score counts up towards the real score, in points per second")]
        public float ScoreCountSpeed = 1500f;

        [Header("Combo : ")]
        public TMP_Text ComboText;

        [Tooltip("Bar under the combo text that shrinks (width, left pivot) as the combo window runs out")]
        public RectTransform ComboTimer;

        [Tooltip("{0} = combo multiplier")]
        public string ComboFormat = "COMBO x{0}";

        [Tooltip("How much bigger the combo text pops when the multiplier goes up (0.35 = 35 %)")]
        public float ComboPunchScale = 0.35f;

        [Tooltip("How fast the pop settles back, per second")]
        public float ComboPunchSpeed = 4f;

        [Header("Panels : ")]
        public GameObject PlayPanel;
        public GameObject GameOverPanel;
        public TMP_Text GameOverScore;

        float shownScore;
        float comboTimerWidth;
        int lastCombo;
        float comboPunch;

        void Start() {
            if (ComboTimer != null) {
                comboTimerWidth = ComboTimer.sizeDelta.x;
            }
        }

        void Update() {
            if (ScoreManager == null) {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            bool gameOver = ScoreManager.IsGameOver;

            if (PlayPanel != null) {
                PlayPanel.SetActive(!gameOver);
            }
            if (GameOverPanel != null) {
                GameOverPanel.SetActive(gameOver);
            }
            if (gameOver && GameOverScore != null) {
                GameOverScore.text = ScoreManager.Score.ToString();
            }

            // Count up instead of jumping, and snap down instantly on restart
            float target = ScoreManager.Score;
            shownScore = target < shownScore ? target : Mathf.MoveTowards(shownScore, target, ScoreCountSpeed * dt);
            if (ScoreValue != null) {
                ScoreValue.text = Mathf.RoundToInt(shownScore).ToString();
            }

            bool comboActive = ScoreManager.ComboActive;
            if (comboActive && ScoreManager.Combo != lastCombo) {
                comboPunch = 1f;
            }
            lastCombo = comboActive ? ScoreManager.Combo : 0;
            comboPunch = Mathf.MoveTowards(comboPunch, 0f, dt * ComboPunchSpeed);

            if (ComboText != null) {
                ComboText.gameObject.SetActive(comboActive);
                ComboText.text = string.Format(ComboFormat, ScoreManager.Combo);
                ComboText.transform.localScale = Vector3.one * (1f + comboPunch * ComboPunchScale);
            }
            if (ComboTimer != null) {
                ComboTimer.gameObject.SetActive(comboActive);
                Vector2 size = ComboTimer.sizeDelta;
                size.x = comboTimerWidth * ScoreManager.ComboTimeLeft;
                ComboTimer.sizeDelta = size;
            }
        }
    }
}
