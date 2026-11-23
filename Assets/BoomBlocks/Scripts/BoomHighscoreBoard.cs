using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoomBlocks {

    /// <summary>
    /// One saved result. The name is optional
    /// </summary>
    [System.Serializable]
    public class BoomHighscoreEntry {
        public int Score;
        public string Name;
        public string Date;
    }

    /// <summary>
    /// Top scores, saved as a JSON file in Application.persistentDataPath so they survive closing the game or leaving Play mode.
    /// When a round makes the list, an "Add your name" button appears. It opens VRIF's keyboard (poke the keys with a finger
    /// or point and pull the trigger). Adding a name is optional : the score is saved either way.
    /// The board's look comes from its children : swap the "Model" child for your own model and keep "Display".
    /// Right click the component header > Clear Highscores to reset, or > Show Save File Path to find the file.
    /// </summary>
    public class BoomHighscoreBoard : MonoBehaviour {

        [System.Serializable]
        class SaveData {
            public List<BoomHighscoreEntry> Entries = new List<BoomHighscoreEntry>();
        }

        public BoomScoreManager ScoreManager;

        [Tooltip("How many scores the board keeps")]
        public int MaxEntries = 10;

        [Tooltip("File name inside Application.persistentDataPath")]
        public string FileName = "boomblocks_highscores.json";

        [Tooltip("Rounds that end with 0 points are not saved")]
        public bool IgnoreZeroScores = true;

        [Header("Rows : ")]
        [Tooltip("One entry per row, top score first. Each row has a rank, score and name text")]
        public TMP_Text[] RankTexts;
        public TMP_Text[] ScoreTexts;
        public TMP_Text[] NameTexts;

        [Tooltip("Shown while there are no scores yet")]
        public GameObject EmptyMessage;

        [Tooltip("Name shown for scores saved without a name")]
        public string NoNameText = "---";

        [Header("Name Entry : ")]
        [Tooltip("Shown after a round that made the list. Opens the keyboard")]
        public GameObject AddNameButton;

        [Tooltip("Name field with the Done and Cancel buttons, shown while typing")]
        public GameObject NameEntryRow;

        public InputField NameInput;

        [Tooltip("VRIF VR Keyboard, shown while typing")]
        public GameObject Keyboard;

        public int MaxNameLength = 12;

        [Header("Look : ")]
        public Color ScoreColor = Color.white;
        public Color DimColor = new Color(1f, 1f, 1f, 0.45f);

        [Tooltip("The score from the round that just ended")]
        public Color NewScoreColor = new Color(1f, 0.78f, 0.2f);

        [Tooltip("Saved with each score (not shown). See C# DateTime formats")]
        public string DateFormat = "yyyy-MM-dd HH:mm";

        SaveData data = new SaveData();
        int newestIndex = -1;

        // The newest score that can still be given a name
        BoomHighscoreEntry pendingEntry;
        bool editingName;

        public string SavePath {
            get { return Path.Combine(Application.persistentDataPath, FileName); }
        }

        public IReadOnlyList<BoomHighscoreEntry> Entries {
            get { return data.Entries; }
        }

        void Start() {
            if (NameInput != null) {
                NameInput.characterLimit = MaxNameLength;
            }

            load();
            refresh();

            if (ScoreManager == null) {
                ScoreManager = BoomScoreManager.Instance;
            }
            if (ScoreManager != null) {
                ScoreManager.RoundEnded += AddScore;
            }
        }

        void OnDestroy() {
            if (ScoreManager != null) {
                ScoreManager.RoundEnded -= AddScore;
            }
        }

        /// <summary>
        /// Adds a result if it makes the list, saves and updates the board
        /// </summary>
        public void AddScore(int score) {
            if (IgnoreZeroScores && score <= 0) {
                return;
            }

            BoomHighscoreEntry entry = new BoomHighscoreEntry {
                Score = score,
                Name = "",
                Date = System.DateTime.Now.ToString(DateFormat)
            };

            // Highest first. A tie goes below the older score
            int index = data.Entries.Count;
            for (int x = 0; x < data.Entries.Count; x++) {
                if (score > data.Entries[x].Score) {
                    index = x;
                    break;
                }
            }

            newestIndex = -1;
            pendingEntry = null;
            if (index < MaxEntries) {
                data.Entries.Insert(index, entry);
                newestIndex = index;
                pendingEntry = entry;
            }
            while (data.Entries.Count > MaxEntries) {
                data.Entries.RemoveAt(data.Entries.Count - 1);
            }

            // A new round ended while typing : the old name entry is dropped
            editingName = false;

            save();
            refresh();
        }

        /// <summary>
        /// "Add your name" button
        /// </summary>
        public void OpenNameEntry() {
            if (pendingEntry == null) {
                return;
            }

            editingName = true;
            if (NameInput != null) {
                NameInput.text = pendingEntry.Name ?? "";
            }
            refresh();
        }

        /// <summary>
        /// "Done" button : saves the typed name on the newest score
        /// </summary>
        public void ConfirmName() {
            if (pendingEntry != null && NameInput != null) {
                string typed = NameInput.text.Trim();
                if (typed.Length > MaxNameLength) {
                    typed = typed.Substring(0, MaxNameLength);
                }
                pendingEntry.Name = typed;
                save();
            }

            editingName = false;
            pendingEntry = null;
            refresh();
        }

        /// <summary>
        /// "Cancel" button : closes the keyboard, the score stays without a name and can still be named
        /// </summary>
        public void CancelNameEntry() {
            editingName = false;
            refresh();
        }

        [ContextMenu("Clear Highscores")]
        public void ClearHighscores() {
            data = new SaveData();
            newestIndex = -1;
            pendingEntry = null;
            editingName = false;
            if (File.Exists(SavePath)) {
                File.Delete(SavePath);
            }
            refresh();
        }

        [ContextMenu("Show Save File Path")]
        void logSavePath() {
            Debug.Log("Boom Blocks highscores : " + SavePath);
        }

        void load() {
            data = new SaveData();
            try {
                if (File.Exists(SavePath)) {
                    SaveData loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                    if (loaded != null && loaded.Entries != null) {
                        data = loaded;
                    }
                }
            }
            catch (System.Exception e) {
                Debug.LogWarning("Boom Blocks : could not read highscores, starting fresh. " + e.Message);
            }
        }

        void save() {
            try {
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            }
            catch (System.Exception e) {
                Debug.LogWarning("Boom Blocks : could not save highscores. " + e.Message);
            }
        }

        void refresh() {
            int rows = RankTexts != null ? RankTexts.Length : 0;
            for (int x = 0; x < rows; x++) {
                bool used = x < data.Entries.Count;
                bool newest = x == newestIndex;
                string name = used && !string.IsNullOrEmpty(data.Entries[x].Name) ? data.Entries[x].Name : (used ? NoNameText : "");

                setRow(RankTexts, x, (x + 1) + ".", newest ? NewScoreColor : DimColor);
                setRow(ScoreTexts, x, used ? data.Entries[x].Score.ToString() : "-", used ? (newest ? NewScoreColor : ScoreColor) : DimColor);
                setRow(NameTexts, x, name, newest ? NewScoreColor : (used && !string.IsNullOrEmpty(data.Entries[x].Name) ? ScoreColor : DimColor));
            }

            bool canName = pendingEntry != null;
            if (AddNameButton != null) {
                AddNameButton.SetActive(canName && !editingName);
            }
            if (NameEntryRow != null) {
                NameEntryRow.SetActive(canName && editingName);
            }
            if (Keyboard != null) {
                Keyboard.SetActive(canName && editingName);
            }
            if (EmptyMessage != null) {
                EmptyMessage.SetActive(data.Entries.Count == 0);
            }
        }

        static void setRow(TMP_Text[] texts, int index, string value, Color color) {
            if (texts == null || index >= texts.Length || texts[index] == null) {
                return;
            }
            texts[index].text = value;
            texts[index].color = color;
        }
    }
}
